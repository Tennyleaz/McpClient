using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class McpServerConfig
{
    public List<McpServer> mcp_servers { get; set; } = new();
}

internal class McpServer
{
    public bool enabled { get; set; }
    public string type { get; set; }
    public string server_name { get; set; }
    public string owner { get; set; }
    public string sse_url { get; set; }
    public string streamable_http_url { get; set; }
    public string command { get; set; }
    public List<string> args { get; set; } = new();
    public Dictionary<string, string> env { get; set; } = new();
}

