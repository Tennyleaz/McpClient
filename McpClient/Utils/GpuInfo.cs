using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Utils;

internal class GpuInfoLinux
{
    public string Name { get; set; }
    public int MemoryMiB { get; set; }
}

internal class GpuInfoWindows
{
    public string Name { get; set; }
    public long AdapterRAM { get; set; }
}

internal class PciIdsEntry
{
    public string VendorName;
    public Dictionary<string, PciDeviceEntry> Devices = new();
}

internal class PciDeviceEntry
{
    public string DeviceName;
    public Dictionary<string, string> Subsystems = new(); // key: "subvendor:subdev"
}
