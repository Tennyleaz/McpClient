using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace McpClient.Utils;

internal static class LlamaDeviceUtils
{
    // Regex for:   CUDA0: NVIDIA P104-100 (8109 MiB, 8020 MiB free)
    private static readonly Regex DeviceLine = new Regex(
      @"^\s*([A-Za-z]+)(\d+):\s+(.+?)\s*\((\d+)\s*MiB,\s*(\d+)\s*MiB free\)", RegexOptions.Compiled);

    // Fallback: If "free" part is missing, e.g. Vulkan0: Foo (1234 MiB)
    private static readonly Regex DeviceLineNoFree = new Regex(
        @"^\s*([A-Z]+)(\d+):\s+(.+?)\s*\((\d+)\s*MiB\)", RegexOptions.Compiled);

    public static List<LlamaDevice> ParseDevices(string output)
    {
        List<LlamaDevice> devices = new List<LlamaDevice>();
        bool inDevices = false;

        foreach (string rawline in output.Split('\n'))
        {
            string line = rawline.TrimEnd('\r', '\n');

            if (!inDevices)
            {
                if (line.Trim() == "Available devices:")
                {
                    inDevices = true;
                }
                continue;
            }

            // End of device list section if blank or a comment or starts another section
            if (string.IsNullOrWhiteSpace(line)
                || line.TrimStart().StartsWith('(')
                || (!line.StartsWith(' ') && !line.StartsWith('\t')))
                break;

            var m = DeviceLine.Match(line);
            if (m.Success)
            {
                devices.Add(new LlamaDevice
                {
                    Backend = m.Groups[1].Value,
                    Index = int.Parse(m.Groups[2].Value),
                    Name = m.Groups[3].Value.Trim(),
                    TotalMemoryMiB = int.Parse(m.Groups[4].Value),
                    FreeMemoryMiB = int.Parse(m.Groups[5].Value)
                });
                continue;
            }

            //var m2 = DeviceLineNoFree.Match(line);
            //if (m2.Success)
            //{
            //    devices.Add(new LlamaDevice
            //    {
            //        Backend = m2.Groups[1].Value,
            //        Index = int.Parse(m2.Groups[2].Value),
            //        Name = m2.Groups[3].Value.Trim(),
            //        TotalMemoryMiB = int.Parse(m2.Groups[4].Value),
            //        FreeMemoryMiB = null
            //    });
            //    continue;
            //}
        }

        // Fallback: If no GPUs parsed, add a CPU fallback device
        if (devices.Count == 0)
        {
            devices.Add(new LlamaDevice { Backend = "CPU", Index = 0, Name = "CPU" });
        }

        return devices;
    }

    /// <summary>
    /// Invokes llama-cli at given path with --list-devices and parses the devices.
    /// Throws on failure.
    /// </summary>
    public static List<LlamaDevice> ListLlamaCppDevices(string llamaCliPath)
    {
        if (!File.Exists(llamaCliPath))
        {
            return new List<LlamaDevice>();
        }

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = llamaCliPath,
            Arguments = "--list-devices",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using Process proc = new Process { StartInfo = psi };

        string stdout, stderr;
        try
        {
            proc.Start();

            // Capture both stdout and stderr (in case info split!)
            stdout = proc.StandardOutput.ReadToEnd();
            stderr = proc.StandardError.ReadToEnd();

            proc.WaitForExit();

            int code = proc.ExitCode;
            if (code != 0)
            {
                //throw new Exception($"llama-cli exited with code {code}\nSTDERR:\n{stderr}\nSTDOUT:\n{stdout}");
                return new List<LlamaDevice>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error getting llama devices: " + ex.Message);
            return new List<LlamaDevice>();
        }

        // Sometimes llama.cpp sends warnings, etc, to stderr; optionally concatenate
        string output = !string.IsNullOrWhiteSpace(stdout) ? stdout : stderr; // fallback, but ideally parse both

        // Or, to be super-safe, combine both outputs.
        if (!string.IsNullOrWhiteSpace(stderr) && !stdout.Contains("Available devices:") &&
            stderr.Contains("Available devices:"))
        {
            output = stderr;
        }
        else if (!string.IsNullOrWhiteSpace(stderr))
        {
            output = stdout + Environment.NewLine + stderr;
        }

        return ParseDevices(output);
    }
}
