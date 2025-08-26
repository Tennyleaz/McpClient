using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class StoreMcpResponse
{
    public List<StoreMcpServer> Servers { get; set; }
    public Pagination Pagination { get; set; }
}

internal class StoreMcpServer
{
    public string Name { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
    [JsonPropertyName("timestamp_text")]
    public string TimestampText { get; set; }
    public string Url { get; set; }
    public Logo Logo { get; set; }

    [JsonIgnore]
    public bool IsInstalled { get; set; }
}

internal class Logo
{
    public string Src { get; set; }

    public string Alt { get; set; }
}

internal class Pagination
{
    [JsonPropertyName("total_servers")]
    public int TotalServers { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("servers_per_page")]
    public int ServersPerPage { get; set; }
}