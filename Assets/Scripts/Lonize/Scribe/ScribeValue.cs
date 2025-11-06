using System;
using System.Collections.Generic;
using System.IO;

namespace Lonize.Scribe
{
    public static class Scribe_Values
    {
        public static void Look(string tag, ref int value, int defaultValue = 0)
        {
            if (Scribe.mode == ScribeMode.Saving)
            {
                var tmp = value;
                Scribe.WriteTLV(FieldType.Int32, tag, w => w.Write(tmp));
            }
            else if (Scribe.mode == ScribeMode.Loading)
            {
                if (Scribe.TryGetField(tag, out var rec) && rec.Type == FieldType.Int32)
                {
                    using var br = new BinaryReader(new MemoryStream(rec.Payload));
                    value = br.ReadInt32();
                }
                else value = defaultValue;
            }
            else throw new InvalidOperationException();
        }

        public static void Look(string tag, ref float value, float defaultValue = 0f)
        {
            if (Scribe.mode == ScribeMode.Saving)
            {
                var tmp = value;
                Scribe.WriteTLV(FieldType.Single, tag, w => w.Write(tmp));
            }
            else if (Scribe.mode == ScribeMode.Loading)
            {
                if (Scribe.TryGetField(tag, out var rec) && rec.Type == FieldType.Single)
                {
                    using var br = new BinaryReader(new MemoryStream(rec.Payload));
                    value = br.ReadSingle();
                }
                else value = defaultValue;
            }
        }

        public static void Look(string tag, ref bool value, bool defaultValue = false)
        {
            if (Scribe.mode == ScribeMode.Saving)
            {
                var tmp = value;
                Scribe.WriteTLV(FieldType.Bool, tag, w => w.Write(tmp));
            }
            else if (Scribe.mode == ScribeMode.Loading)
            {
                if (Scribe.TryGetField(tag, out var rec) && rec.Type == FieldType.Bool)
                {
                    using var br = new BinaryReader(new MemoryStream(rec.Payload));
                    value = br.ReadBoolean();
                }
                else value = defaultValue;
            }
        }

        public static void Look(string tag, ref string value, string defaultValue = null)
        {
            if (Scribe.mode == ScribeMode.Saving)
            {
                if (value == null) Scribe.WriteTLV(FieldType.Null, tag, w => { });
                else
                {
                    var tmp = value;
                    Scribe.WriteTLV(FieldType.String, tag, w => w.Write(tmp));
                }
            }
            else if (Scribe.mode == ScribeMode.Loading)
            {
                if (!Scribe.TryGetField(tag, out var rec))
                {
                    value = defaultValue;
                }
                else if (rec.Type == FieldType.Null)
                {
                    value = null;
                }
                else if (rec.Type == FieldType.String)
                {
                    using var br = new BinaryReader(new MemoryStream(rec.Payload));
                    value = br.ReadString();
                }
                else value = defaultValue;
            }
        }

        public static void LookEnum<TEnum>(string tag, ref TEnum value, TEnum defaultValue = default)
            where TEnum : struct, Enum
        {
            if (Scribe.mode == ScribeMode.Saving)
            {
                var tmp = value;
                Scribe.WriteTLV(FieldType.EnumInt32, tag, w => w.Write(Convert.ToInt32(tmp)));
            }
            else if (Scribe.mode == ScribeMode.Loading)
            {
                if (Scribe.TryGetField(tag, out var rec) && rec.Type == FieldType.EnumInt32)
                {
                    using var br = new BinaryReader(new MemoryStream(rec.Payload));
                    value = (TEnum)Enum.ToObject(typeof(TEnum), br.ReadInt32());
                }
                else value = defaultValue;
            }
        }
    }
}