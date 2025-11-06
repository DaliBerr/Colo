using System.Collections.Generic;
using System.IO;

namespace Lonize.Scribe
{
        public static class Scribe_Collections
    {
        public static void Look(string tag, ref List<int> list)
        {
            if (Scribe.mode == ScribeMode.Saving)
            {
                if (list == null) { Scribe.WriteTLV(FieldType.ListInt, tag, w => w.Write(-1)); return; }
                var items = list.ToArray();
                Scribe.WriteTLV(FieldType.ListInt, tag, w =>
                {
                    w.Write(items.Length);
                    for (int i = 0; i < items.Length; i++) w.Write(items[i]);
                });
            }
            else if (Scribe.mode == ScribeMode.Loading)
            {
                if (!Scribe.TryGetField(tag, out var rec) || rec.Type != FieldType.ListInt) { list = null; return; }
                using var br = new BinaryReader(new MemoryStream(rec.Payload));
                int n = br.ReadInt32();
                if (n < 0) { list = null; return; }
                list = new List<int>(n);
                for (int i = 0; i < n; i++) list.Add(br.ReadInt32());
            }
        }
        public static void Look(string tag, ref List<string> list)
        {
            if (Scribe.mode == ScribeMode.Saving)
            {
                if (list == null) { Scribe.WriteTLV(FieldType.ListStr, tag, w => w.Write(-1)); return; }
                var items = list.ToArray();
                Scribe.WriteTLV(FieldType.ListStr, tag, w =>
                {
                    w.Write(items.Length);
                    for (int i = 0; i < items.Length; i++)
                    {
                        bool has = items[i] != null;
                        w.Write(has);
                        if (has) w.Write(items[i]);
                    }
                });
            }
            else if (Scribe.mode == ScribeMode.Loading)
            {
                if (!Scribe.TryGetField(tag, out var rec) || rec.Type != FieldType.ListStr) { list = null; return; }
                using var br = new BinaryReader(new MemoryStream(rec.Payload));
                int n = br.ReadInt32();
                if (n < 0) { list = null; return; }
                list = new List<string>(n);
                for (int i = 0; i < n; i++)
                {
                    bool has = br.ReadBoolean();
                    list.Add(has ? br.ReadString() : null);
                }
            }
        }
        public static void LookDeep<T>(string tag, ref List<T> list) where T : class, IExposable, new()
        {
            if (Scribe.mode == ScribeMode.Saving)
            {
                if (list == null) { Scribe.WriteTLV(FieldType.ListDeep, tag, w => w.Write(-1)); return; }
                var items = list.ToArray();
                Scribe.WriteTLV(FieldType.ListDeep, tag, w =>
                {
                    w.Write(items.Length);
                    for (int i = 0; i < items.Length; i++)
                    {
                        var item = items[i];
                        if (item == null) { w.Write(0); continue; }
                        // 每个元素写成一个 Node 的 TLV（嵌在 List 的 payload 内）
                        using var elemBuf = new MemoryStream();
                        using var elemW   = new BinaryWriter(elemBuf);
                        // 临时把 writer 压栈，复用 NodeScope 写内部 TLV
                        var topWriter = typeof(Scribe).GetField("_writerStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                                        .GetValue(null) as Stack<BinaryWriter>;
                        topWriter.Push(elemW);
                        using (var node = new Scribe.NodeScope($"elem{i}")) { item.ExposeData(); }
                        topWriter.Pop();

                        var data = elemBuf.ToArray();
                        w.Write(data.Length);
                        w.Write(data);
                    }
                });
            }
            else if (Scribe.mode == ScribeMode.Loading)
            {
                if (!Scribe.TryGetField(tag, out var rec) || rec.Type != FieldType.ListDeep) { list = null; return; }
                using var br = new BinaryReader(new MemoryStream(rec.Payload));
                int n = br.ReadInt32();
                if (n < 0) { list = null; return; }
                list = new List<T>(n);
                for (int i = 0; i < n; i++)
                {
                    int len = br.ReadInt32();
                    if (len == 0) { list.Add(null); continue; }
                    var buf = br.ReadBytes(len);
                    using var ms = new MemoryStream(buf);
                    using var r  = new BinaryReader(ms);
                    // 解析一个“单节点序列”（内部其实是1个 Node TLV）
                    TLV.FieldRec elemRec;
                    if (!TLV.TryRead(r, out elemRec) || elemRec.Type != FieldType.Node) { list.Add(null); continue; }
                    // 临时压栈子帧
                    var frameStackField = typeof(Scribe).GetField("_frameStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    var frames = (Stack<NodeFrame>)frameStackField.GetValue(null);
                    frames.Push(NodeFrame.Parse(elemRec.Payload));
                    var t = new T(); t.ExposeData();
                    frames.Pop();
                    list.Add(t);
                }
            }
        }
    }
}