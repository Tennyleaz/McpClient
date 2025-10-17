using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteVectorStore;

internal class VectorHelpers
{
    public static byte[] FloatArrayToBytes(float[] floats)
    {
        byte[] result = new byte[floats.Length * 4];
        Buffer.BlockCopy(floats, 0, result, 0, result.Length);
        return result;
    }

    public static float[] BytesToFloatArray(byte[] bytes)
    {
        float[] floats = new float[bytes.Length / 4];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }

    public static float CosineSimilarity(float[] v1, float[] v2)
    {
        float dot = 0, norm1 = 0, norm2 = 0;
        for (int i = 0; i < v1.Length; i++)
        {
            dot += v1[i] * v2[i];
            norm1 += v1[i] * v1[i];
            norm2 += v2[i] * v2[i];
        }
        return dot / ((float)Math.Sqrt(norm1) * (float)Math.Sqrt(norm2) + 1e-10f);
    }
}
