using McpClient.Models;
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

internal struct LlamaParam
{
    public string modelPath;
    public string modelType;
    public int deviceIndex;
    public bool isOffloadKvCache;
    public int contextSize;

    // base
    public string binaryPath;
    public string arguments;
    public int maxLogLines;
}

internal class LlamaService : CliService, IDisposable
{
    private readonly HttpClient _httpClient;
    private const int PORT = 2200;
    private static readonly string BASE_URL = "http://localhost:" + PORT;
    private Task _healthTask;

    // EVENTS

    // PROPERTIES
    public string Address => BASE_URL + "/v1";

    // CONFIG
    private readonly int _healthCheckIntervalMs;
    private readonly string _modelType;

    public static LlamaService CreateLlamaService(string modelPath, string modelType, int deviceIndex, bool isOffloadKvCache, int contextSize)
    {
        // server binary
        string binaryPath = GlobalService.LlamaServerBin;

        // arguments
        string arguments = $"--model {modelPath} --ctx-size {contextSize} --main-gpu {deviceIndex} --host 0.0.0.0 --port {PORT} --jinja";
        if (!isOffloadKvCache)
            arguments += " --no-kv-offload";

        // create llama directory
        string dir = GlobalService.LlamaInstallFolder;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        LlamaParam param = new LlamaParam
        {
            modelPath = modelPath,
            modelType = modelType,
            deviceIndex = deviceIndex,
            isOffloadKvCache = isOffloadKvCache,
            contextSize = contextSize,
            binaryPath = binaryPath,
            arguments = arguments,
            maxLogLines = 50
        };
        return new LlamaService(param);
    }

    private LlamaService(LlamaParam param) : base(param.binaryPath, param.arguments, param.maxLogLines)
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        _httpClient.BaseAddress = new Uri(BASE_URL);

        _modelType = param.modelType;
        SetToolCallParam();

        _healthCheckIntervalMs = 5000;
    }

    private void SetToolCallParam()
    {
        // see:
        // https://github.com/ggml-org/llama.cpp/blob/master/docs/function-calling.md
        switch (_modelType)
        {
            case "llama":
            case "llama3":
            case "llama4":
                break;
            case "qwen2":
            case "qwen2v1":
            case "qwen3":
                break;
            case "gemma3":
            case "gemma3n":
                break;
            case "phi3":
                break;
            case "deepseek2":
                break;
            case "gpt-oss":
                break;
            default:
                // unsupported model?
                break;
        }
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

    // LAUNCH
    public new bool Start()
    {
        if (base.Start())
        {
            _healthTask = Task.Run(() => HealthLoop(_cts!.Token));
            return true;
        }

        return false;
    }

    // For cleaning up on shutdown
    public void Dispose()
    {
        Stop();
        _httpClient.Dispose();
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
}