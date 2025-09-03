using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using McpClient.Models;
using McpClient.ViewModels;

namespace McpClient.Services;

internal class McpConfigService
{
    private const string BASE_URL = "http://192.168.41.60:7235";
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _options;
    private string _token;

    public McpConfigService(string token, HttpClient httpClient = null)
    {
        if (httpClient == null)
            _httpClient = new HttpClient();
        else
            _httpClient = httpClient;
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _token = token;
        }

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

    public async Task<McpServerConfigViewModel> GetAllConfigAndStatus()
    {
        // Load all configs
        McpServerConfig config = await GetConfig();
        // Load server status
        McpServerListResponse listResponse = await ListCurrent();
        if (listResponse != null && config != null)
        {
            // Merge each server's status into main view model;
            McpServerConfigViewModel viewModel = new McpServerConfigViewModel(config, listResponse.data);
            return viewModel;
        }
        // Retun null on fail
        return null;
    }

    public async Task<LoginResponse> Login(string username, string password)
    {
        var body = new
        {
            account = username,
            password = password
        };
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(BASE_URL + "/user/login", body);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        string json = await response.Content.ReadAsStringAsync();
        LoginResponse result = JsonSerializer.Deserialize<LoginResponse>(json, _options);

        // Also set self's token for future use
        if (!string.IsNullOrEmpty(result.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
            _token = result.Token;
        }

        return result;
    }

    public async Task<bool> IsLogin()
    {
        //HttpRequestMessage httpRequestMessage = new HttpRequestMessage
        //{
        //    Method = HttpMethod.Get,
        //    RequestUri = new Uri(BASE_URL + "/user/isLogin"),
        //    Headers = {
        //        { HttpRequestHeader.Accept.ToString(), "application/json" },
        //        { "Authorization", $"Bearer {token}" }
        //    }
        //};
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(BASE_URL + "/user/isLogin");
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                bool.TryParse(json, out bool result);
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return false;
    }

    public async Task<ModelData> ListModels()
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(BASE_URL + "/chat/model/list");
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            ModelData servers = JsonSerializer.Deserialize<ModelData>(json, _options);
            return servers;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }
}

