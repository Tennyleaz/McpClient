using McpClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.ViewModels;

internal class DesignMcpServerConfigViewModel : McpServerConfigViewModel
{
    public DesignMcpServerConfigViewModel()
    {
        McpServer config1 = new McpServer
        {
            enabled = false,
            type = "stdio",
            server_name = "edgeone-pages-mcp-server",
            command = "npx",
            args = new List<string> { "edgeone-pages-mcp" },
            source = ""
        };
        McpServers.Add(new McpViewModel(config1, false));

        McpServer config2 = new McpServer
        {
            enabled = true,
            type = "sse",
            server_name = "Text2Image",
            sse_url = "http://localhost:9000/sse",
            owner = "statham_li",
            source = ""
        };
        McpServers.Add(new McpViewModel(config2, true));

        McpServer config3 = new McpServer
        {
            enabled = true,
            type = "sse",
            server_name = "RAGServer",
            sse_url = "http://192.168.41.60:8012/sse",
            owner = "statham_li",
            source = "cloud",
        };
        McpServers.Add(new McpViewModel(config3, true));

        McpServer config4 = new McpServer
        {
            enabled = true,
            type = "streamableHttp",
            server_name = "AwesomeStreamableHttpServer",
            sse_url = "http://192.168.41.60:8012/streamableHttp",
            owner = "statham_li",
            source = "cloud",
        };
        McpServers.Add(new McpViewModel(config4, true));
    }
}

