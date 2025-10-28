using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace McpClient.Utils;

internal static class LocalServiceUtils
{
    private static readonly string[] knownCommands = ["python3", "node", "npm", "npx", "uv", "uvx", "docker", "bun", "bunx"];

    public static List<string> ListKnownLocalServices()
    {
        List<string> foundServices = new List<string>();

        foreach (string command in knownCommands)
        {
            if (FindCommand(command))
            {
                foundServices.Add(command);
            }
        }

        return foundServices;
    }

    public static bool FindCommand(string command)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            //return PowershellGetCommand(command);
            return CmdWhere(command);
        }

        ProcessStartInfo psi = new ProcessStartInfo
        {
            Arguments = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            FileName = "which"  // works for all unix like os
        };

        using Process process = new Process();
        process.StartInfo = psi;
        process.Start();
        process.WaitForExit(2000);

        //string stdout = process.StandardOutput.ReadToEnd();
        //string stderr = process.StandardError.ReadToEnd();

        if (process.ExitCode == 0)
        {
            // found in linux or macos
            return true;
        }

        return false;
    }

    /// <summary>
    /// Because where.exe is not good at microsoft store apps, we use powershell get-command instead.
    /// </summary>
    private static bool PowershellGetCommand(string command)
    {
        // Compose the PS command.
        string psCmd = $"Get-Command {command} | Select-Object -ExpandProperty Source";

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-Command \"{psCmd}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = true,
            CreateNoWindow = true,
        };

        using Process process = new Process();

        process.StartInfo = psi;
        process.Start();
        string output = process.StandardOutput.ReadToEnd().Trim();
        string error = process.StandardError.ReadToEnd().Trim();
        process.WaitForExit();

        // Non-empty output means found (should be the path)
        if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            return true;

        return false;
    }

    private static bool CmdWhere(string command)
    {
        // Compose the PS command.
        string psCmd = $"/c where {command}";

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = psCmd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using Process process = new Process();

        process.StartInfo = psi;
        process.Start();
        string output = process.StandardOutput.ReadToEnd().Trim();
        string error = process.StandardError.ReadToEnd().Trim();
        process.WaitForExit();

        // Non-empty output means found (should be the path)
        if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            return true;

        return false;
    }

    public static string FindRuntimeByCommand(string command)
    {
        switch (command)
        {
            case "python":
            case "python3":
            case "pip":
            case "pip3":
                return "python3";
            case "uv":
            case "uvx":
                return "uv";
            case "node":
            case "npm":
            case "npx":
                return "node";
            case "docker":
                return "docker";
            case "bun":
            case "bunx":
                return "bun";
            case "chroma":
            case "chromadb":
                return "chroma";
            default:
                return command;
        }
    }
}
