using Lonize.Events;
using Lonize.Logging;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using Kernel.Building;
using Kernel;
using Kernel.Pool;   // 为了用 AddressableRef.LoadAsync<GameObject>

namespace Kernel.Building
{
    /// <summary>
    /// 基于 BuildingDef / BuildingFactory 的建筑放置控制器
    /// - 预览：用真实 prefab 实例化一份 ghost（禁用碰撞，只做显示）
    /// - 放置：用 BuildingFactory.SpawnToWorldAsync(def.Id, pos, rot) 创建正式建筑
    /// </summary>
    public class BuildingPlacementController : MonoBehaviour
    {
        [Header("基本引用")]
        public Camera mainCamera;
        public Transform buildingRoot;
        public Tilemap placementTilemap;
        public LayerMask buildingLayerMask;
        public LayerMask obstacleLayerMask;

        [Header("BuildingDef 配置")]
        [Tooltip("与 UI 按钮 index 对应的 BuildingDef Id 列表")]
        public string[] buildingIds;

        [Header("虚影参数")]
        public Color ghostColor = new Color(1f, 1f, 1f, 0.4f);
        public Color cannotPlaceColor = new Color(1f, 0.3f, 0.3f, 0.4f);

        [Header("旋转设置")]
        [Tooltip("是否允许在放置时旋转建筑（用 Q/E 或鼠标滚轮等）")]
        public bool enableRotate = true;
        public KeyCode rotateLeftKey = KeyCode.Q;
        public KeyCode rotateRightKey = KeyCode.E;

        // 当前状态
        private BuildingDef _currentDef;
        private GameObject _ghostInstance;
        private int _rotationSteps = 0;  // 0/1/2/3 => 0/90/180/270
        private bool _isPlacing = false;

        void Update()
        {
            if (_isPlacing)
            {
                HandlePlacementUpdate();
            }
        }

        #region 启动放置

        /// <summary>
        /// 给 UI 用：根据 index 找对应的 BuildingDef Id
        /// </summary>
        public async void StartPlacementByIndex(int index)
        {
            if (buildingIds == null || index < 0 || index >= buildingIds.Length)
            {
                Log.Warn("[BuildingPlacement] Building index out of range.");
                return;
            }

            string id = buildingIds[index];
            await StartPlacementById(id);
        }

        /// <summary>
        /// 主入口：根据 BuildingDef.Id 开始放置流程
        /// </summary>
        public async System.Threading.Tasks.Task StartPlacementById(string buildingId)
        {
            // 清理旧虚影
            if (_ghostInstance != null)
            {
                Destroy(_ghostInstance);
                _ghostInstance = null;
            }

            _rotationSteps = 0;
            _currentDef = null;
            _isPlacing = false;

            if (!BuildingDatabase.TryGet(buildingId, out _currentDef))
            {
                Log.Error($"[BuildingPlacement] 未找到 BuildingDef: {buildingId}");
                return;
            }

            Log.Info($"[BuildingPlacement] 开始放置建筑：{_currentDef.Id} ({_currentDef.Name})");

            // 从 Addressables 加载 prefab（用于 ghost 预览）
            var prefab = await AddressableRef.LoadAsync<GameObject>(_currentDef.PrefabAddress);
            if (prefab == null)
            {
                Log.Error($"[BuildingPlacement] 无法加载 Prefab: {_currentDef.PrefabAddress}");
                return;
            }

            // 实例化 ghost
            _ghostInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity, buildingRoot);
            _ghostInstance.name = _currentDef.Id + "_Ghost";

            // 禁用 ghost 上的碰撞器，避免影响射线检测或物理
            foreach (var col in _ghostInstance.GetComponentsInChildren<Collider2D>())
            {
                col.enabled = false;
            }

            // 设置初始颜色为 ghostColor
            SetGhostColor(ghostColor);

            // ghost 不需要逻辑：可以选择禁用 BuildingRuntimeHost 等脚本
            var host = _ghostInstance.GetComponent<BuildingRuntimeHost>();
            if (host != null)
            {
                host.enabled = false;
            }

            _isPlacing = true;
        }

        #endregion

        #region 每帧更新逻辑

        private void HandlePlacementUpdate()
        {
            if (_currentDef == null || _ghostInstance == null) return;

            // 鼠标在 UI 上就不处理放置
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // 1. 旋转处理（可选）
            if (enableRotate)
            {
                HandleRotateInput();
            }

            // 2. 计算锚点格子 & 对齐 ghost
            Vector3 mouseScreenPos = Input.mousePosition;
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            var cellPos = placementTilemap.WorldToCell(mouseWorldPos);
            Vector3 cellCenterWorld = placementTilemap.GetCellCenterWorld(cellPos);

            // TODO: 如果你使用 Pivot 在左下角、且考虑宽高/旋转，可以在这里做更复杂的 offset 计算
            _ghostInstance.transform.position = cellCenterWorld;
            _ghostInstance.transform.rotation = Quaternion.Euler(0f, 0f, _rotationSteps * 90f);

            // 3. 检查能否放置（现在简单版本，只判断 tile 是否存在；多格/占用检测可在这里扩展）
            bool canPlace = CheckCanPlace(cellPos);

            // 根据能否放置切换颜色
            SetGhostColor(canPlace ? ghostColor : cannotPlaceColor);

            // 4. 左键确认放置
            if (Input.GetMouseButtonDown(0))
            {
                if (canPlace)
                {
                    // Fire & forget 放置
                    _ = PlaceBuildingAsync(cellPos, cellCenterWorld);
                }
                else
                {
                    Log.Info("[BuildingPlacement] 当前位置不可放置。");
                }
            }

            // 5. 右键取消放置
            if (Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
            }
        }

        private void HandleRotateInput()
        {
            if (Input.GetKeyDown(rotateLeftKey))
            {
                _rotationSteps = (_rotationSteps + 3) % 4; // -90
            }
            if (Input.GetKeyDown(rotateRightKey))
            {
                _rotationSteps = (_rotationSteps + 1) % 4; // +90
            }
        }

        #endregion

        #region 放置 & 取消

    /// <summary>
    /// 真正生成建筑，优先从对象池中获取，没有则通过 BuildingFactory 创建。
    /// </summary>
    private async System.Threading.Tasks.Task PlaceBuildingAsync(Vector3Int cellPos, Vector3 worldPos)
    {
        if (_currentDef == null)
        {
            Log.Warn("[BuildingPlacement] PlaceBuilding 时 _currentDef 为空。");
            return;
        }

        if (!CheckCanPlace(cellPos))
        {
            Log.Info("[BuildingPlacement] PlaceBuilding 时检测失败。");
            return;
        }

        Quaternion rot = Quaternion.Euler(0f, 0f, _rotationSteps * 90f);

        if (PoolManager.Instance == null)
        {
            Log.Error("[BuildingPlacement] PoolManager.Instance 为空，无法生成建筑。");
            return;
        }

        // 这里使用的是 BuildingDef.Id，和 PoolManager / BuildingFactory 的约定一致
        var go = await PoolManager.Instance.GetAsync(_currentDef.Id, worldPos, rot);
        if (go == null)
        {
            Log.Error("[BuildingPlacement] PoolManager.GetAsync 失败。");
            return;
        }

        if (buildingRoot != null)
        {
            go.transform.SetParent(buildingRoot, true);
        }

        Log.Info($"[BuildingPlacement] 已放置建筑：{_currentDef.Id} @ {worldPos}");

        var runtimeHost = go.GetComponent<BuildingRuntimeHost>();
        Events.eventBus.Publish(new BuildingPlaced(true, runtimeHost));
    }

        private void CancelPlacement()
        {
            if (_ghostInstance != null)
            {
                Destroy(_ghostInstance);
            }
            _ghostInstance = null;
            _currentDef = null;
            _isPlacing = false;
            _rotationSteps = 0;

            Log.Info("[BuildingPlacement] 已取消放置模式。");
        }

        #endregion

        #region 工具方法：可放置检测 & 颜色

        /// <summary>
        /// 判断从给定 anchor cell 开始是否可以放置当前建筑。
        /// 当前简单版：仅判断该 cell 是否有 tile。
        /// 你可以在这里接入：
        /// - BuildingFootprint.GetCells(def.Width, def.Height, _rotationSteps)
        /// - 自己的占用表/阻挡表
        /// </summary>
        private bool CheckCanPlace(Vector3Int anchorCell)
        {
            if(_currentDef == null || placementTilemap == null)
            {
                Log.Warn("[BuildingPlacement] CheckCanPlace 时 _currentDef 或 placementTilemap 为空。");
                return false;
            }
            if(!placementTilemap.HasTile(anchorCell))
            {
                Log.Warn("[BuildingPlacement] CheckCanPlace 时目标格子无 Tile。");
                return false;
            }
                // return false;
            // if(BuildingLayerCheck() == false)
            // {
            //     Log.Warn("[BuildingPlacement] CheckCanPlace 时目标格子有建筑物。");
            //     return false;
            // }
            if(IsCellOccupied(anchorCell))
            {
                return false;
            }
            
            // if(_currentDef.Width > 1 || _currentDef.Height > 1)
            // {
            //     var offsets = BuildingFootprint.GetCells(_currentDef.Width, _currentDef.Height, _rotationSteps);
            //     foreach(var o in offsets)
            //     {
            //         var c = anchorCell + o;
            //         if(!placementTilemap.HasTile(c))
            //             return false;
            //     }
            // }
            // if (_currentDef == null || placementTilemap == null) return false;

            // // 简单版：要求 anchor 那格必须有 tile
            // if (!placementTilemap.HasTile(anchorCell))
            //     return false;

            // ↓↓↓ 如果你想支持多格 & 占用检测，可以扩展如下（示意） ↓↓↓
            /*
            var offsets = BuildingFootprint.GetCells(_currentDef.Width, _currentDef.Height, _rotationSteps);
            foreach (var o in offsets)
            {
                var c = anchorCell + o;
                // 1) 必须有 tile
                if (!placementTilemap.HasTile(c)) return false;
                // 2) TODO: 检查此格是否已经被其他建筑占用
                // if (IsCellOccupied(c)) return false;
            }
            */
            return true;
        }
        private bool IsCellOccupied(Vector3Int cellPos)
        {
            var _startPos = cellPos - new Vector3Int(_currentDef.Width / 2, _currentDef.Height / 2, 0);
            for(int i = 0; i < _currentDef.Width; i++)
            {
                for(int j = 0; j < _currentDef.Height; j++)
                {
                    Vector3Int checkPos = _startPos + new Vector3Int(i, j, 0);
                    // 共同使用 placementTilemap 的 cell -> world 位置
                    Vector3 worldPos = placementTilemap.GetCellCenterWorld(checkPos);

                    // 1) 检查建筑物层（通过 Physics2D 碰撞检测）
                    RaycastHit2D hitBuilding = Physics2D.Raycast(worldPos, Vector2.zero, 0f, buildingLayerMask);
                    if (hitBuilding.collider != null)
                    {
                        // 点到建筑了
                        return true;
                    }

                    // 2) 检查场景中所有标记为 obstacle 的 Tilemap（通过 Tilemap.HasTile）
                    //    这样可以检测到放在其它 Tilemap 上、但与 placementTilemap 不同的 Tilemap
                            var allTilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
                    foreach (var tm in allTilemaps)
                    {
                        // 跳过不在 obstacleLayerMask 中的 Tilemap
                        if ((obstacleLayerMask.value & (1 << tm.gameObject.layer)) == 0) continue;

                        // 将 worldPos 转换为该 Tilemap 的 cell，然后判断是否存在 Tile
                        Vector3Int tmCell = tm.WorldToCell(worldPos);
                        if (tm.HasTile(tmCell))
                        {
                            Log.Info("[BuildingPlacement] Obstacle Tile detected at cell: " + checkPos + " on Tilemap: " + tm.name);
                            return true;
                        }
                    }

                    // 3) 保留对 obstacle layer 上物理碰撞体的检测（有些障碍是 Collider）
                    RaycastHit2D hitObstacle = Physics2D.Raycast(worldPos, Vector2.zero, 0f, obstacleLayerMask);
                    if (hitObstacle.collider != null)
                    {
                        Log.Info("[BuildingPlacement] Obstacle collider detected at cell: " + checkPos + " (collider: " + hitObstacle.collider.name + ")");
                        return true;
                    }
                }
            }
            return false;
        }

        private void SetGhostColor(Color c)
        {
            if (_ghostInstance == null) return;

            foreach (var sr in _ghostInstance.GetComponentsInChildren<SpriteRenderer>())
            {
                // 只改颜色，不改材质
                sr.color = c;
            }
        }

        #endregion
    }
}