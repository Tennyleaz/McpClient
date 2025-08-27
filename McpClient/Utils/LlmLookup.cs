using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Utils;

internal class LlmLookup
{
    private static readonly LlmRecommendation[] Recommendations =
    [
        new LlmRecommendation(0, 2048, "<2B", 1, "Q4_0", "Very small models"),
        new LlmRecommendation(0, 3072, "3B", 3, "Q4_0", "Very small models"),
        new LlmRecommendation(3072, 5120, "4B", 6, "Q4_0", "Standard LLaMA/Alpaca 7B size"),
        new LlmRecommendation(5120, 6144, "7B", 9, "Q4_0", "Full 13B models"),
        new LlmRecommendation(6144, 8192, "13B",  12,"Q4_0", "Full 13B models"),
        new LlmRecommendation(8192, 12288, "14B", 24, "Q4_0", "High-end consumer, short context"),
        new LlmRecommendation(12288, 16384, "33B", 32, "Q4_0", "Modern high-end cards"),
        new LlmRecommendation(16384, 24576, "65B", 64, "Q4_0", "Modern high-end cards"),
        new LlmRecommendation(24576, 99999, "70B+", -1, "Q5 or fp16", "Top-end pro cards"),
    ];

    public static LlmRecommendation GetRecommendation(int vramMb)
    {
        foreach (LlmRecommendation r in Recommendations)
        {
            if (vramMb > r.MinVramMb && vramMb <= r.MaxVramMb)
            {
                return r;
            }
        }

        return Recommendations.Last();
    }
}

internal record LlmRecommendation(int MinVramMb, int MaxVramMb, string MaxModel, int hugginFaceSize, string Quantization, string Notes);