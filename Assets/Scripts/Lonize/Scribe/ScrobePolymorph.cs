

using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Lonize.Scribe
{
     public static class Scribe_Polymorph
    {
        /// <summary>List&lt;ISaveItem&gt;：元素作为 Node 写入，前置写入 TypeId（string）</summary>
        public static void LookList(string tag, ref List<ISaveItem> list)
        {
            if (Scribe.mode == ScribeMode.Saving)
            {
                if (list == null) { Scribe.WriteTLV(FieldType.ListPoly, tag, w => w.Write(-1)); return; }

                var localList = list;
                Scribe.WriteTLV(FieldType.ListPoly, tag, w =>
                {
                    w.Write(localList.Count);
                    for (int i = 0; i < localList.Count; i++)
                    {
                        var it = localList[i];
                        var typeId = it?.TypeId ?? string.Empty;
                        w.Write(typeId);

                        if (it == null) { w.Write(0); continue; }

                        // 把条目内容写成一个 Node 的 TLV，序列化进列表 payload
                        using var elemBuf = new MemoryStream();
                        using var elemW   = new BinaryWriter(elemBuf);

                        // 临时把 writer 压到 Scribe 的栈顶，借助 NodeScope 写内部 TLV
                        var wsField = typeof(Scribe).GetField("_writerStack", BindingFlags.NonPublic|BindingFlags.Static);
                        var ws = (Stack<BinaryWriter>)wsField.GetValue(null);
                        ws.Push(elemW);
                        using (var node = new Scribe.NodeScope($"elem{i}"))
                            it.ExposeData();
                        ws.Pop();

                        var data = elemBuf.ToArray();
                        w.Write(data.Length);
                        w.Write(data);
                    }
                });
            }
            else if (Scribe.mode == ScribeMode.Loading)
            {
                if (!Scribe.TryGetField(tag, out var rec) || rec.Type != FieldType.ListPoly) { list = null; return; }

                using var br = new BinaryReader(new MemoryStream(rec.Payload));
                int n = br.ReadInt32();
                if (n < 0) { list = null; return; }
                list = new List<ISaveItem>(n);

                var frameField = typeof(Scribe).GetField("_frameStack", BindingFlags.NonPublic|BindingFlags.Static);
                var frames = (Stack<NodeFrame>)frameField.GetValue(null);

                for (int i = 0; i < n; i++)
                {
                    var typeId = br.ReadString();
                    int len = br.ReadInt32();
                    if (string.IsNullOrEmpty(typeId) || len == 0) { list.Add(null); continue; }

                    if (!PolymorphRegistry.TryCreate(typeId, out var obj))
                    {
                        // 未注册的类型：跳过 payload，放空位，避免崩
                        br.ReadBytes(len);
                        list.Add(null);
                        continue;
                    }

                    var buf = br.ReadBytes(len);
                    using var ms = new MemoryStream(buf);
                    using var r  = new BinaryReader(ms);
                    if (!TLV.TryRead(r, out var elemRec) || elemRec.Type != FieldType.Node) { list.Add(null); continue; }

                    // 压入子帧，ExposeData 读取其内部 TLV
                    frames.Push(NodeFrame.Parse(elemRec.Payload));
                    obj.ExposeData();
                    frames.Pop();

                    list.Add(obj);
                }
            }
            else throw new System.InvalidOperationException();
        }
    }
}