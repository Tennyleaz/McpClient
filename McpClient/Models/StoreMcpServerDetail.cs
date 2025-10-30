using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class StoreMcpServerDetailBase
{
    [JsonPropertyName("server_config")]
    public string ServerConfig { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("author")]
    public string Author { get; set; }

    [JsonPropertyName("github_url")]
    public string GithubUrl { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("overview")]
    public string Overview { get; set; }

    [JsonPropertyName("content")]
    public object Content { get; set; }

    [JsonPropertyName("project_info")]
    public StoreProjectInfo ProjectInfo { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("timestamp_text")]
    public string TimestampText { get; set; }

    [JsonPropertyName("published_at")]
    public string PublishedAt { get; set; }

    [JsonPropertyName("type_id")]
    public McpServerType TypeId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }
}

internal class StoreMcpServerDetail : StoreMcpServerDetailBase
{
    [JsonPropertyName("server_config")]
    public new StoreMcpServerConfig ServerConfig { get; set; }

    public McpServer SeverTypeToLocalType()
    {
        JsonNode node = ServerConfig.McpServers;
        if (node == null)
            return null;

        /*
         Find this pattern:
         "server_config": {
               "mcpServers": {
                   "名字在這裡": {
                       "url": "http://localhost:8080/mcp/",
                       "type": "sse"
                   }
               }
           }
         */
        string name, url, type, command;
        JsonNode firstChild;
        try
        {
            name = node.AsObject().First().Key;
            firstChild = node.Root[name];
            if (firstChild == null)
                return null;

            url = firstChild["url"]?.ToString();
            type = firstChild["type"]?.ToString();
            command = firstChild["command"]?.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }

        McpServer mcpServer = new McpServer
        {
            enabled = true,
            server_name = name,
            owner = Author,
            source = "cloud",
            detail = Url,
            description = Description.Trim(),
        };

        switch (type)
        {
            case "streamable-http":
                mcpServer.type = McpServerType.StreamableHttp;
                break;
            case "sse":
                mcpServer.type = McpServerType.SSE;
                break;
            default:
                if (string.IsNullOrEmpty(command))
                    mcpServer.type = McpServerType.SSE;
                else
                    mcpServer.type = McpServerType.Stdio;
                break;
        }

        if (mcpServer.type == McpServerType.Stdio)
        {
            try
            {
                mcpServer.command = command;
                if (firstChild["env"] != null)
                {
                    string json = firstChild["env"].ToJsonString();
                    mcpServer.env = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                }

                if (firstChild["args"] != null)
                {
                    string json = firstChild["args"].ToJsonString();
                    mcpServer.args = JsonSerializer.Deserialize<List<string>>(json);
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine(ex);
            }
        }
        else if (mcpServer.type == McpServerType.StreamableHttp)
        {
            mcpServer.streamable_http_url = url;
            if (firstChild["headers"] != null)
            {
                string json = firstChild["headers"].ToJsonString();
                mcpServer.http_headers = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
        }
        else if (mcpServer.type == McpServerType.SSE)
        {
            mcpServer.sse_url = url;
            if (firstChild["headers"] != null)
            {
                string json = firstChild["headers"].ToJsonString();
                mcpServer.http_headers = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
        }

        return mcpServer;
    }
}

internal class StoreMcpServerConfig
{
    [JsonPropertyName("mcpServers")]
    public JsonNode McpServers { get; set; }
}

public class StoreProjectInfo
{
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public object UpdatedAt { get; set; }

    [JsonPropertyName("github_stats")]
    public object GithubStats { get; set; }

    [JsonPropertyName("license")]
    public object License { get; set; }
}

internal class StoreMcpServerItem
{
    [JsonPropertyName("url")]
    public string Url { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("alwaysAllow")]
    public string[] AlwaysAllow { get; set; }
}