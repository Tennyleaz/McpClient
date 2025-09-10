using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class McpServerConfig
{
    public List<McpServer> mcp_servers { get; set; } = new();
}

internal class McpServer
{
    public bool enabled { get; set; }
    public McpServerType type { get; set; }
    public string server_name { get; set; }
    public string owner { get; set; }
    public string sse_url { get; set; }
    public string streamable_http_url { get; set; }
    public string command { get; set; }
    public List<string> args { get; set; } = new();
    public Dictionary<string, string> env { get; set; } = new();
    public Dictionary<string, string> http_headers { get; set; } = new();
    public string source { get; set; }

    public override string ToString()
    {
        return $"{server_name} ({type})";
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum McpServerType
{
    [EnumMember(Value = "stdio")]
    Stdio = 1,
    [EnumMember(Value = "sse")]
    SSE = 2,
    [EnumMember(Value = "streambleHttp")]
    StreamableHttp = 3,
    [EnumMember(Value = "unknown")]
    Unknown = 4
}

