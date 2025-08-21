using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McpClient.Models;

namespace McpClient.ViewModels;

internal class DesignStoreMcpServerViewModel : StoreMcpServer
{
    public DesignStoreMcpServerViewModel()
    {
        Name = "NPM package docs";
        Author = "Anand";
        Description =
            "A Model Context Protocol (MCP) server that provides up-to-date documentation for npm packages directly in your IDE. This tool fetches the latest README documentation from either the package&#x27;s GitHub repository or the README bundled with the npm package itself.";
        Timestamp = "4 days ago";
        Url = "https://mcp.so/server/npm-package-docs/Anand";
        Logo = new Logo
        {
            Src = "https://mcp.so/logo.svg",
            Alt = "@reactuses"
        };
    }
}
