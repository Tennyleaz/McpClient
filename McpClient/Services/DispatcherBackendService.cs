using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace McpClient.Services;

internal class DispatcherBackendService : CliService
{
    private const int DISPATCHER_PORT = 5000;

    public static DispatcherBackendService CreateBackendService()
    {
        // Check for "mcp_servers.config.json"
        string jsonPath = Path.Combine(GlobalService.McpHostFolder, "mcp_servers.config.json");
        // Add as "config-file" ASP.NET argument
        string arguments = $"--config-file \"{jsonPath}\"";

        string path = Path.Combine(GlobalService.DispatcherFolder, "dispatcher.exe");
        if (File.Exists(path))
            return new DispatcherBackendService(path, arguments);
        return null;
    }

    private DispatcherBackendService(string binaryPath, string arguments) : base(binaryPath, arguments, 50, DISPATCHER_PORT, true)
    {

    }
}
