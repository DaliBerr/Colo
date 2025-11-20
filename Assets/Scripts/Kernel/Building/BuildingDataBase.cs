// BuildingDatabase.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lonize.Events;
using Lonize.Logging;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Kernel.Building
{
    public static class BuildingDatabase
    {
        public static readonly Dictionary<string, BuildingDef> Defs = new();

        static readonly JsonSerializerSettings _jsonSettings = new()
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        public static async Task LoadAllAsync(string labelOrGroup = "BuildingDef")
        {
            Defs.Clear();

            AsyncOperationHandle<IList<IResourceLocation>> locHandle =
                Addressables.LoadResourceLocationsAsync(labelOrGroup, typeof(TextAsset));

            IList<IResourceLocation> locations = null;
            try { locations = await locHandle.Task; }
            catch (System.Exception ex)
            {
                Log.Error($"[Building] 查询 Addressables 失败（{labelOrGroup}）：\n{ex}");
                // Debug.LogError($"[Building] 查询 Addressables 失败（{labelOrGroup}）：\n{ex}");
                if (locHandle.IsValid()) Addressables.Release(locHandle);
                return;
            }

            if (locations == null || locations.Count == 0)
            {
                Log.Warn($"[Building] 未找到任何 TextAsset（{labelOrGroup}）。");
                if (locHandle.IsValid()) Addressables.Release(locHandle);
                return;
            }

            AsyncOperationHandle<IList<TextAsset>> loadHandle =
                Addressables.LoadAssetsAsync<TextAsset>(locations, null, true);

            IList<TextAsset> assets = null;
            try { assets = await loadHandle.Task; }
            catch (System.Exception ex)
            {
                Log.Error($"[Building] 批量加载 TextAsset 失败：\n{ex}");
                if (loadHandle.IsValid()) Addressables.Release(loadHandle);
                if (locHandle.IsValid()) Addressables.Release(locHandle);
                return;
            }

            foreach (var ta in assets)
            {
                if (!ta) continue;
                try
                {
                    var def = JsonConvert.DeserializeObject<BuildingDef>(ta.text, _jsonSettings);
                    if (BuildingValidation.Validate(def, out var msg))
                    {
                        if (!Defs.TryAdd(def.Id, def))
                            Log.Error($"[Building] 重复ID：{def.Id}（资产：{ta.name}）");
                    }
                    else
                    {
                        Log.Error($"[Building] 定义非法（资产：{ta.name}）：\n{msg}");
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[Building] 解析失败（资产：{ta.name}）：\n{ex}");
                }
            }

            if (loadHandle.IsValid()) Addressables.Release(loadHandle);
            if (locHandle.IsValid()) Addressables.Release(locHandle);

            // BuildingEvents.RaiseDatabaseLoaded(Defs.Keys.ToList());
            // Events.eventBus.Publish(new BuildingLoadingProgress(Defs.Keys.Count, Defs.Count));
            Events.eventBus.Publish(new BuildingLoaded(Defs.Keys.Count));
        }

        public static bool TryGet(string id, out BuildingDef def) => Defs.TryGetValue(id, out def);
    }
}
