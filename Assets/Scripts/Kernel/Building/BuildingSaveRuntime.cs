using System.Collections;
using System.Collections.Generic;
using Lonize.Logging;
using UnityEngine;
using UnityEngine.Tilemaps;
using Kernel.Nav;
using Kernel.Pool;

namespace Kernel.Building
{
    /// <summary>
    /// summary: 负责在存档和场景建筑之间做转换
    /// </summary>
    public static class BuildingSaveRuntime
    {
        private static bool _hasRestoredOnce = false;

        /// <summary>
        /// summary: 收集场景中所有建筑并写入存档列表
        /// param: list 用于输出存档数据的列表引用
        /// return: 无
        /// </summary>
        public static void CollectBuildingsForSave(ref List<SaveBuildingInstance> list)
        {
            var controller = Object.FindFirstObjectByType<BuildingPlacementController>();
            if (controller == null)
            {
                Log.Error("[SaveAllBuildings] CollectBuildingsForSave 找不到 BuildingPlacementController。");
                list = null;
                return;
            }

            if (controller.placementTilemap == null)
            {
                Log.Error("[SaveAllBuildings] CollectBuildingsForSave 中 placementTilemap 为空。");
                list = null;
                return;
            }

            // 只从 buildingRoot 下找建筑，避免误抓别的预制
            BuildingRuntimeHost[] hosts;
            if (controller.buildingRoot != null)
            {
                hosts = controller.buildingRoot.GetComponentsInChildren<BuildingRuntimeHost>(true);
            }
            else
            {
                hosts = Object.FindObjectsByType<BuildingRuntimeHost>(UnityEngine.FindObjectsSortMode.None);
            }

            if (hosts == null || hosts.Length == 0)
            {
                list = new List<SaveBuildingInstance>();
                Log.Info("[SaveAllBuildings] CollectBuildingsForSave：当前没有建筑需要保存。");
                return;
            }

            if (list == null)
            {
                list = new List<SaveBuildingInstance>(hosts.Length);
            }
            else
            {
                list.Clear();
                if (list.Capacity < hosts.Length)
                    list.Capacity = hosts.Length;
            }

            foreach (var host in hosts)
            {
                if (host == null) continue;

                var data = host.CreateSaveData(controller.placementTilemap);
                if (data != null)
                {
                    list.Add(data);
                }
            }

            Log.Info($"[SaveAllBuildings] CollectBuildingsForSave：已收集 {list.Count} 个建筑。");
        }
        /// <summary>
        /// summary: 清理当前地图上的所有建筑
        /// param: controller 放置控制器，提供 tilemap/nav 等
        /// return: 无
        /// </summary>
        private static void ClearExistingBuildings(BuildingPlacementController controller)
        {
            BuildingRuntimeHost[] hosts;
            if (controller.buildingRoot != null)
            {
                hosts = controller.buildingRoot.GetComponentsInChildren<BuildingRuntimeHost>(true);
            }
            else
            {
                hosts = Object.FindObjectsByType<BuildingRuntimeHost>(UnityEngine.FindObjectsSortMode.None);
            }

            if (hosts == null || hosts.Length == 0)
            {
                return;
            }

            var nav = controller.navGrid != null ? controller.navGrid : NavGrid.Instance;
            var tilemap = controller.placementTilemap;

            foreach (var host in hosts)
            {
                if (host == null) continue;
                var go = host.gameObject;

                // 还原导航阻挡（把之前占用的格子释放）
                if (nav != null && tilemap != null)
                {
                    // if (BuildingDatabase.TryGet(host.Runtime.Def, out var def))
                    // {
                    var def = host.Runtime.Def;
                        Vector3Int cellPos = tilemap.WorldToCell(host.transform.position);
                        float z = host.transform.eulerAngles.z;
                        byte rotSteps = (byte)(Mathf.RoundToInt(z / 90f) & 3);
                        nav.UpdateAreaBlocked(cellPos, def.Width, def.Height, rotSteps, false);
                    // }
                }

                // TODO: 如果你的 PoolManager 有专门的 Release 接口，可以改成回收
                Object.Destroy(go);
            }

            Log.Info($"[SaveAllBuildings] ClearExistingBuildings：清理 {hosts.Length} 个建筑。");
        }
        /// <summary>
        /// summary: 根据存档数据在场景中重新生成建筑
        /// param: list 从存档读取的建筑实例数据列表
        /// return: 无
        /// </summary>
        public static void RestoreBuildingsFromSave(List<SaveBuildingInstance> list)
        {
            Debug.Log("RestoreBuildingsFromSave called. List count: " + (list != null ? list.Count.ToString() : "null"));
            if (list == null || list.Count == 0)
            {
                Log.Info("[SaveAllBuildings] RestoreBuildingsFromSave：存档中没有建筑。");
                return;
            }
            var controller = Object.FindFirstObjectByType<BuildingPlacementController>();
            ClearExistingBuildings(controller);
            // if (_hasRestoredOnce)
            // {
            //     Log.Warn("[SaveAllBuildings] RestoreBuildingsFromSave 已经执行过一次，跳过重复还原。");
            //     return;
            // }
            // _hasRestoredOnce = true;

            // var controller = Object.FindFirstObjectByType<BuildingPlacementController>();
            if (controller == null)
            {
                Log.Error("[SaveAllBuildings] RestoreBuildingsFromSave 找不到 BuildingPlacementController。");
                return;
            }

            controller.StartCoroutine(RestoreBuildingsCoroutine(controller, list));
        }

        /// <summary>
        /// summary: 协程逐个生成建筑并应用存档数据
        /// param: controller 放置控制器，用于提供 Tilemap 与 Root
        /// param: list 存档中的建筑实例列表
        /// return: 协程枚举器
        /// </summary>
        private static IEnumerator RestoreBuildingsCoroutine(
            BuildingPlacementController controller,
            List<SaveBuildingInstance> list)
        {
            if (controller.placementTilemap == null)
            {
                Log.Error("[SaveAllBuildings] RestoreBuildingsCoroutine 中 placementTilemap 为空。");
                yield break;
            }
            var nav = controller.navGrid != null ? controller.navGrid : NavGrid.Instance;
            int restoredCount = 0;

            foreach (var data in list)
            {
                if (data == null) continue;

                // 1) 找 BuildingDef
                if (!BuildingDatabase.TryGet(data.DefId, out var def))
                {
                    Log.Warn($"[SaveAllBuildings] 还原时未找到 BuildingDef: {data.DefId}");
                    continue;
                }

                // 2) 坐标与旋转
                var cellPos = new Vector3Int(data.CellX, data.CellY, 0);
                var worldPos = controller.placementTilemap.GetCellCenterWorld(cellPos);
                byte rotSteps = data.RotSteps;
                Quaternion rot = Quaternion.Euler(0f, 0f, rotSteps * 90f);

                // 3) 通过对象池生成
                if (PoolManager.Instance == null)
                {
                    Log.Error("[SaveAllBuildings] RestoreBuildingsCoroutine 中 PoolManager.Instance 为空。");
                    yield break;
                }

                var task = PoolManager.Instance.GetAsync(def.Id, worldPos, rot);
                while (!task.IsCompleted)
                {
                    yield return null;
                }

                var go = task.Result;
                if (go == null)
                {
                    Log.Error($"[SaveAllBuildings] PoolManager.GetAsync 返回 null, DefId={def.Id}");
                    continue;
                }

                if (controller.buildingRoot != null)
                {
                    go.transform.SetParent(controller.buildingRoot, true);
                }

                // 4) 导航阻挡
                if (nav != null)
                {
                    nav.UpdateAreaBlocked(cellPos, def.Width, def.Height, rotSteps, true);
                }

                // 5) 应用运行时状态
                var host = go.GetComponent<BuildingRuntimeHost>();
                if (host != null)
                {
                    host.ApplySaveData(data);
                }

                restoredCount++;
                if (restoredCount % 16 == 0)
                {
                    yield return null;
                }
            }

            Log.Info($"[SaveAllBuildings] RestoreBuildingsFromSave 完成，还原 {restoredCount} 个建筑。");
        }
    }
}
