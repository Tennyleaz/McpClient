using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace McpClient.Utils;

internal static class LocalServiceUtils
{
    private static readonly string[] knownCommands = ["python3", "node", "npm", "npx", "uv", "uvx", "docker"];

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
        ProcessStartInfo psi = new ProcessStartInfo
        {
            Arguments = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            psi.FileName = "where.exe";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            //psi.FileName = "where.exe";
        }
        else
        {
            psi.FileName = "which";
        }

        Process process = new Process();
        process.StartInfo = psi;
        process.Start();
        process.WaitForExit(1500);

        //string stdout = process.StandardOutput.ReadToEnd();
        //string stderr = process.StandardError.ReadToEnd();

        if (process.ExitCode == 0)
        {
            // found in windows or linux
            return true;
        }

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
            default:
                return command;
        }
    }
}
