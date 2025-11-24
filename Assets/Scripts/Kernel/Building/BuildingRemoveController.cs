using Lonize.Logging;
using UnityEngine;
using UnityEngine.EventSystems;
using Kernel.Pool;

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

        [Header("输入设置")]
        [Tooltip("退出拆除模式的按键，例如 Esc。")]
        public KeyCode exitKey = KeyCode.Escape;

        private bool _isRemoving = false;

        /// <summary>
        /// 启动拆除模式（可绑定到 UI 按钮）。
        /// </summary>
        public void StartRemoveMode()
        {
            _isRemoving = true;
            Log.Info("[BuildingRemove] 进入拆除模式。");
        }

        /// <summary>
        /// 退出拆除模式。
        /// </summary>
        public void StopRemoveMode()
        {
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

            RemoveBuilding(host.gameObject);
        }

        /// <summary>
        /// 执行建筑移除逻辑，优先回收到对象池，否则直接销毁。
        /// </summary>
        /// <param name="buildingGo">要移除的建筑根 GameObject。</param>
        private void RemoveBuilding(GameObject buildingGo)
        {
            if (buildingGo == null)
            {
                return;
            }

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
    }
}
