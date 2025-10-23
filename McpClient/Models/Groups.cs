using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class Group
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public bool IsPublic { get; set; }
    public List<Agent> Workflows { get; set; }
}

internal class Agent
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("is_default")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("mcp_registry_id")]
    public int McpRegistryId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("created_by")]
    public int CreatedBy { get; set; }

    [JsonPropertyName("customHeaders")]
    public List<AgentHeader> CustomHeaders { get; set; }

    [JsonPropertyName("user_enable")]
    public bool UserEnable { get; set; }

    [JsonPropertyName("user_obtained")]
    public bool UserObtained { get; set; }
}

internal class AgentHeader
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("agent_id")]
    public int AgentId { get; set; }

    [JsonPropertyName("header")]
    public string Header { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("placehold")]
    public string Placehold { get; set; }

    [JsonPropertyName("default")]
    public string Default { get; set; }

    [JsonPropertyName("headerValues")]
    public HeaderValue HeaderValues { get; set; }
}

internal class HeaderValue
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("header_id")]
    public int HeaderId { get; set; }

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}

internal class Workflow
{
    public int Id { get; set; }
    public int Group_id { get; set; }
    public int Agent_id { get; set; }
    public int Sequence_order { get; set; }
}