using Lonize.Logging;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using Kernel.Pool;
using Kernel.Status;
using Kernel.Nav;
namespace Kernel.Building
{
    /// <summary>
    /// 建筑拆除控制器：
    /// - 进入拆除模式后，点击建筑可将其回收到对象池（若支持池），否则直接销毁。
    /// - 可由 UI 按钮调用 StartRemoveMode() 开启拆除模式。
    /// </summary>
    public class BuildingRemoveController : MonoBehaviour
    {
        [Header("基本引用")]
        public Camera mainCamera;
        public LayerMask buildingLayerMask;
        public LayerMask obstacleLayerMask;
        public Tilemap placementTilemap;
        public NavGrid navGrid;

        [Header("输入设置")]
        [Tooltip("退出拆除模式的按键，例如 Esc。")]
        public KeyCode exitKey = KeyCode.Escape;

        private bool _isRemoving = false;

        private void Awake()
        {
            if (navGrid == null)
            {
                navGrid = NavGrid.Instance;
            }

            if (placementTilemap == null && navGrid != null)
            {
                placementTilemap = navGrid.mainTilemap;
            }

            navGrid?.InitializeFromTilemap(placementTilemap, buildingLayerMask, obstacleLayerMask);
        }

        /// <summary>
        /// 启动拆除模式（可绑定到 UI 按钮）。
        /// </summary>
        public void StartRemoveMode()
        {
            if (!StatusController.AddStatus(StatusList.RemovingBuildingStatus))
            {
                Log.Warn("[BuildingRemove] 无法进入拆除模式，已有其他状态阻塞。");
                return;
            }
            _isRemoving = true;
            Log.Info("[BuildingRemove] 进入拆除模式。");
        }

        /// <summary>
        /// 退出拆除模式。
        /// </summary>
        public void StopRemoveMode()
        {
            StatusController.RemoveStatus(StatusList.RemovingBuildingStatus);
            _isRemoving = false;
            Log.Info("[BuildingRemove] 退出拆除模式。");
        }

        /// <summary>
        /// 每帧检查是否在拆除模式下，并处理输入。
        /// </summary>
        private void Update()
        {
            if (!_isRemoving) return;

            // 鼠标在 UI 上则不处理
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // 按退出键退出拆除模式
            if (Input.GetKeyDown(exitKey) || Input.GetMouseButtonDown(1))
            {
                StopRemoveMode();
                return;
            }

            // 左键点击尝试拆除建筑
            if (Input.GetMouseButtonDown(0))
            {
                TryRemoveBuildingUnderMouse();
            }
        }

        /// <summary>
        /// 在鼠标位置发射射线，尝试找到建筑并执行移除。
        /// </summary>
        private void TryRemoveBuildingUnderMouse()
        {
            if (mainCamera == null)
            {
                Log.Error("[BuildingRemove] mainCamera 未设置。");
                return;
            }

            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 pos2D = new Vector2(worldPos.x, worldPos.y);

            // 射线检测建筑层
            RaycastHit2D hit = Physics2D.Raycast(pos2D, Vector2.zero, 0f, buildingLayerMask);
            if (hit.collider == null)
            {
                Log.Info("[BuildingRemove] 点击处没有检测到建筑。");
                return;
            }

            // 通常 BuildingRuntimeHost 在根对象，可以用 GetComponentInParent 保守一点
            var host = hit.collider.GetComponentInParent<BuildingRuntimeHost>();
            if (host == null)
            {
                Log.Warn("[BuildingRemove] 点击到的对象不包含 BuildingRuntimeHost，放弃拆除。");
                return;
            }

            RemoveBuilding(host);
        }

        /// <summary>
        /// 执行建筑移除逻辑，优先回收到对象池，否则直接销毁。
        /// </summary>
        /// <param name="host">要移除的建筑宿主组件。</param>
        private void RemoveBuilding(BuildingRuntimeHost host)
        {
            if (host == null)
            {
                return;
            }

            GameObject buildingGo = host.gameObject;

            TryReleaseNavGridArea(host);

            // 尝试使用对象池回收
            var poolMember = buildingGo.GetComponent<BuildingPoolMember>();
            if (PoolManager.Instance != null && poolMember != null)
            {
                PoolManager.Instance.ReturnToPool(buildingGo);
                Log.Info($"[BuildingRemove] 已将建筑回收到对象池：{buildingGo.name}");
            }
            else
            {
                // 没有池信息或没有 PoolManager，就直接销毁
                Destroy(buildingGo);
                Log.Info($"[BuildingRemove] 已销毁建筑（未池化）：{buildingGo.name}");
            }

            // 如果你以后定义了 BuildingRemoved 事件，可以在这里发布
            // var host = buildingGo.GetComponent<BuildingRuntimeHost>();
            // Events.eventBus.Publish(new BuildingRemoved(host));
        }

        private void TryReleaseNavGridArea(BuildingRuntimeHost host)
        {
            var nav = navGrid != null ? navGrid : NavGrid.Instance;
            if (nav == null)
            {
                return;
            }

            var map = placementTilemap != null ? placementTilemap : nav.mainTilemap;
            if (map == null)
            {
                return;
            }

            nav.InitializeFromTilemap(map, buildingLayerMask, obstacleLayerMask);

            var def = host.Runtime?.Def;
            int width = def?.Width ?? 1;
            int height = def?.Height ?? 1;
            int rotationSteps = Mathf.RoundToInt(host.transform.eulerAngles.z / 90f) % 4;
            Vector3Int anchorCell = map.WorldToCell(host.transform.position);

            nav.UpdateAreaBlocked(anchorCell, width, height, rotationSteps, false);
        }
    }
}
