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
internal class AiNexusService
{
    private const string BASE_URL = "http://192.168.41.208:5155";
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _options;

    public AiNexusService()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(BASE_URL);
        
        _options = new JsonSerializerOptions();
        _options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        _options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }

    #region Groups

    public async Task<List<Group>> GetAllGroups(int userId)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/Groups/{userId}");
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Group>>(json, _options);
        }

        return null;
    }

    public async Task<Group> GetGroupById(int userId, int groupId)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/Groups/{userId}/{groupId}");
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Group>(json, _options);
        }

        return null;
    }

    public async Task<bool> CreateGroup(int userId, Group group)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"/api/v1/Groups/{userId}", group, _options);
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> SetGroupById(int userId, Group group)
    {
        HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"/api/v1/Groups/{userId}/{group.Id}", group, _options);
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> DeleteGroupById(int userId, int groupId)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"/api/v1/Groups/{userId}/{groupId}");
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    #endregion

    #region Execute workflow

    public async Task<bool> ExecuteOfflineWorkflow(string name, string endpoint, string payload)
    {
        var body = new 
        {
            name,
            endpoint,
            payload
        };
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/v1/AutoGen/offline", body, _options);
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> ExecuteDynamicWorkflow(string connectionId, string query)
    {
        var body = new
        {
            connectionId,
            query
        };
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/v1/AutoGen/dynamic", body, _options);
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> ExecuteStaticWorkflow(string connectionId, List<int> agents, int group, string query)
    {
        var body = new
        {
            connectionId,
            agents,
            group,
            query
        };
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/v1/AutoGen/static", body, _options);
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    #endregion

    #region Workflows

    public async Task<bool> CreateWorkflow(int groupId, Workflow workflow)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"/api/v1/Workflow?group_id={groupId}", workflow, _options);
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    public async Task<List<Workflow>> GetAllWorkflow(int groupId)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/Workflow/{groupId}");
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Workflow>>(json, _options);
        }

        return null;
    }

    public async Task<bool> SetWorkflow(int groupId, Workflow workflow)
    {
        HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"/api/v1/Workflow?group_id={groupId}", workflow, _options);
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> DeleteWorkflow(int groupId)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"/api/v1/Workflow?group_id={groupId}");
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    #endregion
}
