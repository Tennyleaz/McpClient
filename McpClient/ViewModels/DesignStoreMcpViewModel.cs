using Avalonia.Controls.Shapes;
using McpClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.ViewModels;

internal class DesignStoreMcpViewModel : StoreMcpViewModel
{
    public DesignStoreMcpViewModel()
    {
        Servers.Add(new StoreMcpServer
        {
            Name = "@reactuses",
            Author = "mcp",
            Description = "description...",
            Timestamp = "24 minutes ago",
            Url = "https://mcp.so/server/@reactuses/mcp/packages",
            Logo = new Logo
            {
                Src = "https://mcp.so/logo.svg",
                Alt = "@reactuses"
            }
        });
        Servers.Add(new StoreMcpServer
        {
            Name = "Crawlbase Mcp",
            Author = "crawlbase",
            Description = "Crawlbase MCP Server connects AI agents and LLMs with real-time web data. It powers Claude, Cursor, and Windsurf integrations with battle-tested web scraping, JavaScript rendering, and anti-bot protection — enabling structured, live data inside your AI workflows.",
            Timestamp = "a day ago",
            Url = "https://mcp.so/server/crawlbase-mcp/crawlbase",
            Logo = new Logo
            {
                Src = "https://mcp.so/logo.svg",
                Alt = "@reactuses"
            },
            IsInstalled = true
        });
        Servers.Add(new StoreMcpServer
        {
            Name = "NPM package docs",
            Author = "Anand",
            Description = "A Model Context Protocol (MCP) server that provides up-to-date documentation for npm packages directly in your IDE. This tool fetches the latest README documentation from either the package&#x27;s GitHub repository or the README bundled with the npm package itself.",
            Timestamp = "4 days ago",
            Url = "https://mcp.so/server/npm-package-docs/Anand",
            Logo = new Logo
            {
                Src = "https://mcp.so/logo.svg",
                Alt = "@reactuses"
            }
        });
    }
}
