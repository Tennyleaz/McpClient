using System;
using System.IO;
using McpClient.Utils;

namespace McpClient.Services;

internal class RagBackendService : CliService
{
    private const int RAG_API_PORT = 6400;

    public static RagBackendService CreateBackendService()
    {
        string databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "McpClient", "rag_documents.db");
        // Add as "database" ASP.NET argument
        string arguments = $"--database {databasePath} --urls \"http://+:{RAG_API_PORT}\"";

        string path = Path.Combine(GlobalService.RagBackendFolder, "AiNexusRagService.exe");
        if (File.Exists(path))
            return new RagBackendService(path, arguments);
        return null;
    }

    private RagBackendService(string binaryPath, string arguments) : base("RAG", binaryPath, arguments, 50, RAG_API_PORT, true)
    {

    }

    public static bool IsRuntimeInstalled()
    {
        // Check python3
        if (!LocalServiceUtils.FindCommand("python3"))
            return false;

        // Check pipx
        if (!LocalServiceUtils.FindCommand("pipx"))
            return false;

        // Check chroma
        if (!LocalServiceUtils.FindCommand("chroma"))
            return false;

        return true;
    }
}
