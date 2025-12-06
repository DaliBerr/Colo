using System.Collections.Generic;
using Lonize.Logging;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Kernel.Nav
{
    /// <summary>
    /// 简易导航格子服务，记录每个格子的可行走状态。
    /// </summary>
    public class NavGrid : MonoBehaviour
    {
        [Header("引用")]
        public Tilemap mainTilemap;
        public LayerMask buildingLayerMask;
        public LayerMask obstacleLayerMask;

        /// <summary>
        /// 单例引用。
        /// </summary>
        public static NavGrid Instance { get; private set; }

        private readonly Dictionary<Vector3Int, bool> _blocked = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            InitializeFromTilemap(mainTilemap, buildingLayerMask, obstacleLayerMask);
        }

        /// <summary>
        /// 重新从 Tilemap 初始化阻挡数据。
        /// </summary>
        public void InitializeFromTilemap(Tilemap tilemap, LayerMask buildingMask, LayerMask obstacleMask)
        {
            if (tilemap != null && mainTilemap != tilemap)
            {
                mainTilemap = tilemap;
                _blocked.Clear();
            }

            buildingLayerMask = buildingMask;
            obstacleLayerMask = obstacleMask;

            if (mainTilemap == null)
            {
                Log.Warn("[NavGrid] mainTilemap 未设置，跳过初始化。");
                return;
            }

            if (_blocked.Count > 0)
            {
                return;
            }

            foreach (var cell in mainTilemap.cellBounds.allPositionsWithin)
            {
                if (!mainTilemap.HasTile(cell))
                {
                    continue;
                }

                _blocked[cell] = HasStaticObstacle(cell);
            }
        }

        /// <summary>
        /// 检查某格是否已被阻挡。
        /// </summary>
        public bool IsCellBlocked(Vector3Int cell)
        {
            return !_blocked.TryGetValue(cell, out var blocked) || blocked;
        }

        /// <summary>
        /// 更新单个格子的阻挡状态。
        /// </summary>
        public void UpdateCellBlocked(Vector3Int cell, bool blocked)
        {
            if (mainTilemap != null && !mainTilemap.HasTile(cell))
            {
                return;
            }

            _blocked[cell] = blocked;
        }

        /// <summary>
        /// 以中心格子为基准更新矩形占用区。
        /// </summary>
        public void UpdateAreaBlocked(Vector3Int anchorCell, int width, int height, int rotationSteps, bool blocked)
        {
            foreach (var cell in GetFootprintCells(anchorCell, width, height, rotationSteps))
            {
                UpdateCellBlocked(cell, blocked);
            }
        }

        /// <summary>
        /// 获取建筑在当前旋转下覆盖的格子列表。
        /// </summary>
        public List<Vector3Int> GetFootprintCells(Vector3Int anchorCell, int width, int height, int rotationSteps)
        {
            var cells = new List<Vector3Int>();
            if (mainTilemap == null)
            {
                return cells;
            }

            int rot = ((rotationSteps % 4) + 4) % 4;
            int realWidth = (rot % 2 == 1) ? height : width;
            int realHeight = (rot % 2 == 1) ? width : height;

            Vector3Int start = anchorCell - new Vector3Int(realWidth / 2, realHeight / 2, 0);
            for (int x = 0; x < realWidth; x++)
            {
                for (int y = 0; y < realHeight; y++)
                {
                    Vector3Int cell = start + new Vector3Int(x, y, 0);
                    if (mainTilemap.HasTile(cell))
                    {
                        cells.Add(cell);
                    }
                }
            }

            return cells;
        }

        private bool HasStaticObstacle(Vector3Int cell)
        {
            if (mainTilemap == null)
            {
                return true;
            }

            Vector3 worldPos = mainTilemap.GetCellCenterWorld(cell);

            if (Physics2D.Raycast(worldPos, Vector2.zero, 0f, buildingLayerMask))
            {
                return true;
            }

            if (Physics2D.Raycast(worldPos, Vector2.zero, 0f, obstacleLayerMask))
            {
                return true;
            }

            var allTilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            foreach (var tm in allTilemaps)
            {
                if ((obstacleLayerMask.value & (1 << tm.gameObject.layer)) == 0)
                {
                    continue;
                }

                Vector3Int tmCell = tm.WorldToCell(worldPos);
                if (tm.HasTile(tmCell))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
