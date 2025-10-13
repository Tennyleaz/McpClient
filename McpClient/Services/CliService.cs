using ModelContextProtocol.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

internal abstract class CliService
{
    internal Process _process;
    internal readonly ConcurrentQueue<string> _stdoutQueue;
    internal readonly ConcurrentQueue<string> _stderrQueue;
    internal CancellationTokenSource _cts;

    // EVENTS
    public event Action<CliServiceState, CliServiceState> OnStateChanged;

    // PROPERTIES
    public CliServiceState State { get; private set; } = CliServiceState.Stopped;
    public string[] LastStdOut => _stdoutQueue.ToArray();
    public string[] LastStdErr => _stderrQueue.ToArray();

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

                //_healthTask = Task.Run(() => HealthLoop(_cts!.Token));
                SetState(CliServiceState.Starting);
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
    }
}