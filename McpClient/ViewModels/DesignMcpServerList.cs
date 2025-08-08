using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.ViewModels;

internal class DesignMcpServerList : McpServerListViewModel
{
    public DesignMcpServerList()
    {
        ServerNames.Add("edgeone-pages-mcp-server");
        ServerNames.Add("Text2Image"); 
        ServerNames.Add("quickchart-server");
    }
}

