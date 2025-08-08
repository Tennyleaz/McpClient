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
            args = new List<string> { "edgeone-pages-mcp" }
        };
        McpServers.Add(new McpViewModel(config1, false));

        McpServer config2 = new McpServer
        {
            enabled = true,
            type = "sse",
            server_name = "Text2Image",
            sse_url = "http://localhost:9000/sse",
            owner = "statham_li"
        };
        McpServers.Add(new McpViewModel(config2, true));
    }
}

