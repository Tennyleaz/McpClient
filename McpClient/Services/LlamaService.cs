using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace McpClient.Services;

internal enum LlamaServerState
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Crashed,
    Error
}

internal class LlamaService : IDisposable
{
    private readonly HttpClient _httpClient;
    private const int PORT = 2200;
    private static readonly string BASE_URL = "http://localhost:" + PORT;
    private Process _process;
    private readonly ConcurrentQueue<string> _stdoutQueue;
    private readonly ConcurrentQueue<string> _stderrQueue;
    private CancellationTokenSource _cts;
    private Task _healthTask;

    // EVENTS
    public event Action<LlamaServerState, LlamaServerState> OnStateChanged;

    // PROPERTIES
    public LlamaServerState State { get; private set; } = LlamaServerState.Stopped;
    public string[] LastStdOut => _stdoutQueue.ToArray();
    public string[] LastStdErr => _stderrQueue.ToArray();

    // CONFIG
    private readonly string _binaryPath;
    private readonly string _arguments;
    private readonly int _maxLogLines;
    private readonly int _healthCheckIntervalMs;

    public LlamaService(string modelPath, int deviceIndex, bool isOffloadKvCache, int contextSize)
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        _httpClient.BaseAddress = new Uri(BASE_URL);

        // llama directory
        string dir = GlobalService.LlamaInstallFolder;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // server binary
        string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string binDir = Path.Combine(appdata, "McpClient", "Llama");
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            _binaryPath = Path.Combine(binDir, "llama-server.exe");
        else
            _binaryPath = Path.Combine(binDir, "llama-server");

        // arguments
        string arguments = $"--model {modelPath} --ctx-size {contextSize} --main-gpu {deviceIndex}";
        if (!isOffloadKvCache)
            arguments += " --no-kv-offload";
        _arguments = arguments;
        
        _maxLogLines = 20;
        _healthCheckIntervalMs = 5000;
        _stdoutQueue = new ConcurrentQueue<string>();
        _stderrQueue = new ConcurrentQueue<string>();
    }

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

    // STATE MANAGEMENT
    private void SetState(LlamaServerState state)
    {
        var old = State;
        State = state;
        OnStateChanged?.Invoke(old, state);
    }

    // LAUNCH
    public bool Start()
    {
        lock (this)
        {
            if (State == LlamaServerState.Running || State == LlamaServerState.Starting)
                return false;
            if (!File.Exists(_binaryPath))
                return false;

            SetState(LlamaServerState.Starting);

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
                    CreateNoWindow = true
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

                _healthTask = Task.Run(() => HealthLoop(_cts!.Token));
                SetState(LlamaServerState.Running);
                return true;
            }
            catch (Exception ex)
            {
                HandleOutput(_stderrQueue, "Startup FAILED: " + ex.Message);
                SetState(LlamaServerState.Error);
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
                SetState(LlamaServerState.Stopped);
                return;
            }

            SetState(LlamaServerState.Stopping);
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
            }
            catch { /* ignore if already dead */ }
            finally
            {
                SetState(LlamaServerState.Stopped);
                _process?.Dispose();
                _process = null;
            }
        }
    }

    // For cleaning up on shutdown
    public void Dispose()
    {
        Stop();
        _httpClient.Dispose();
    }

    private void HandleOutput(ConcurrentQueue<string> queue, string data)
    {
        if (data == null)
            return;
        queue.Enqueue(data);
        while (queue.Count > _maxLogLines)
            queue.TryDequeue(out _);
    }

    private void HandleExit()
    {
        // This runs on a threadpool thread
        if (State == LlamaServerState.Stopping || State == LlamaServerState.Stopped)
        {
            SetState(LlamaServerState.Stopped);
        }
        else
        {
            SetState(LlamaServerState.Crashed);
        }
    }

    private async Task HealthLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_process == null || _process.HasExited)
            {
                SetState(LlamaServerState.Crashed);
                break;
            }
            // Optional: ping HTTP /health endpoint here for deeper checks
            bool isUp = await CheckHealth();
            if (!isUp)
            {
                if (State == LlamaServerState.Running)
                {
                    SetState(LlamaServerState.Crashed);
                }
                else if (State == LlamaServerState.Stopping || State == LlamaServerState.Stopped)
                {
                    SetState(LlamaServerState.Stopped);
                }
                else
                {
                    SetState(LlamaServerState.Crashed);
                }
                break;
            }

            await Task.Delay(_healthCheckIntervalMs, cancellationToken).ContinueWith(_ => { }, cancellationToken);
        }
    }
}