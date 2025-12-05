using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Kernel
{
    /// <summary>
    /// 网格路径寻路服务，支持同步与异步查询。
    /// </summary>
    public sealed class PathfindingService
    {
        private readonly Tilemap _tilemap;
        private readonly NavGrid _navGrid;
        private readonly IMovementCostStrategy _movementCostStrategy;
        private readonly INeighborStrategy _neighborStrategy;
        private readonly bool _allowDiagonal;

        public PathfindingService(
            Tilemap tilemap,
            NavGrid navGrid,
            IMovementCostStrategy movementCostStrategy = null,
            INeighborStrategy neighborStrategy = null,
            bool allowDiagonal = false)
        {
            _tilemap = tilemap ? tilemap : throw new ArgumentNullException(nameof(tilemap));
            _navGrid = navGrid ?? throw new ArgumentNullException(nameof(navGrid));
            _movementCostStrategy = movementCostStrategy ?? new DefaultMovementCostStrategy();
            _neighborStrategy = neighborStrategy ?? new GridNeighborStrategy();
            _allowDiagonal = allowDiagonal;
        }

        /// <summary>
        /// 使用世界坐标进行同步寻路。
        /// </summary>
        public IReadOnlyList<Vector3Int> FindPathFromWorld(
            Vector3 worldStart,
            Vector3 worldGoal,
            int maxSteps = 4096,
            bool? allowDiagonalOverride = null,
            CancellationToken cancellationToken = default)
        {
            var startCell = WorldToCell(worldStart);
            var goalCell = WorldToCell(worldGoal);
            return FindPath(startCell, goalCell, maxSteps, allowDiagonalOverride, cancellationToken);
        }

        /// <summary>
        /// 使用世界坐标进行异步寻路，带超时保护。
        /// </summary>
        public async Task<IReadOnlyList<Vector3Int>> FindPathFromWorldAsync(
            Vector3 worldStart,
            Vector3 worldGoal,
            int maxSteps = 4096,
            TimeSpan? timeout = null,
            bool? allowDiagonalOverride = null,
            CancellationToken cancellationToken = default)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (timeout.HasValue)
            {
                linkedCts.CancelAfter(timeout.Value);
            }

            return await Task.Run(
                () => FindPathFromWorld(worldStart, worldGoal, maxSteps, allowDiagonalOverride, linkedCts.Token),
                linkedCts.Token);
        }

        /// <summary>
        /// 使用 cell 坐标进行同步寻路。
        /// </summary>
        public IReadOnlyList<Vector3Int> FindPath(
            Vector3Int start,
            Vector3Int goal,
            int maxSteps = 4096,
            bool? allowDiagonalOverride = null,
            CancellationToken cancellationToken = default)
        {
            if (!_navGrid.IsWalkable(start) || !_navGrid.IsWalkable(goal))
            {
                return Array.Empty<Vector3Int>();
            }

            var openSet = new List<Vector3Int> { start };
            var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            var gScore = new Dictionary<Vector3Int, float> { [start] = 0f };
            var fScore = new Dictionary<Vector3Int, float> { [start] = Heuristic(start, goal, allowDiagonalOverride) };
            var allowDiagonal = allowDiagonalOverride ?? _allowDiagonal;

            var steps = 0;
            while (openSet.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (steps++ > maxSteps)
                {
                    return Array.Empty<Vector3Int>();
                }

                var current = PopLowest(openSet, fScore);
                if (current == goal)
                {
                    return ReconstructPath(cameFrom, current);
                }

                foreach (var neighbor in _neighborStrategy.GetNeighbors(current, allowDiagonal))
                {
                    if (!_navGrid.IsWalkable(neighbor))
                    {
                        continue;
                    }

                    var tentativeG = gScore[current] + _movementCostStrategy.GetCost(current, neighbor, _navGrid);
                    if (!gScore.TryGetValue(neighbor, out var neighborScore) || tentativeG < neighborScore)
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + Heuristic(neighbor, goal, allowDiagonal);
                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            return Array.Empty<Vector3Int>();
        }

        /// <summary>
        /// 使用 cell 坐标进行异步寻路，带超时保护。
        /// </summary>
        public async Task<IReadOnlyList<Vector3Int>> FindPathAsync(
            Vector3Int start,
            Vector3Int goal,
            int maxSteps = 4096,
            TimeSpan? timeout = null,
            bool? allowDiagonalOverride = null,
            CancellationToken cancellationToken = default)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (timeout.HasValue)
            {
                linkedCts.CancelAfter(timeout.Value);
            }

            return await Task.Run(
                () => FindPath(start, goal, maxSteps, allowDiagonalOverride, linkedCts.Token),
                linkedCts.Token);
        }

        private Vector3Int WorldToCell(Vector3 worldPosition)
        {
            return _tilemap.WorldToCell(worldPosition);
        }

        private static Vector3Int PopLowest(List<Vector3Int> openSet, Dictionary<Vector3Int, float> fScore)
        {
            var bestIndex = 0;
            var bestValue = float.PositiveInfinity;
            for (var i = 0; i < openSet.Count; i++)
            {
                var node = openSet[i];
                var score = fScore.TryGetValue(node, out var value) ? value : float.PositiveInfinity;
                if (score < bestValue)
                {
                    bestValue = score;
                    bestIndex = i;
                }
            }

            var result = openSet[bestIndex];
            openSet.RemoveAt(bestIndex);
            return result;
        }

        private static IReadOnlyList<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
        {
            var path = new List<Vector3Int> { current };
            while (cameFrom.TryGetValue(current, out var previous))
            {
                current = previous;
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        private static float Heuristic(Vector3Int from, Vector3Int to, bool? allowDiagonalOverride)
        {
            var dx = Mathf.Abs(from.x - to.x);
            var dy = Mathf.Abs(from.y - to.y);
            var allowDiagonal = allowDiagonalOverride ?? false;
            // 对角启用时使用棋盘距离，否则使用曼哈顿距离
            return allowDiagonal ? Mathf.Max(dx, dy) : dx + dy;
        }
    }

    /// <summary>
    /// 网格可行走数据。
    /// </summary>
    public sealed class NavGrid
    {
        private readonly HashSet<Vector3Int> _walkableCells;
        private readonly Dictionary<Vector3Int, float> _terrainCosts;

        public float DefaultCost { get; }

        public NavGrid(IEnumerable<Vector3Int> walkableCells, IDictionary<Vector3Int, float> terrainCosts = null, float defaultCost = 1f)
        {
            _walkableCells = new HashSet<Vector3Int>(walkableCells ?? Array.Empty<Vector3Int>());
            _terrainCosts = terrainCosts != null
                ? new Dictionary<Vector3Int, float>(terrainCosts)
                : new Dictionary<Vector3Int, float>();
            DefaultCost = defaultCost;
        }

        public bool IsWalkable(Vector3Int cell)
        {
            return _walkableCells.Contains(cell);
        }

        public float GetCellCost(Vector3Int cell)
        {
            return _terrainCosts.TryGetValue(cell, out var cost) ? cost : DefaultCost;
        }
    }

    /// <summary>
    /// 移动代价策略接口，便于支持不同地形。
    /// </summary>
    public interface IMovementCostStrategy
    {
        float GetCost(Vector3Int from, Vector3Int to, NavGrid navGrid);
    }

    /// <summary>
    /// 邻居采样策略接口，便于切换对角移动规则。
    /// </summary>
    public interface INeighborStrategy
    {
        IEnumerable<Vector3Int> GetNeighbors(Vector3Int cell, bool allowDiagonal);
    }

    /// <summary>
    /// 默认移动代价策略，包含地形代价和对角额外成本。
    /// </summary>
    public sealed class DefaultMovementCostStrategy : IMovementCostStrategy
    {
        public float GetCost(Vector3Int from, Vector3Int to, NavGrid navGrid)
        {
            var baseCost = navGrid.GetCellCost(to);
            var isDiagonal = Math.Abs(from.x - to.x) == 1 && Math.Abs(from.y - to.y) == 1;
            // 对角移动稍微增大代价
            return isDiagonal ? baseCost * 1.4142f : baseCost;
        }
    }

    /// <summary>
    /// 默认邻居策略，支持 4/8 方向。
    /// </summary>
    public sealed class GridNeighborStrategy : INeighborStrategy
    {
        private static readonly Vector3Int[] CardinalDirections =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0)
        };

        private static readonly Vector3Int[] DiagonalDirections =
        {
            new Vector3Int(1, 1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(-1, -1, 0)
        };

        public IEnumerable<Vector3Int> GetNeighbors(Vector3Int cell, bool allowDiagonal)
        {
            foreach (var dir in CardinalDirections)
            {
                yield return cell + dir;
            }

            if (!allowDiagonal)
            {
                yield break;
            }

            foreach (var dir in DiagonalDirections)
            {
                yield return cell + dir;
            }
        }
    }
}
