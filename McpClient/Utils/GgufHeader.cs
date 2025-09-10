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

    public static string LlamaTypeToString(LlamaFileType ftype)
    {
        switch (ftype)
        {
            case LlamaFileType.LLAMA_FTYPE_ALL_F32: return "all F32";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_F16: return "F16";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_BF16: return "BF16";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_Q4_0: return "Q4_0";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_Q4_1: return "Q4_1";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_Q5_0: return "Q5_0";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_Q5_1: return "Q5_1";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_Q8_0: return "Q8_0";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_MXFP4_MOE: return "MXFP4 MoE";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_Q2_K: return "Q2_K - Medium";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_Q2_K_S: return "Q2_K - Small";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_Q3_K_S: return "Q3_K - Small";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_Q3_K_M: return "Q3_K - Medium";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_Q3_K_L: return "Q3_K - Large";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_Q4_K_S: return "Q4_K - Small";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_Q4_K_M: return "Q4_K - Medium";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_Q5_K_S: return "Q5_K - Small";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_Q5_K_M: return "Q5_K - Medium";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_Q6_K: return "Q6_K";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_TQ1_0: return "TQ1_0 - 1.69 bpw ternary";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_TQ2_0: return "TQ2_0 - 2.06 bpw ternary";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_IQ2_XXS: return "IQ2_XXS - 2.0625 bpw";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_IQ2_XS: return "IQ2_XS - 2.3125 bpw";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_IQ2_S: return "IQ2_S - 2.5 bpw";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_IQ2_M: return "IQ2_M - 2.7 bpw";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_IQ3_XS: return "IQ3_XS - 3.3 bpw";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_IQ3_XXS: return "IQ3_XXS - 3.0625 bpw";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_IQ1_S: return "IQ1_S - 1.5625 bpw";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_IQ1_M: return "IQ1_M - 1.75 bpw";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_IQ4_NL: return "IQ4_NL - 4.5 bpw";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_IQ4_XS: return "IQ4_XS - 4.25 bpw";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_IQ3_S: return "IQ3_S - 3.4375 bpw";
            case LlamaFileType.LLAMA_FTYPE_MOSTLY_IQ3_M: return "IQ3_S mix - 3.66 bpw";

            default: return "unknown, may not work";
        }
    }
}

internal enum LlamaFileType
{
    LLAMA_FTYPE_ALL_F32 = 0,
    LLAMA_FTYPE_MOSTLY_F16 = 1,  // except 1d tensors
    LLAMA_FTYPE_MOSTLY_Q4_0 = 2,  // except 1d tensors
    LLAMA_FTYPE_MOSTLY_Q4_1 = 3,  // except 1d tensors
    // LLAMA_FTYPE_MOSTLY_Q4_1_SOME_F16 = 4,  // tok_embeddings.weight and output.weight are F16
    // LLAMA_FTYPE_MOSTLY_Q4_2       = 5,  // support has been removed
    // LLAMA_FTYPE_MOSTLY_Q4_3       = 6,  // support has been removed
    LLAMA_FTYPE_MOSTLY_Q8_0 = 7,  // except 1d tensors
    LLAMA_FTYPE_MOSTLY_Q5_0 = 8,  // except 1d tensors
    LLAMA_FTYPE_MOSTLY_Q5_1 = 9,  // except 1d tensors
    LLAMA_FTYPE_MOSTLY_Q2_K = 10, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_Q3_K_S = 11, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_Q3_K_M = 12, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_Q3_K_L = 13, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_Q4_K_S = 14, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_Q4_K_M = 15, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_Q5_K_S = 16, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_Q5_K_M = 17, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_Q6_K = 18, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_IQ2_XXS = 19, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_IQ2_XS = 20, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_Q2_K_S = 21, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_IQ3_XS = 22, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_IQ3_XXS = 23, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_IQ1_S = 24, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_IQ4_NL = 25, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_IQ3_S = 26, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_IQ3_M = 27, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_IQ2_S = 28, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_IQ2_M = 29, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_IQ4_XS = 30, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_IQ1_M = 31, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_BF16 = 32, // except 1d tensors
    //LLAMA_FTYPE_MOSTLY_Q4_0_4_4      = 33, // removed from gguf files, use Q4_0 and runtime repack
    //LLAMA_FTYPE_MOSTLY_Q4_0_4_8      = 34, // removed from gguf files, use Q4_0 and runtime repack
    //LLAMA_FTYPE_MOSTLY_Q4_0_8_8      = 35, // removed from gguf files, use Q4_0 and runtime repack
    LLAMA_FTYPE_MOSTLY_TQ1_0 = 36, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_TQ2_0 = 37, // except 1d tensors
    LLAMA_FTYPE_MOSTLY_MXFP4_MOE = 38, // except 1d tensors

    LLAMA_FTYPE_GUESSED = 1024, // not specified in the model file
}