using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Utils;

internal enum RuntimeType
{
    CUDA,   // Nvidia CUDA windows/linux
    Radeon, // AMD ROCm linux (HIP)
    Vulkan, // Windows/linux
    CPU,    // Any OS
    Metal,  // MacOS on GPU
    OpenCL, // Qualcomm Adreno GPU, arm64
    SYCL,   // Intel Arc GPU
    IPEX,   // Intel GPU and NPU
    Other
}

internal class LlamaDevice
{
    public string Backend { get; set; }      // e.g. "CUDA"
    public int Index { get; set; }           // e.g. 0
    public string Name { get; set; }         // e.g. "NVIDIA P104-100"
    public int? TotalMemoryMiB { get; set; }
    public int? FreeMemoryMiB { get; set; }
    public bool IsCPU => Backend == "CPU";
    public override string ToString()
    {
        if (IsCPU) return "CPU";
        if (TotalMemoryMiB.HasValue)
            return $"{Backend}{Index}: {Name} ({TotalMemoryMiB} MiB{(FreeMemoryMiB.HasValue ? $", {FreeMemoryMiB} MiB free" : "")})";
        return $"{Backend}{Index}: {Name}";
    }
}