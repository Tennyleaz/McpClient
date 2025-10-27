using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class McpRegistryResponse
{
    [JsonPropertyName("servers")]
    public List<McpRegistryItem> Servers { get; set; }
}

internal class McpRegistryItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("title")]
    public object Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("endpoint_url")]
    public object EndpointUrl { get; set; }

    [JsonPropertyName("type_id")]
    public int TypeId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("website_url")]
    public object WebsiteUrl { get; set; }

    [JsonPropertyName("repository_url")]
    public string RepositoryUrl { get; set; }

    [JsonPropertyName("official_is_latest")]
    public bool OfficialIsLatest { get; set; }

    [JsonPropertyName("official_published_at")]
    public DateTime OfficialPublishedAt { get; set; }

    [JsonPropertyName("official_updated_at")]
    public DateTime OfficialUpdatedAt { get; set; }

    [JsonPropertyName("official_status")]
    public string OfficialStatus { get; set; }

    [JsonPropertyName("timestamp_text")]
    public string TimestampText { get; set; }
}

internal class McpRegistryDetail
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("title")]
    public object Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("website_url")]
    public object WebsiteUrl { get; set; }

    [JsonPropertyName("type_id")]
    public int TypeId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("endpoint_url")]
    public object EndpointUrl { get; set; }

    [JsonPropertyName("repository")]
    public McpRepository Repository { get; set; }

    [JsonPropertyName("official")]
    public McpOfficial Official { get; set; }

    [JsonPropertyName("icons")]
    public object Icons { get; set; }

    [JsonPropertyName("packages")]
    public List<McpPackage> Packages { get; set; }

    [JsonPropertyName("remotes")]
    public object Remotes { get; set; }

    [JsonPropertyName("publisher_meta")]
    public object PublisherMeta { get; set; }
}

internal class McpPackage
{
    [JsonPropertyName("transport")]
    public McpTransport Transport { get; set; }

    [JsonPropertyName("fileSha256")]
    public string FileSha256 { get; set; }

    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    [JsonPropertyName("runtimeHint")]
    public string RuntimeHint { get; set; }

    [JsonPropertyName("registryType")]
    public string RegistryType { get; set; }

    [JsonPropertyName("environmentVariables")]
    public List<McpEnvironmentVariable> EnvironmentVariables { get; set; }
}

internal class McpEnvironmentVariable
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("format")]
    public string Format { get; set; }

    [JsonPropertyName("isSecret")]
    public bool IsSecret { get; set; }

    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}

public class McpTransport
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
}

internal class McpRepository
{
    [JsonPropertyName("id")]
    public object Id { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; }

    [JsonPropertyName("subfolder")]
    public object Subfolder { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}

internal class McpOfficial
{

    [JsonPropertyName("is_latest")]
    public bool IsLatest { get; set; }

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }
}