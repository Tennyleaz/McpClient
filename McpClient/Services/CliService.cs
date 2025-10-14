using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;

namespace McpClient.Services;

internal enum CliServiceState
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Crashed,
    Error
}

internal abstract class CliService
{
    internal Process _process;
    internal readonly ConcurrentQueue<string> _stdoutQueue;
    internal readonly ConcurrentQueue<string> _stderrQueue;
    internal CancellationTokenSource _cts;
    private readonly string _serviceName;

    // EVENTS
    public event Action<CliServiceState, CliServiceState> OnStateChanged;

    // PROPERTIES
    public CliServiceState State { get; private set; } = CliServiceState.Stopped;
    public string[] LastStdOut => _stdoutQueue.ToArray();
    public string[] LastStdErr => _stderrQueue.ToArray();
    public int Pid { get; private set; }

    // CONFIG
    internal readonly string _binaryPath;
    internal readonly string _arguments;
    internal readonly int _maxLogLines;

    internal CliService(string binaryPath, string arguments, int maxLogLines)
    {
        _binaryPath = binaryPath;
        _arguments = arguments;
        _maxLogLines = maxLogLines;
        _stdoutQueue = new ConcurrentQueue<string>();
        _stderrQueue = new ConcurrentQueue<string>();

        // save service name, used for PID
        _serviceName = Path.GetFileNameWithoutExtension(binaryPath);
    }

    public bool Start()
    {
        lock (this)
        {
            if (State == CliServiceState.Running || State == CliServiceState.Starting)
                return false;
            if (!File.Exists(_binaryPath))
                return false;

            SetState(CliServiceState.Starting);

            // Check if an orphaned process havend't stopped yet
            TryKillLastProcess();

            // Clean up last process if any
            Stop();

            _stdoutQueue.Clear();
            _stderrQueue.Clear();
            _cts = new CancellationTokenSource();

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _binaryPath,
                    Arguments = _arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(_binaryPath)
                },
                EnableRaisingEvents = true
            };
            _process.OutputDataReceived += (s, e) => HandleOutput(_stdoutQueue, e.Data);
            _process.ErrorDataReceived += (s, e) => HandleOutput(_stderrQueue, e.Data);
            _process.Exited += (s, e) => HandleExit();

            try
            {
                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
                Pid = _process.Id;

                //_healthTask = Task.Run(() => HealthLoop(_cts!.Token));
                SetState(CliServiceState.Starting);

                // Save the PID to file
                SettingsManager.Local.SavePid(_serviceName, Pid);

                return true;
            }
            catch (Exception ex)
            {
                HandleOutput(_stderrQueue, "Startup FAILED: " + ex.Message);
                SetState(CliServiceState.Error);
                return false;
            }
        }
    }

    // STOP
    public void Stop()
    {
        lock (this)
        {
            if (_process == null || _process.HasExited)
            {
                SetState(CliServiceState.Stopped);
                return;
            }

            SetState(CliServiceState.Stopping);
            try
            {
                _cts?.Cancel();

                if (!_process.HasExited)
                {
                    _process.CloseMainWindow();
                    if (!_process.WaitForExit(2000))
                    {
                        _process.Kill(entireProcessTree: true);
                        _process.WaitForExit(2000);
                    }
                }

                // Remove the PID from file after successfully stop
                SettingsManager.Local.RemovePid(_serviceName);
            }
            catch { /* ignore if already dead */ }
            finally
            {
                SetState(CliServiceState.Stopped);
                _process?.Dispose();
                _process = null;
            }
        }
    }

    // STATE MANAGEMENT
    internal void SetState(CliServiceState state)
    {
        var old = State;
        State = state;
        OnStateChanged?.Invoke(old, state);
    }

    private void HandleOutput(ConcurrentQueue<string> queue, string data)
    {
        if (string.IsNullOrEmpty(data))
            return;

        Debug.WriteLine(data);

        queue.Enqueue(data);
        while (queue.Count > _maxLogLines)
            queue.TryDequeue(out _);
    }

    private void HandleExit()
    {
        // This runs on a threadpool thread
        if (State == CliServiceState.Stopping || State == CliServiceState.Stopped)
        {
            SetState(CliServiceState.Stopped);
        }
        else
        {
            SetState(CliServiceState.Crashed);
        }

        // Remove the PID from file after successfully stop
        SettingsManager.Local.RemovePid(_serviceName);
    }

    private void TryKillLastProcess()
    {
        int oldPid = SettingsManager.Local.LoadPid(_serviceName);
        if (oldPid > 0)
        {
            try
            {
                // Only kill if the process filename matches our backend's path!
                if (IsExpectedBackend(oldPid, _binaryPath))
                {
                    Process oldProcess = Process.GetProcessById(oldPid);
                    oldProcess.Kill();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to kill old process! " + ex.Message);
            }
            SettingsManager.Local.RemovePid(_serviceName);
        }
    }

    private static bool IsExpectedBackend(int pid, string expectedBinary)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var proc = Process.GetProcessById(pid);
            return Path.GetFullPath(proc.MainModule.FileName).Equals(Path.GetFullPath(expectedBinary), StringComparison.OrdinalIgnoreCase);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Readlink on '/proc/{pid}/exe'
            string exeLink = $"/proc/{pid}/exe";
            if (File.Exists(exeLink))
            {
                try
                {
                    // Use bash to get symlink target (no native API yet)
                    string targetExe = GetProcessNameLinux(pid);
                    return Path.GetFullPath(targetExe).Equals(Path.GetFullPath(expectedBinary), StringComparison.Ordinal);
                }
                catch { return false; } // No permission or process gone
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Use 'ps' to get command path
            try
            {
                string targetExe = GetProcessNameMacOs(pid);
                return Path.GetFullPath(targetExe).Equals(Path.GetFullPath(expectedBinary), StringComparison.Ordinal);
            }
            catch { return false; }
        }
        return false;
    }

    private static string GetProcessNameLinux(int pid)
    {
        string exeLink = $"/proc/{pid}/exe";
        string targetExe = null;
        if (File.Exists(exeLink))
        {
            var psi = new ProcessStartInfo
            {
                FileName = "readlink",
                Arguments = exeLink,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            using var ps = Process.Start(psi);
            targetExe = ps.StandardOutput.ReadToEnd().Trim();
            ps.WaitForExit();
        }
        return targetExe;
    }

    private static string GetProcessNameMacOs(int pid)
    {
        var psi = new ProcessStartInfo("ps", $"-p {pid} -o comm=")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        using var ps = Process.Start(psi);
        string outPath = ps.StandardOutput.ReadToEnd().Trim();
        ps.WaitForExit();
        return outPath;
    }
}