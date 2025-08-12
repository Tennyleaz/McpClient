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
    private const string BASE_URL = "http://192.168.41.60:7235";
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _options;

    public McpConfigService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _options = new JsonSerializerOptions();
        _options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        _options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }

    public async Task<McpServerConfig> GetConfig()
    {
        HttpResponseMessage response = await _httpClient.GetAsync(BASE_URL + "/chat/mcpserver/config");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        string json = await response.Content.ReadAsStringAsync();
        McpServerConfig config = JsonSerializer.Deserialize<McpServerConfig>(json, _options);
        return config;
    }

    public async Task<bool> SetConfig(McpServerConfig config)
    {
        string contentString = JsonSerializer.Serialize(config, _options);
        var body = new 
        {
            content = contentString
        };
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(BASE_URL + "/chat/mcpserver/config", body);
        return response.StatusCode == HttpStatusCode.OK;
    }

    public async Task<McpServerListResponse> ListCurrent()
    {
        HttpResponseMessage response = await _httpClient.GetAsync(BASE_URL + "/chat/mcpserver/list");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        string json = await response.Content.ReadAsStringAsync();
        McpServerListResponse servers = JsonSerializer.Deserialize<McpServerListResponse>(json, _options);
        return servers;
    }

    public async Task<LoginResponse> Login(string username, string password)
    {
        var body = new
        {
            account = username,
            password = password
        };
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(BASE_URL + "/user/login", body);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        string json = await response.Content.ReadAsStringAsync();
        LoginResponse result = JsonSerializer.Deserialize<LoginResponse>(json, _options);
        return result;
    }

    public async Task<bool> IsLogin(string token)
    {
        HttpRequestMessage httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(BASE_URL + "/user/isLogin"),
            Headers = {
                { HttpRequestHeader.Accept.ToString(), "application/json" },
                { "Authorization", $"Bearer {token}" }
            }
        };
        HttpResponseMessage response = await _httpClient.SendAsync(httpRequestMessage);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return false;
        }

        string json = await response.Content.ReadAsStringAsync();
        bool.TryParse(json, out bool result);
        return result;
    }
}

