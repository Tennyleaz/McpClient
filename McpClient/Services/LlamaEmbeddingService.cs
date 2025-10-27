using System.IO;

namespace McpClient.Services;

internal class LlamaEmbeddingService : CliService
{
    private const int IMBEDDING_PORT = 6402;

    public static LlamaEmbeddingService CreateLlamaEmbeddingService(string modelPath)
    {
        // server binary
        string binaryPath = GlobalService.LlamaServerBin;

        // arguments
        string arguments = $"--model {modelPath} --ngl 0 --host 0.0.0.0 --port {IMBEDDING_PORT} --embeddings --no-webui";

        // create llama directory
        string dir = GlobalService.LlamaInstallFolder;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        LlamaParam param = new LlamaParam
        {
            modelPath = modelPath,
            //modelType = modelType,
            //deviceIndex = deviceIndex,
            //isOffloadKvCache = isOffloadKvCache,
            //contextSize = contextSize,
            binaryPath = binaryPath,
            arguments = arguments,
            maxLogLines = 50
        };
        return new LlamaEmbeddingService(param);
    }

    private LlamaEmbeddingService(LlamaParam param) : base("Llama embedding", param.binaryPath, param.arguments, param.maxLogLines, IMBEDDING_PORT)
    {

    }
}
