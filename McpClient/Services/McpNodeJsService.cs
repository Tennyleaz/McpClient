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


internal class McpNodeJsService : CliService
{
    public static McpNodeJsService CreateMcpNodeJsService()
    {
        string path = Path.Combine(GlobalService.McpHostFolder, "mcp-host-use.exe");
        if (File.Exists(path))
            return new McpNodeJsService(path);
        return null;
    }

    private McpNodeJsService(string binaryPath) : base(binaryPath, null, 50)
    {
        
    }
}
