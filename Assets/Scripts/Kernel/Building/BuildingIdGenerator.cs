using System;
using System.Linq;
using Lonize.Scribe;

namespace Kernel.Building
{
    /// <summary>
    /// 统一管理 Building 的自增 ID，并通过 Scribe 系统持久化。
    /// </summary>
    public static class BuildingIdGenerator
    {
        // 存档里用于识别这个条目的 TypeId
        private const string SaveItemTypeId = "building-id-counter";

        // 下一次要用的 ID（内存中的真实计数器）
        private static long _nextBuildingID = 1;

        // 对应的存档条目对象（加载后会指向同一个实例）
        private static SaveItem _saveItem;

        private static bool _initialized;

        /// <summary>
        /// 生成一个新的建筑 ID，并更新存档条目中的计数值。
        /// （请在游戏初始化时先调用 InitializeFromSave）
        /// </summary>
        public static long GenerateBuildingID()
        {
            EnsureInitialized();

            long id = _nextBuildingID++;
            if (_saveItem != null)
            {
                // 我们保存“下一次要使用的 ID”，和你原来的 SaveLastUsedID 行为一致
                _saveItem.nextId = _nextBuildingID;
            }
            return id;
        }

        /// <summary>
        /// 初始化：从当前存档中读取计数器，没有的话就创建一个新的条目。
        /// 应该在 ScribeSaveManager 读完存档之后调用一次。
        /// </summary>
        public static void InitializeFromSave()
        {
            if (_initialized) return;
            _initialized = true;

            var mgr = ScribeSaveManager.Instance;
            if (mgr == null)
            {
                // 没有存档管理器，就只用内存计数（不会持久化）
                return;
            }

            // 找有没有已经存在的计数条目（通常只有一个）
            _saveItem = mgr.GetItems<SaveItem>().FirstOrDefault();

            if (_saveItem == null)
            {
                // 旧存档里没有这个条目：创建一份新的，并用当前内存值初始化
                _saveItem = new SaveItem { nextId = _nextBuildingID };
                mgr.AddItem(_saveItem);
            }
            else
            {
                // 从存档恢复计数器，至少从 1 开始
                _nextBuildingID = Math.Max(_saveItem.nextId, 1L);
            }
        }

        /// <summary>
        /// 重置计数器（例如新游戏时从 1 开始）。
        /// </summary>
        public static void Reset(long startValue = 1)
        {
            _nextBuildingID = Math.Max(startValue, 1L);
            if (_saveItem != null)
            {
                _saveItem.nextId = _nextBuildingID;
            }
        }

        /// <summary>
        /// 提供给 ScribeSaveManager 调用，用于注册这个条目类型到多态系统。
        /// </summary>
        public static void RegisterSaveType()
        {
            PolymorphRegistry.Register<SaveItem>(SaveItemTypeId);
        }

        // —— 兼容旧接口（可选）——
        // 如果你项目里还有旧保存逻辑，可以这样桥接到新的实现：
        // public static void LoadFromSavedData(long lastUsedID)
        // {
        //     EnsureInitialized();
        //     if (lastUsedID >= _nextBuildingID)
        //         _nextBuildingID = lastUsedID;

        //     if (_saveItem != null)
        //         _saveItem.nextId = _nextBuildingID;
        // }

        // public static void SaveLastUsedID(out long lastUsedID)
        // {
        //     EnsureInitialized();
        //     lastUsedID = _nextBuildingID;
        //     if (_saveItem != null)
        //         _saveItem.nextId = _nextBuildingID;
        // }

        // —— 内部工具 —— //
        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                // 如果你保证外面会显式调用 InitializeFromSave，可以直接 return；
                // 这里做个兜底，防止忘记初始化时也不会炸。
                InitializeFromSave();
            }
        }

        /// <summary>
        /// 存档条目：实际存储“下一次要使用的 ID”。
        /// 由 Scribe 系统负责序列化/反序列化。
        /// </summary>
        [Serializable]
        private sealed class SaveItem : ISaveItem
        {
            public string TypeId => SaveItemTypeId;

            public long nextId = 1;

            public void ExposeData()
            {
                // 用我们之前的 long Codec，通过泛型 Look 保存/读取
                Scribe.Scribe_Generic.Look("nextId", ref nextId, 1L, forceSave: true);
            }
        }
    }
}
