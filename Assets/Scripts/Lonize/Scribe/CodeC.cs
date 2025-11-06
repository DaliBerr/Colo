using System;
using System.Collections.Generic;
using System.IO;
using Lonize.Scribe;


namespace Lonize.Scribe
{
    public interface ICodec<T>
    {
        FieldType FieldType { get; }            // 该类型使用的 TLV 类型码
        void Write(BinaryWriter w, in T value); // 写 payload
        T Read(BinaryReader r);                 // 读 payload
    }
    public static class CodecRegistry
    {
        private static readonly Dictionary<Type, object> _map = new();

        public static void Register<T>(ICodec<T> c) => _map[typeof(T)] = c;

        public static bool TryGet<T>(out ICodec<T> c)
        {
            if (_map.TryGetValue(typeof(T), out var o)) { c = (ICodec<T>)o; return true; }
            c = null; return false;
        }
    }

    // 注册内置类型的 Codec
    // Bool
public sealed class BoolCodec : ICodec<bool>
{
    public FieldType FieldType => FieldType.Bool;
    public void Write(BinaryWriter w, in bool v) => w.Write(v);
    public bool Read(BinaryReader r) => r.ReadBoolean();
}

// Int
public sealed class IntCodec : ICodec<int>
{
    public FieldType FieldType => FieldType.Int32;
    public void Write(BinaryWriter w, in int v) => w.Write(v);
    public int Read(BinaryReader r) => r.ReadInt32();
}

// Float
public sealed class FloatCodec : ICodec<float>
{
    public FieldType FieldType => FieldType.Single;
    public void Write(BinaryWriter w, in float v) => w.Write(v);
    public float Read(BinaryReader r) => r.ReadSingle();
}

// String (null 支持)
public sealed class StringCodec : ICodec<string>
{
    public FieldType FieldType => FieldType.String;
    public void Write(BinaryWriter w, in string v)
    {
        bool has = v != null; w.Write(has); if (has) w.Write(v);
    }
    public string Read(BinaryReader r)
    {
        bool has = r.ReadBoolean(); return has ? r.ReadString() : null;
    }
}

// Enum<T> （统一写成 Int32）
public sealed class EnumCodec<T> : ICodec<T> where T : struct, Enum
{
    public FieldType FieldType => FieldType.EnumInt32;
    public void Write(BinaryWriter w, in T v) => w.Write(Convert.ToInt32(v));
    public T Read(BinaryReader r) => (T)Enum.ToObject(typeof(T), r.ReadInt32());
}

    // Dictionary<string,string>
    public sealed class DictStrStrCodec : ICodec<Dictionary<string, string>>
    {
        public FieldType FieldType => FieldType.DictStrStr; // 之前已定义
        public void Write(BinaryWriter w, in Dictionary<string, string> dict)
        {
            if (dict == null) { w.Write(-1); return; }
            w.Write(dict.Count);
            foreach (var kv in dict)
            {
                w.Write(kv.Key ?? string.Empty);
                bool has = kv.Value != null; w.Write(has); if (has) w.Write(kv.Value);
            }
        }
        public Dictionary<string, string> Read(BinaryReader r)
        {
            int n = r.ReadInt32(); if (n < 0) return null;
            var d = new Dictionary<string, string>(n);
            for (int i = 0; i < n; i++) { var k = r.ReadString(); bool has = r.ReadBoolean(); d[k] = has ? r.ReadString() : null; }
            return d;
        }

        
    }
    // Dictionary<string,float>
    public sealed class DictStrFloatCodec : ICodec<Dictionary<string, float>>
    {
        public FieldType FieldType => FieldType.DictStrFloat; // 之前已定义
        public void Write(BinaryWriter w, in Dictionary<string, float> dict)
        {
            if (dict == null) { w.Write(-1); return; }
            w.Write(dict.Count);
            foreach (var kv in dict)
            {
                w.Write(kv.Key ?? string.Empty);
                w.Write(kv.Value);
            }
        }
        public Dictionary<string, float> Read(BinaryReader r)
        {
            int n = r.ReadInt32(); if (n < 0) return null;
            var d = new Dictionary<string, float>(n);
            for (int i = 0; i < n; i++) { var k = r.ReadString(); var v = r.ReadSingle(); d[k] = v; }
            return d;
        }
    }
    // Dictionary<string,EnumInt32>
    public sealed class DictStrEnumInt32Codec<T> : ICodec<Dictionary<string, T>> where T : struct, Enum
    {
        public FieldType FieldType => FieldType.DictStrEnumInt32; // 之前已定义
        public void Write(BinaryWriter w, in Dictionary<string, T> dict)
        {
            if (dict == null) { w.Write(-1); return; }
            w.Write(dict.Count);
            foreach (var kv in dict)
            {
                w.Write(kv.Key ?? string.Empty);
                w.Write(Convert.ToInt32(kv.Value));
            }
        }
        public Dictionary<string, T> Read(BinaryReader r)
        {
            int n = r.ReadInt32(); if (n < 0) return null;
            var d = new Dictionary<string, T>(n);
            for (int i = 0; i < n; i++)
            {
                var k = r.ReadString();
                var v = (T)Enum.ToObject(typeof(T), r.ReadInt32());
                d[k] = v;
            }
            return d;
        }
    }
}