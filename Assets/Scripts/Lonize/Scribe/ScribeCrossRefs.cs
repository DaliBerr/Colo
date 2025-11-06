using System;
using System.Collections.Generic;
using System.IO;

namespace Lonize.Scribe
{
    public interface ILoadReferenceable
    {
        string GetSaveId(); // 保存时需要输出稳定ID
    }

    public static class ScribeRefs
    {
        private static readonly Dictionary<string, object> _id2obj = new();
        private static readonly List<Action> _pending = new();

        public static void Clear()
        {
            _id2obj.Clear();
            _pending.Clear();
        }

        public static void Register(string id, object obj)
        {
            if (string.IsNullOrEmpty(id) || obj == null) return;
            _id2obj[id] = obj;
        }

        public static void AddResolve<T>(string id, Action<T> setter) where T : class
        {
            _pending.Add(() =>
            {
                if (_id2obj.TryGetValue(id, out var o)) setter(o as T);
                else setter(null); // 找不到 → 置空或在此报错
            });
        }

        public static void ResolveAll()
        {
            for (int i = 0; i < _pending.Count; i++) _pending[i]();
            _pending.Clear();
        }
    }

    public static class Scribe_Refs
    {
        // 单引用 (使用 getter/setter delegate 避免在 lambda 中捕获 ref 参数)
        public static void Look<T>(string tag, Func<T> getter, Action<T> setter, bool allowNull = true)
            where T : class, ILoadReferenceable
        {
            if (Scribe.mode == ScribeMode.Saving)
            {
                var target = getter();
                string idToWrite = target == null ? string.Empty : target.GetSaveId() ?? string.Empty;
                Scribe.WriteTLV(FieldType.RefId, tag, w => w.Write(idToWrite));
            }
            else if (Scribe.mode == ScribeMode.Loading)
            {
                if (!Scribe.TryGetField(tag, out var rec) || rec.Type != FieldType.RefId)
                {
                    if (!allowNull) setter(null);
                    return;
                }
                using var br = new BinaryReader(new MemoryStream(rec.Payload));
                var id = br.ReadString();
                if (string.IsNullOrEmpty(id)) { setter(null); return; }
                // 延后解析
                ScribeRefs.AddResolve<T>(id, o => setter(o));
            }
        }

        // 引用列表
        public static void LookList<T>(string tag, ref List<T> list)
        where T : class, ILoadReferenceable
        {
            if (Scribe.mode == ScribeMode.Saving)
            {
                // 原逻辑不变
            }
            else if (Scribe.mode == ScribeMode.Loading)
            {
                if (!Scribe.TryGetField(tag, out var rec) || rec.Type != FieldType.ListRefId) { list = null; return; }
                using var br = new BinaryReader(new MemoryStream(rec.Payload));
                int n = br.ReadInt32();
                if (n < 0) { list = null; return; }

                // 先建出 list 并填充占位
                list = new List<T>(n);
                for (int i = 0; i < n; i++) list.Add(null);

                // 拷贝到本地变量，避免在 lambda 中捕获 ref 参数
                var localList = list;

                for (int i = 0; i < n; i++)
                {
                    var id = br.ReadString();
                    int idx = i;
                    if (string.IsNullOrEmpty(id)) continue;
                    ScribeRefs.AddResolve<T>(id, o => localList[idx] = o); // 直接写回 list 的本地引用
                }
            }
        }

        public static class Scribe_Meta
        {
            // 读写对象的保存ID，并在加载时自动注册
            public static void HandleSaveId(object self, string tag, ref string id)
            {
                if (Scribe.mode == ScribeMode.Saving)
                {
                    Scribe_Values.Look(tag, ref id, null);
                }
                else if (Scribe.mode == ScribeMode.Loading)
                {
                    Scribe_Values.Look(tag, ref id, null);
                    if (!string.IsNullOrEmpty(id)) ScribeRefs.Register(id, self);
                }
            }
        }
    }
}