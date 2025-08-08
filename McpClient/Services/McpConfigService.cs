using McpClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace McpClient.Services;

internal class McpConfigService
{
    private const string BASE_URL = "http://192.168.41.60:7235/chat/mcpserver";
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _options;

    public McpConfigService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _options = new JsonSerializerOptions();
        _options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    }

    public async Task<McpServerConfig> GetConfig()
    {
        HttpResponseMessage response = await _httpClient.GetAsync(BASE_URL + "/config");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        string json = await response.Content.ReadAsStringAsync();
        McpServerConfig config = JsonSerializer.Deserialize<McpServerConfig>(json);
        return config;
    }

    public async Task<bool> SetConfig(McpServerConfig config)
    {
        string contentString = JsonSerializer.Serialize(config, _options);
        var body = new 
        {
            content = contentString
        };
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(BASE_URL + "/config", body);
        return response.StatusCode == HttpStatusCode.OK;
    }

    public async Task<McpServerListResponse> ListCurrent()
    {
        HttpResponseMessage response = await _httpClient.GetAsync(BASE_URL + "/list");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        string json = await response.Content.ReadAsStringAsync();
        McpServerListResponse servers = JsonSerializer.Deserialize<McpServerListResponse>(json);
        return servers;
    }
}

