using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Utils;

internal enum GgufMetadataValueType : uint
{
    UINT8 = 0,
    INT8 = 1,
    UINT16 = 2,
    INT16 = 3,
    UINT32 = 4,
    INT32 = 5,
    FLOAT32 = 6,
    BOOL = 7,
    STRING = 8,
    ARRAY = 9,
    UINT64 = 10,
    INT64 = 11,
    FLOAT64 = 12,
}

internal class GGUFMetadataKV
{
    public string Key;
    public GgufMetadataValueType ValueType;
    public object Value;

    public override string ToString()
    {
        return $"{Key}: {Value}";
    }
}

internal class GGUFHeader
{
    public uint Magic;
    public uint Version;
    public ulong TensorCount;
    public ulong MetadataKVCount;
    public List<GGUFMetadataKV> Metadata = new List<GGUFMetadataKV>();
}

internal class GGUFReader
{
    public GGUFHeader Header;

    public GGUFReader(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        Header = new GGUFHeader
        {
            Magic = br.ReadUInt32(),
            Version = br.ReadUInt32(),
            TensorCount = br.ReadUInt64(),
            MetadataKVCount = br.ReadUInt64()
        };

        if (Header.Magic != 0x46554747) // "GGUF" in little-endian
            throw new Exception("Not a GGUF file.");

        for (ulong i = 0; i < Header.MetadataKVCount; i++)
        {
            string key = ReadGGUFString(br);
            GgufMetadataValueType valueType = (GgufMetadataValueType)br.ReadUInt32();
            object value = ReadGGUFValue(br, valueType);

            Header.Metadata.Add(new GGUFMetadataKV
            {
                Key = key,
                ValueType = valueType,
                Value = value
            });
        }

        br.Close();
        fs.Close();
    }

    private string ReadGGUFString(BinaryReader br)
    {
        ulong len = br.ReadUInt64();
        byte[] bytes = br.ReadBytes((int)len);
        return Encoding.UTF8.GetString(bytes);
    }

    private object ReadGGUFValue(BinaryReader br, GgufMetadataValueType valueType)
    {
        switch (valueType)
        {
            case GgufMetadataValueType.UINT8: return br.ReadByte();
            case GgufMetadataValueType.INT8: return br.ReadSByte();
            case GgufMetadataValueType.UINT16: return br.ReadUInt16();
            case GgufMetadataValueType.INT16: return br.ReadInt16();
            case GgufMetadataValueType.UINT32: return br.ReadUInt32();
            case GgufMetadataValueType.INT32: return br.ReadInt32();
            case GgufMetadataValueType.UINT64: return br.ReadUInt64();
            case GgufMetadataValueType.INT64: return br.ReadInt64();
            case GgufMetadataValueType.FLOAT32: return br.ReadSingle();
            case GgufMetadataValueType.FLOAT64: return br.ReadDouble();
            case GgufMetadataValueType.BOOL:
                {
                    byte b = br.ReadByte();
                    return b == 1;
                }
            case GgufMetadataValueType.STRING:
                return ReadGGUFString(br);
            case GgufMetadataValueType.ARRAY:
                {
                    GgufMetadataValueType elementType = (GgufMetadataValueType)br.ReadUInt32();
                    ulong len = br.ReadUInt64();
                    var arr = new object[len];
                    for (ulong i = 0; i < len; i++)
                        arr[i] = ReadGGUFValue(br, elementType);
                    return arr;
                }
            default: throw new NotSupportedException($"Value type {valueType} not supported.");
        }
    }

    public void PrintMetadata()
    {
        foreach (var kv in Header.Metadata)
        {
            Console.WriteLine($"{kv.Key} ({kv.ValueType}): {ValueToString(kv.Value)}");
        }
    }

    private string ValueToString(object val)
    {
        if (val is Array arr)
        {
            var elems = new List<string>();
            foreach (var v in arr) elems.Add(ValueToString(v));
            return "[" + string.Join(", ", elems) + "]";
        }
        return val?.ToString() ?? "null";
    }
}