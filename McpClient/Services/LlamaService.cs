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

internal class LlamaService : CliService
{
    private const int LLAMA_PORT = 2200;

    // EVENTS

    // PROPERTIES
    public string Address => _base_url + "/v1";

    // CONFIG
    private readonly string _modelType;

    public static LlamaService CreateLlamaService(string modelPath, string modelType, int deviceIndex, bool isOffloadKvCache, int contextSize)
    {
        // server binary
        string binaryPath = GlobalService.LlamaServerBin;

        // arguments
        string arguments = $"--model {modelPath} --ctx-size {contextSize} --main-gpu {deviceIndex} --host 0.0.0.0 --port {LLAMA_PORT} --jinja";
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

    private LlamaService(LlamaParam param) : base(param.binaryPath, param.arguments, param.maxLogLines, LLAMA_PORT)
    {
        _modelType = param.modelType;
        SetToolCallParam();
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
}