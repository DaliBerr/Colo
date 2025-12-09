
using System.Collections.Generic;
// using Lonize.Logging;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Kernel.Building
{
    public class BuildingRuntimeHost : MonoBehaviour
    {
        // [SerializeField]
        public BuildingRuntime Runtime;
        public List<IBuildingBehaviour> Behaviours = new();

        public SaveBuildingInstance CreateSaveData(Tilemap placementTilemap)
        {
            if (placementTilemap == null)
            {
                return null;
            }

            // 计算所在网格格子
            Vector3Int cellPos = placementTilemap.WorldToCell(transform.position);
            // 计算旋转步数（0/1/2/3）
            float z = transform.eulerAngles.z;
            byte rotSteps = (byte)(Mathf.RoundToInt(z / 90f) & 3);

            var data = new SaveBuildingInstance
            {
                DefId = Runtime.Def.Id,
                RuntimeId = Runtime.BuildingID,
                CellX = cellPos.x,
                CellY = cellPos.y,
                RotSteps = rotSteps,
                HP = Runtime.HP
            };

            if (Runtime.RuntimeStats != null && Runtime.RuntimeStats.Count > 0)
            {
                int count = Runtime.RuntimeStats.Count;
                data.StatKeys = new string[count];
                data.StatValues = new float[count];

                int i = 0;
                foreach (var kv in Runtime.RuntimeStats)
                {
                    data.StatKeys[i] = kv.Key;
                    data.StatValues[i] = kv.Value;
                    i++;
                }
            }
            else
            {
                data.StatKeys = System.Array.Empty<string>();
                data.StatValues = System.Array.Empty<float>();
            }

            return data;
        }

        /// <summary>
        /// summary: 将存档数据应用到当前建筑实例
        /// param: data 存档中的建筑数据
        /// return: 无
        /// </summary>
        public void ApplySaveData(SaveBuildingInstance data)
        {
            if (data == null) return;

            Runtime.BuildingID = data.RuntimeId;
            Runtime.Def.Id = data.DefId;
            Runtime.HP = data.HP;

            Runtime.RuntimeStats ??= new Dictionary<string, float>();
            Runtime.RuntimeStats.Clear();

            if (data.StatKeys != null && data.StatValues != null)
            {
                int len = Mathf.Min(data.StatKeys.Length, data.StatValues.Length);
                for (int i = 0; i < len; i++)
                {
                    var key = data.StatKeys[i];
                    var val = data.StatValues[i];
                    if (!string.IsNullOrEmpty(key))
                    {
                        Runtime.RuntimeStats[key] = val;
                    }
                }
            }
        }
    }
}