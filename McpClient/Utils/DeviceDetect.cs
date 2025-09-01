using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace McpClient.Utils;

internal class DeviceDetect
{
    #region Linux

    public static List<GpuInfoLinux> GetGpuInfoLinux()
    {
        List<GpuInfoLinux> gpus = new List<GpuInfoLinux>();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string nvidia = RunCommand("nvidia-smi", "--query-gpu=name,memory.total --format=csv,noheader");
            if (!string.IsNullOrWhiteSpace(nvidia) && !nvidia.Contains("not found"))
            {
                /*
                   ~$ nvidia-smi --query-gpu=name,memory.total --format=csv,noheader
                   NVIDIA RTX 4000 Ada Generation, 20475 MiB
                   NVIDIA RTX 4000 Ada Generation, 20475 MiB
                   NVIDIA RTX 4000 Ada Generation, 20475 MiB
                   NVIDIA RTX 4000 Ada Generation, 20475 MiB
                 */
                gpus.AddRange(ParseNvidiaSmiCsv(nvidia));
            }

            // load the pci.id file
            string text = System.IO.File.ReadAllText("pci.ids");
            Dictionary<string, PciIdsEntry> dict = ParsePciIds(text);

            // lspci -nnD | grep -E 'VGA|Display'
            // 0000:03:00.0 VGA compatible controller [0300]: Advanced Micro Devices, Inc. [AMD/ATI] Device [1002:7550] (rev c3)
            // 0000:00:02.0 Display controller[0380]: Intel Corporation CoffeeLake - S GT2[UHD Graphics 630][8086:3e92]
            string outputLines = RunCommand("lspci", "-nnD");
            string[] lines = outputLines.Split('\n');
            foreach (string line in lines)
            {
                if (!line.Contains("VGA") && !line.Contains("Display"))
                    continue;

                var match = Regex.Match(line, @"^(?<addr>[0-9a-f:.]+).*\[(?<vendor>[0-9a-fA-F]{4}):(?<device>[0-9a-fA-F]{4})\]");
                if (match.Success)
                {
                    string address = match.Groups["addr"].Value.ToLower();
                    string vendorId = match.Groups["vendor"].Value.ToLower();
                    string deviceId = match.Groups["device"].Value.ToLower();
                    string gpuName = LookupGpu(dict, vendorId, deviceId);
                    if (vendorId == "1002")
                    {
                        // Find in /sys/bus/pci/devices/PCI_ADDR/mem_info_vram_total
                        string path = $"/sys/bus/pci/devices/{address}/mem_info_vram_total";
                        if (System.IO.File.Exists(path))
                        {
                            string txt = System.IO.File.ReadAllText(path).Trim();
                            if (long.TryParse(txt, out long bytes))
                            {
                                gpus.Add(new GpuInfoLinux
                                {
                                    Name = gpuName,
                                    MemoryMiB = (int)(bytes / 1024)
                                });
                            }
                        }
                        else
                        {
                            Console.WriteLine("path does not exist: " + path);
                        }
                    }
                    else if (vendorId == "8086")
                    {
                        // TODO: intel arc vram?
                        gpus.Add(new GpuInfoLinux
                        {
                            Name = gpuName,
                            MemoryMiB = 0
                        });
                    }
                }
                else
                {

                }
            }
        }

        return gpus;
    }

    public static List<GpuInfoLinux> ParseNvidiaSmiCsv(string nvidiaSmiOutput)
    {
        var result = new List<GpuInfoLinux>();
        var lines = nvidiaSmiOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // Each line is 'GPU Name, XXXX MiB'
            var parts = line.Trim().Split(',', 2);
            if (parts.Length < 2) continue;

            // Remove any whitespace from memory part and extract number before " MiB"
            string name = parts[0].Trim();
            string memPart = parts[1].Trim();
            int miB = 0;

            var miBstr = memPart.Split(' ')[0];
            if (int.TryParse(miBstr, out miB))
            {
                result.Add(new GpuInfoLinux { Name = name, MemoryMiB = miB });
            }
        }
        return result;
    }
    
    private static Dictionary<string, PciIdsEntry> ParsePciIds(string pciIdsText)
    {
        var vendors = new Dictionary<string, PciIdsEntry>();
        string currentVendor = null;
        string currentDevice = null;
        var vendorRegex = new Regex(@"^([0-9a-fA-F]{4})\s+(.+)$");
        var deviceRegex = new Regex(@"^\t([0-9a-fA-F]{4})\s+(.+)$");
        var subsysRegex = new Regex(@"^\t\t([0-9a-fA-F]{4})\s+([0-9a-fA-F]{4})\s+(.+)$");
        foreach (var line in pciIdsText.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#")) 
                continue;
            var vMatch = vendorRegex.Match(line);
            if (vMatch.Success)
            {
                currentVendor = vMatch.Groups[1].Value.ToLower();
                vendors[currentVendor] = new PciIdsEntry
                {
                    VendorName = vMatch.Groups[2].Value.Trim()
                };
                currentDevice = null;
                continue;
            }
            var dMatch = deviceRegex.Match(line);
            if (dMatch.Success && currentVendor != null)
            {
                currentDevice = dMatch.Groups[1].Value.ToLower();
                vendors[currentVendor].Devices[currentDevice] = new PciDeviceEntry
                {
                    DeviceName = dMatch.Groups[2].Value.Trim()
                };
                continue;
            }
            var sMatch = subsysRegex.Match(line);
            if (sMatch.Success && currentVendor != null && currentDevice != null)
            {
                string subsysKey = $"{sMatch.Groups[1].Value.ToLower()}:{sMatch.Groups[2].Value.ToLower()}";
                vendors[currentVendor].Devices[currentDevice].Subsystems[subsysKey] = sMatch.Groups[3].Value.Trim();
            }
        }
        return vendors;
    }

    private static string LookupGpu(Dictionary<string, PciIdsEntry> pci, string vendor, string device, string subvendor = null, string subdevice = null)
    {
        if (pci.TryGetValue(vendor, out var vEntry))
        {
            if (vEntry.Devices.TryGetValue(device, out var dEntry))
            {
                if (!string.IsNullOrEmpty(subvendor) && !string.IsNullOrEmpty(subdevice))
                {
                    string key = $"{subvendor}:{subdevice}";
                    if (dEntry.Subsystems.TryGetValue(key, out var subName))
                        return subName;
                }
                return dEntry.DeviceName;
            }
            return vEntry.VendorName;
        }
        return $"{vendor}:{device}";
    }

    #endregion

    #region Windows


    public static List<GpuInfoWindows> GetGpuInfoWindows()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Try via wmic
            // powershell -c "Get-CimInstance -ClassName win32_VideoController | Select-Object Name, AdapterRAM | ConvertTo-Json"
            string json = RunCommand("powershell",
                "-c \"Get-CimInstance -ClassName win32_VideoController | Select-Object Name, AdapterRAM | ConvertTo-Json\"");
            return ParseGpuWindows(json);
        }
        return new List<GpuInfoWindows>();
    }

    private static List<GpuInfoWindows> ParseGpuWindows(string json)
    {
        // Handles single or multiple objects, powershell may return 0~N GPU!
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<GpuInfoWindows>();
        }

        if (json.TrimStart().StartsWith("["))
        {
            // an array
            return JsonSerializer.Deserialize<List<GpuInfoWindows>>(json);
        }
        // 0~1 object
        return new List<GpuInfoWindows> { JsonSerializer.Deserialize<GpuInfoWindows>(json) };
    }

    #endregion

    private static string RunCommand(string file, string args)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            return output;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return string.Empty;
        }
    }
}

