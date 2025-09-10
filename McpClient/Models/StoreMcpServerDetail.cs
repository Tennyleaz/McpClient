using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    [JsonPropertyName("type_id")]
    public McpServerType TypeId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }
}

internal class StoreMcpServerDetail : StoreMcpServerDetailBase
{
    [JsonPropertyName("server_config")]
    public new StoreMcpServerConfig ServerConfig { get; set; }
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