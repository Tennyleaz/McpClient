using McpClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace McpClient.Utils;

internal static class McpFileSystemRootUtils
{
    /// <summary>
    /// Read filesystem MCP setting from "mcp_servers.config.json"
    /// </summary>
    /// <returns></returns>
    public static async Task<List<string>> GetRootsFromSetting()
    {
        List<string> roots = new List<string>();
        try
        {
            string path = GlobalService.McpHostConfigFile;
            string json = await File.ReadAllTextAsync(path);
            McpServerConfig config = JsonSerializer.Deserialize<McpServerConfig>(json);

            foreach (McpServer server in config.mcp_servers)
            {
                if (server.IsFileSystem())
                {
                    // "npx"
                    // "-y","@modelcontextprotocol/server-filesystem", "/path/to/allowed/dir", ...
                    // Update the 3rd parameter
                    const int index = 2;
                    if (server.args.Count <= index)
                    {
                        // No paths are set
                    }
                    else
                    {
                        // Update to global variable
                        for (int i = index; i < server.args.Count; i++)
                        {
                            if (Path.IsPathFullyQualified(server.args[i]))
                            {
                                roots.Add(server.args[i]);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return roots;
    }

    /// <summary>
    /// Write new filesystem roots to "mcp_servers.config.json"
    /// </summary>
    /// <param name="roots"></param>
    /// <returns></returns>
    public static async Task<bool> SetRootsToSetting(List<string> roots)
    {
        try
        {
            string path = GlobalService.McpHostConfigFile;
            string json = await File.ReadAllTextAsync(path);
            McpServerConfig config = JsonSerializer.Deserialize<McpServerConfig>(json);
            bool isSet = false;
            foreach (McpServer server in config.mcp_servers)
            {
                if (server.IsFileSystem())
                {
                    // "npx"
                    // "-y","@modelcontextprotocol/server-filesystem", "/path/to/allowed/dir", ...
                    // Update the 3rd parameter
                    List<string> newArgs = new();
                    const int startIndex = 2;
                    if (server.args.Count >= startIndex)
                    {
                        // Only copy the first 2 arguments
                        newArgs.Add(server.args[0]);
                        newArgs.Add(server.args[1]);
                    }

                    newArgs.AddRange(roots);
                    server.args = newArgs;
                    isSet = true;
                    break;
                }
            }

            if (isSet)
            {
                json = JsonSerializer.Serialize(config);
                await File.WriteAllTextAsync(path, json);
            }

            return isSet;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        // Not found filesystem MCP
        return false;
    }

    /// <summary>
    /// Reset and write filesystem roots to "mcp_servers.config.json"
    /// </summary>
    /// <returns></returns>
    public static async Task<List<string>> ResetDefaults()
    {
        string defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "McpFileSystem");
        Directory.CreateDirectory(defaultDir);
        List<string> roots = [defaultDir];
        await SetRootsToSetting(roots);
        return roots;
    }
}
