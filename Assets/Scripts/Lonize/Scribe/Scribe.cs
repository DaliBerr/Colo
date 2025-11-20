using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lonize.Scribe
{
    public enum ScribeMode { Inactive, Saving, Loading }

    public enum FieldType : byte
    {
        Null = 0,
        Int32 = 1,
        Single = 2,
        Bool = 3,
        String = 4,
        EnumInt32 = 5,
        Int64 = 6,

        DictStrInt = 10,
        DictStrFloat = 11,
        DictStrBool = 12,
        DictStrStr = 13,
        DictStrEnumInt32 = 14,

        Node = 20, // 对象/嵌套帧（其内部还是 TLV 序列）
        ListInt = 30,
        ListFloat = 31,
        ListBool = 32,
        ListStr = 33,
        ListDeep = 34, // payload 里是 N 个 Node 串联

        RefId = 40, // 引用（string id）
        ListRefId = 41,
        ListPoly = 63,
    }

    internal static class TLV
    {
        public static void Write(BinaryWriter bw, FieldType type, string tag, Action<BinaryWriter> writePayload)
        {
            using var buf = new MemoryStream();
            using var pw  = new BinaryWriter(buf);
            writePayload(pw);
            pw.Flush();
            var payload = buf.ToArray();

            bw.Write((byte)type);
            bw.Write(tag ?? string.Empty);
            bw.Write(payload.Length);
            bw.Write(payload);
        }

        public struct FieldRec
        {
            public FieldType Type;
            public string Tag;
            public byte[] Payload;
        }

        public static bool TryRead(BinaryReader br, out FieldRec rec)
        {
            rec = default;
            if (br.BaseStream.Position >= br.BaseStream.Length) return false;
            var t   = (FieldType)br.ReadByte();
            var tag = br.ReadString();
            var len = br.ReadInt32();
            var buf = br.ReadBytes(len);
            rec = new FieldRec { Type = t, Tag = tag, Payload = buf };
            return true;
        }
    }
    public interface IExposable { void ExposeData(); }

    public static class Scribe
    {
        public static ScribeMode mode { get; private set; } = ScribeMode.Inactive;
        internal static int fileVersion;

        // 写入
        private static BinaryWriter _rootWriter;
        private static readonly Stack<BinaryWriter> _writerStack = new();

        // 读取
        private static BinaryReader _rootReader;
        private static readonly Stack<NodeFrame> _frameStack = new();

        // ========== 生命周期 ==========
        public static void InitSaving(Stream stream, int version = 1)
        {
            if (mode != ScribeMode.Inactive) throw new InvalidOperationException("Scribe already active.");
            _rootWriter = new BinaryWriter(stream);
            mode = ScribeMode.Saving;
            fileVersion = version;
            _rootWriter.Write(fileVersion); // 文件头：版本
            _writerStack.Clear();
            _writerStack.Push(_rootWriter);
        }

        public static void InitLoading(Stream stream)
        {
            if (mode != ScribeMode.Inactive) throw new InvalidOperationException("Scribe already active.");
            _rootReader = new BinaryReader(stream);
            mode = ScribeMode.Loading;
            fileVersion = _rootReader.ReadInt32(); // 读版本
            _frameStack.Clear();

            // 把整个文件解析为“顶层帧”
            using var ms = new MemoryStream();
            var remain = (int)(_rootReader.BaseStream.Length - _rootReader.BaseStream.Position);
            ms.Write(_rootReader.ReadBytes(remain));
            ms.Position = 0;
            using var br = new BinaryReader(ms);
            var top = new NodeFrame();
            while (TLV.TryRead(br, out var rec))
            {
                // 直接塞进“顶层帧”的 map（与 NodeFrame 结构兼容）
                // 这里复用了 NodeFrame 的字典策略
                var f = typeof(NodeFrame).GetField("_map", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var map = (Dictionary<string, TLV.FieldRec>)f.GetValue(top);
                map[rec.Tag] = rec;
            }
            _frameStack.Push(top);
        }

        public static void FinalizeWriting()
        {
            if (mode != ScribeMode.Saving) return;
            _rootWriter?.Flush();
            _writerStack.Clear();
            _rootWriter = null;
            mode = ScribeMode.Inactive;
        }

        public static void FinalizeLoading()
        {
            if (mode != ScribeMode.Loading) return;
            _frameStack.Clear();
            _rootReader = null;
            mode = ScribeMode.Inactive;
        }

        // ========== Node 作用域（保存/读取都可用） ==========
        public sealed class NodeScope : IDisposable
        {
            private readonly string _tag;
            private readonly BinaryWriter _parentWriter;
            private readonly MemoryStream _buf;
            private readonly BinaryWriter _bw;
            private readonly bool _isSaving;
            private readonly NodeFrame _prevFrame;
            private bool _disposed;

            // 保存：在父 writer 上写一个 Node(TLV)，其 payload 就是 _buf
            public NodeScope(string tag)
            {
                _tag = tag;
                _isSaving = (mode == ScribeMode.Saving);
                if (_isSaving)
                {
                    _parentWriter = _writerStack.Peek();
                    _buf = new MemoryStream();
                    _bw = new BinaryWriter(_buf);
                    _writerStack.Push(_bw);
                }
                else
                {
                    // 加载：把当前帧里 tag 对应的 Node 解析成子帧，压栈
                    var cur = _frameStack.Peek();
                    _prevFrame = cur;
                    if (cur.TryGet(tag, out var rec) && rec.Type == FieldType.Node)
                        _frameStack.Push(NodeFrame.Parse(rec.Payload));
                    else
                        _frameStack.Push(NodeFrame.Parse(Array.Empty<byte>())); // 空帧
                }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                if (_isSaving)
                {
                    _writerStack.Pop(); // 弹出子 writer
                    _bw.Flush();
                    var payload = _buf.ToArray();
                    TLV.Write(_parentWriter, FieldType.Node, _tag, w => w.Write(payload));
                    _bw.Dispose();
                    _buf.Dispose();
                }
                else
                {
                    _frameStack.Pop(); // 弹出子帧
                }
            }
        }

        // ========== 根对象 Look ==========
        // 把根对象放进标签 "__root" 的 Node 里，内部再按字段 TLV 存
        public static void Look<T>(ref T obj) where T : class, IExposable, new()
        {
            if (mode == ScribeMode.Saving)
            {
                using var root = new NodeScope("__root");
                Scribe_Deep.Look("root", ref obj);
            }
            else if (mode == ScribeMode.Loading)
            {
                using var root = new NodeScope("__root");
                Scribe_Deep.Look("root", ref obj);
            }
            else throw new InvalidOperationException("Scribe not active.");
        }

        // 写 TLV 到“当前” writer
        internal static void WriteTLV(FieldType t, string tag, Action<BinaryWriter> writePayload)
        {
            TLV.Write(_writerStack.Peek(), t, tag, writePayload);
        }

        // 读“当前帧”里的字段
        internal static bool TryGetField(string tag, out TLV.FieldRec rec)
            => _frameStack.Peek().TryGet(tag, out rec);

        public static class Scribe_Generic
    {
        public static void Look<T>(string tag, ref T value, T defaultValue = default, bool forceSave = false)
        {
            var ty = typeof(T);

            // 类型门禁：遇到复杂对象，提示改用 Deep/Refs
            if (typeof(ILoadReferenceable).IsAssignableFrom(ty))
                throw new InvalidOperationException($"Type {ty.Name} is reference-like. Use Scribe_Refs.Look.");
            if (typeof(IExposable).IsAssignableFrom(ty))
                throw new InvalidOperationException($"Type {ty.Name} implements IExposable. Use Scribe_Deep.Look.");

            if (Scribe.mode == ScribeMode.Saving)
            {
                if (!forceSave)
                {
                    if (value == null && defaultValue == null) return;
                    if (value != null && value.Equals(defaultValue)) return;
                }

                if (value == null) { Scribe.WriteTLV(FieldType.Null, tag, w => { }); return; }

                if (!CodecRegistry.TryGet<T>(out var codec))
                    throw new NotSupportedException($"No codec registered for {ty.Name}.");

                // capture a local copy so we don't reference the ref parameter inside the lambda
                var toWrite = value;
                Scribe.WriteTLV(codec.FieldType, tag, w => codec.Write(w, in toWrite));
            }
            else if (Scribe.mode == ScribeMode.Loading)
            {
                if (!Scribe.TryGetField(tag, out var rec))
                {
                    value = defaultValue;
                    return;
                }
                if (rec.Type == FieldType.Null) { value = default; return; }

                if (!CodecRegistry.TryGet<T>(out var codec))
                    throw new NotSupportedException($"No codec registered for {ty.Name}.");

                using var br = new BinaryReader(new MemoryStream(rec.Payload));
                value = codec.Read(br);
            }
            else throw new InvalidOperationException("Scribe not active.");
        }

        // public static void LookRef<TRef>(string tag, ref TRef obj) where TRef : class, ILoadReferenceable
        //     => Scribe_Refs.Look(tag, ref obj);

        public static void LookDeep<TDeep>(string tag, ref TDeep obj) where TDeep : class, IExposable, new()
            => Scribe_Deep.Look(tag, ref obj);
        }
    }
}
