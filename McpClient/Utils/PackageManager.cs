using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace McpClient.Utils;

internal class Instruction
{
    [JsonPropertyName("manager")]
    public string Manager { get; set; }

    [JsonPropertyName("package")]
    public string Package { get; set; }
}



internal class PackageManagerFactory
{
    public static PackageManager Create(string managerName)
    {
        return managerName switch
        {
            "brew" => new BrewManager(),
            "apt" => new AptManager(),
            "dnf" => new DnfManager(),
            "winget" => new WingetManager(),
            "pip" => new PipManager(),
            "pipx" => new PipxManager(),
            "npm" => new NpmManager(),
            _ => null
        };
    }

    // Helper to choose a "default" manager for the OS, can be extended
    public static string DetectDefaultManager()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "brew";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "winget";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Optionally parse /etc/os-release for more accuracy between apt/dnf
            string linux = CheckLinuxVersion();
            switch (linux)
            {
                case "arch":
                    return "pacman";
                case "suse":
                    return "zypper";
                case "rhel":
                    return "dnf";
                case "debian":
                default:
                    return "apt";
            }
        }
        return null;
    }

    public static string CheckLinuxVersion()
    {
        // known versions:
        // debian, rhel, suse, arch
        if (System.IO.File.Exists("/etc/os-release"))
        {
            string[] lines = System.IO.File.ReadAllLines("/etc/os-release");
            string idLine = lines.FirstOrDefault(x => x.StartsWith("ID="));
            if (!string.IsNullOrEmpty(idLine))
            {
                idLine = idLine.Substring(3);
                switch (idLine)
                {
                    case "ubuntu":
                    case "debian":
                        return "debian";  // use apt
                    case "fedora":
                    case "centos":
                    case "rhel":
                    case "rocky":
                    case "almalinux":
                        return "rhel";  // use dnf
                    case "arch":
                        return "arch";
                    case "opensuse":
                    case "sles":
                        return "suse";
                }
            }
        }

        // fallback
        if (System.IO.File.Exists("/etc/debian_version"))
            return "debian";
        if (System.IO.File.Exists("/etc/redhat-release"))
            return "rhel";
        if (System.IO.File.Exists("/etc/SuSE-release"))
            return "suse";
        if (System.IO.File.Exists("/etc/arch-release"))
            return "arch";
        return "debian";
    }
}

internal abstract class PackageManager
{
    public abstract string Name { get; }

    public abstract bool IsAvailable();
    public abstract bool IsPackageInstalled(string package);
    public abstract string InstallCommand(string package);

    // Helper: Runs a shell command and returns stdout
    protected string RunCommand(string cmd, string args = "")
    {
        var psi = new ProcessStartInfo
        {
            FileName = cmd,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false
        };
        using var process = Process.Start(psi);
        string result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return result;
    }

    /// <summary>
    /// Normal managers simply run the command via shell.
    /// </summary>
    public virtual CommandResult RunInstallCommand(string command)
    {
        return ShellHelper.RunShellCommand(command);
    }
}

internal class WingetManager : PackageManager
{
    public override string Name => "winget";

    public override bool IsAvailable() =>
        !string.IsNullOrWhiteSpace(RunCommand("where", "winget"));

    public override bool IsPackageInstalled(string package)
    {
        // Special case: search command "node" for "OpenJS.NodeJS" package
        if (package == "OpenJS.NodeJS")
            package = "node";

        // Note: winget list is too slow
        return LocalServiceUtils.FindCommand(package);

        // Example: winget list NodeJS
        //string output = RunCommand("winget", $"list {package}");
        //return output.Contains(package, StringComparison.OrdinalIgnoreCase);
    }

    public override string InstallCommand(string package) => $"winget install --id={package} -e"; // -e for exact match
}

internal class BrewManager : PackageManager
{
    public override string Name => "brew";
    public override bool IsAvailable() =>
        !string.IsNullOrWhiteSpace(RunCommand("which", "brew"));

    public override bool IsPackageInstalled(string package) =>
        !string.IsNullOrWhiteSpace(RunCommand("brew", $"list --versions {package}"));

    public override string InstallCommand(string package) => $"brew install {package}";
}

internal class AptManager : PackageManager
{
    public override string Name => "apt";
    public override bool IsAvailable() =>
        !string.IsNullOrWhiteSpace(RunCommand("which", "apt"));

    public override bool IsPackageInstalled(string package) =>
        // Returns non-empty if installed
        !string.IsNullOrWhiteSpace(RunCommand("dpkg-query", $"-W -f='${{Status}}' {package}"));

    public override string InstallCommand(string package) => $"sudo apt-get install -y {package}";
}

internal class DnfManager : PackageManager
{
public override string Name => "dnf";
public override bool IsAvailable() =>
    !string.IsNullOrWhiteSpace(RunCommand("which", "dnf"));

public override bool IsPackageInstalled(string package) =>
    !string.IsNullOrWhiteSpace(RunCommand("rpm", $"-q {package}"));

public override string InstallCommand(string package) => $"sudo dnf install -y {package}";
}

internal class PipManager : PackageManager
{
    public override string Name => "pip";

    public override bool IsAvailable()
    {
        string output = RunCommand("python3", "-m pip --version");
        // pip 24.0 from /usr/lib/python3/dist-packages/pip (python 3.12)
        // pip 25.2 from C:\Program Files\WindowsApps\PythonSoftwareFoundation.Python.3.13_3.13.2544.0_x64__qbz5n2kfra8p0\Lib\site-packages\pip (python 3.13)
        return !string.IsNullOrWhiteSpace(output) && output.StartsWith("pip");
    }

    public override bool IsPackageInstalled(string package)
    {
        // Returns non-empty if installed
        string output = RunCommand("python3", $"-m pip show {package}");
        string match = $"Name: {package}";
        return !string.IsNullOrWhiteSpace(output) && output.Contains(match, StringComparison.OrdinalIgnoreCase);
    }

    public override string InstallCommand(string package) =>
        $"python3 -m pip install --user {package}";
}

internal class NpmManager : PackageManager
{
    public override string Name => "npm";

    public override bool IsAvailable()
    {
        string output = RunCommand("npm", "-v");
        return !string.IsNullOrWhiteSpace(output);
    }

    public override bool IsPackageInstalled(string package)
    {
        // PS C:\Users\tenny_lu> npm list -g --depth=0 npm
        // C:\nvm4w\nodejs -> .\
        // `-- npm@11.4.2
        string output;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Use CMD to run npm, because it is a script
            var result = ShellHelper.RunShellCommand($"npm list -g --depth=0 {package}");
            output = result.Output;
        }
        else
        {
            // Run npm directly
            output = RunCommand("npm", $"list -g --depth=0 {package}");
        }

        string match = $"-- {package}@";
        return !string.IsNullOrWhiteSpace(output) && output.Contains(match, StringComparison.OrdinalIgnoreCase);
    }

    public override string InstallCommand(string package) =>
        $"npm install -g {package}";
}

internal class PipxManager : PackageManager
{
    public override string Name => "pipx";

    public override bool IsAvailable()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            //return !string.IsNullOrWhiteSpace(RunCommand("where", "pipx"));
            // Note: where.exe or Get-Command cannot find pipx from windows store!
            string output = RunCommand("python3", "-m pip show pipx");
            string match = "Name: pipx";
            return !string.IsNullOrWhiteSpace(output) && output.Contains(match, StringComparison.OrdinalIgnoreCase);
        }

        return !string.IsNullOrWhiteSpace(RunCommand("which", "pipx"));
    }

    public override bool IsPackageInstalled(string package)
    {
        // pipx list includes a heading like 'package <version>'
        // python3 -m pipx list
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string output = RunCommand("python3", "-m pipx list");
            return output.Contains(package, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            string output = RunCommand("pipx", "list");
            return output.Contains(package, StringComparison.OrdinalIgnoreCase);
        }
    }

    public override string InstallCommand(string package) =>
        $"pipx install {package}";

    /// <summary>
    /// On windows, pipx should be executed by python3 module.
    /// </summary>
    public override CommandResult RunInstallCommand(string command)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ShellHelper.RunShellCommand(command);
        }

        // pipx install xxx => python3 -m pipx install xxx
        if (command.StartsWith("pipx install "))
        {
            command = "python3 -m " + command;
        }

        return ShellHelper.RunShellCommand(command);
    }
}

internal static class ShellHelper
{
    // Runs a shell command - optionally as administrator (affects Windows)
    public static CommandResult RunShellCommand(string command, bool runAsAdmin = false)
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var psi = new ProcessStartInfo();

        if (isWindows)
        {
            psi.FileName = "cmd.exe";
            psi.Arguments = $"/c {command}";
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;

            if (command.StartsWith("winget"))
                runAsAdmin = true;

            // On Windows, request admin if needed
            if (runAsAdmin)
            {
                psi.UseShellExecute = true; // Required for RunAs
                psi.RedirectStandardOutput = false;
                psi.RedirectStandardError = false;
                psi.Verb = "runas";
            }
        }
        else // Linux/macOS
        {
            psi.FileName = "/bin/bash";
            psi.Arguments = $"-c \"{command}\"";
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            // Sudo must be handled in the command string itself
        }

        string output = "", error = "";
        int exitCode = -1;

        try
        {
            using (var process = new Process())
            {
                process.StartInfo = psi;
                process.Start();

                if (!psi.UseShellExecute)
                {
                    output = process.StandardOutput.ReadToEnd();
                    error = process.StandardError.ReadToEnd();
                }

                process.WaitForExit();
                exitCode = process.ExitCode;
            }
        }
        catch (Exception ex)
        {
            error += Environment.NewLine + ex.ToString();
        }

        return new CommandResult(output, error, exitCode);
    }
}

internal struct CommandResult
{
    public string Output;
    public string Error;
    public int ExitCode;

    public bool Success => ExitCode == 0;

    public CommandResult(string output, string error, int exitCode)
    {
        Output = output;
        Error = error;
        ExitCode = exitCode;
    }
}