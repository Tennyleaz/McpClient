using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace McpClient.Services;

internal class ChromaDbService : CliService
{
    private const int CHROMA_PORT = 9092;

    public static ChromaDbService CreateChromaDbService()
    {
        // Use CMD to run python script
        string binaryPath;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            binaryPath = "chroma.exe";
        else
            binaryPath = "chroma";
        // Get database path from settings
        string arguments = $"run --path \"{GlobalService.ChromaDbFolder}\" --port {CHROMA_PORT}";
        return new ChromaDbService(binaryPath, arguments)
        {
            SkipBinaryCheck = true  // Because this is not an absolute path
        };
    }

    private ChromaDbService(string binaryPath, string arguments) : base("ChromaDB", binaryPath, arguments, 50, CHROMA_PORT, true)
    {

    }

    public override async Task<bool> CheckHealth()
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync("/api/v2/healthcheck");
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                ChromaHealth health = JsonSerializer.Deserialize<ChromaHealth>(json);
                return health.is_executor_ready;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        return false;
    }

    private struct ChromaHealth
    {
        public bool is_executor_ready { get; set; }
    }
}
