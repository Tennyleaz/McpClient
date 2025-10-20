using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

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

internal abstract class CliService : IDisposable
{
    internal Process _process;
    internal readonly ConcurrentQueue<string> _stdoutQueue;
    internal readonly ConcurrentQueue<string> _stderrQueue;
    internal CancellationTokenSource _cts;
    private readonly string _serviceName;

    // For HTTP services only
    private readonly HttpClient _httpClient;
    internal readonly string _base_url;
    private readonly int _healthCheckIntervalMs;
    private readonly bool _allowShutdown;
    private Task _healthTask;

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

    internal CliService(string binaryPath, string arguments, int maxLogLines, int port = 0, bool allowShutdown = false)
    {
        _binaryPath = binaryPath;
        _arguments = arguments;
        _maxLogLines = maxLogLines;
        _stdoutQueue = new ConcurrentQueue<string>();
        _stderrQueue = new ConcurrentQueue<string>();

        // save service name, used for PID
        _serviceName = Path.GetFileNameWithoutExtension(binaryPath);

        // For http services
        if (port > 0)
        {
            _base_url= "http://localhost:" + port;

            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
            _httpClient.BaseAddress = new Uri(_base_url);

            _healthCheckIntervalMs = 5000;
        }
        _allowShutdown = allowShutdown;
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

                SetState(CliServiceState.Starting);

                // Save the PID to file
                SettingsManager.Local.SavePid(_serviceName, Pid);

                // Only start health check if it has HTTP url
                if (_httpClient != null)
                {
                    _healthTask = Task.Run(() => HealthLoop(_cts!.Token));
                }

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

    public void Dispose()
    {
        Stop();
        _httpClient?.Dispose();
    }

    #region Http service health check

    public async Task<bool> CheckHealth()
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }

    private async Task HealthLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_process == null || _process.HasExited)
            {
                SetState(CliServiceState.Crashed);
                break;
            }
            // Optional: ping HTTP /health endpoint here for deeper checks
            bool isUp = await CheckHealth();
            if (!isUp)
            {
                if (State == CliServiceState.Running)
                {
                    SetState(CliServiceState.Crashed);
                    break;
                }
                else if (State == CliServiceState.Starting)
                {
                    // Do nothing, wait for start finish
                }
                else if (State == CliServiceState.Stopping || State == CliServiceState.Stopped)
                {
                    SetState(CliServiceState.Stopped);
                    break;
                }
                else
                {
                    SetState(CliServiceState.Crashed);
                    break;
                }
            }
            else
            {
                SetState(CliServiceState.Running);
            }

            await Task.Delay(_healthCheckIntervalMs, cancellationToken).ContinueWith(_ => { }, cancellationToken);
        }
    }

    private bool SendShutdown()
    {
        if (!_allowShutdown)
            return true;

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/shutdown");
        request.Headers.Add("X-Shutdown-Token", "AI_NEXUS_CLIENT");

        try
        {
            using HttpResponseMessage response = _httpClient.Send(request);
            if (response.IsSuccessStatusCode)
                return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        return false;
    }

    #endregion
}