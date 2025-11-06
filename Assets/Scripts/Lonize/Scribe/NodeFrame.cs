

using System.Collections.Generic;
using System.IO;

namespace Lonize.Scribe
{
        internal sealed class NodeFrame
    {
        // 一个 tag 只保留“最后一次出现”的字段，足够覆盖“改名/新增/替换”的常见场景
        private readonly Dictionary<string, TLV.FieldRec> _map = new();

        public static NodeFrame Parse(byte[] payload)
        {
            var nf = new NodeFrame();
            using var ms = new MemoryStream(payload, writable: false);
            using var br = new BinaryReader(ms);
            while (TLV.TryRead(br, out var rec))
                nf._map[rec.Tag] = rec;
            return nf;
        }

        public bool TryGet(string tag, out TLV.FieldRec rec) => _map.TryGetValue(tag, out rec);
    }
}