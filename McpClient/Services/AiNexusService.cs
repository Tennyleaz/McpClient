using McpClient.Models;
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

namespace McpClient.Services;
internal class AiNexusService
{
    private const string BASE_URL = "http://192.168.41.208:5155";
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _options;

    public AiNexusService(string token)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(BASE_URL);
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        _options = new JsonSerializerOptions();
        _options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        _options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }

    public async Task<LoginResponse> Login(string username, string password)
    {
        var body = new
        {
            username = username,
            password = password
        };
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/v1/login", body);
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
        }

        return result;
    }

    #region Groups

    public async Task<List<Group>> GetAllGroups()
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/Groups");
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Group>>(json, _options);
        }

        return null;
    }

    public async Task<List<OfflineWorkflow>> GetOfflineGroups()
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/Groups/offline");
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<OfflineWorkflow>>(json, _options);
        }

        return null;
    }

    public async Task<(bool success, int groupId)> SetOfflineGroup(string serverName, string modelName, string endpointUrl, string payload)
    {
        MultipartFormDataContent content = new MultipartFormDataContent();
        if (!string.IsNullOrEmpty(serverName))
        {
            content.Add(new StringContent(serverName), "name");
        }
        if (!string.IsNullOrEmpty(modelName))
        {
            content.Add(new StringContent(modelName), "model");
        }
        if (!string.IsNullOrEmpty(endpointUrl))
        {
            content.Add(new StringContent(endpointUrl), "endpoint");
        }
        if (!string.IsNullOrEmpty(payload))
        {
            content.Add(new StringContent(payload), "payload");
        }

        HttpResponseMessage response = await _httpClient.PostAsync($"/api/v1/Groups/offline", content);
        bool success = response.IsSuccessStatusCode;
        int groupId = -1;
        if (success)
        {
            string json = await response.Content.ReadAsStringAsync();
            success = int.TryParse(json, out groupId);
        }

        return (success, groupId);
    }

    public async Task<Group> GetGroupById(int groupId)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/Groups/{groupId}");
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Group>(json, _options);
        }

        return null;
    }

    public async Task<bool> CreateGroup(Group group)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"/api/v1/Groups", group, _options);
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> SetGroupById(Group group)
    {
        HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"/api/v1/Groups/{group.Id}", group, _options);
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> DeleteGroupById(int groupId)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"/api/v1/Groups/{groupId}");
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    #endregion

    #region Execute workflow

    public async Task<(bool, string)> ExecuteOfflineWorkflow(int groupId, string model)
    {
        MultipartFormDataContent content = new MultipartFormDataContent();
        content.Add(new StringContent(groupId.ToString()), "group_id");
        content.Add(new StringContent(model), "model");
        HttpResponseMessage response = await _httpClient.PostAsync("/api/v1/AutoGen/offline", content);
        bool success = response.IsSuccessStatusCode;
        string json = await response.Content.ReadAsStringAsync();
        return (success, json);
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

    public async Task<(bool, string)> ExecuteStaticWorkflow(string connectionId, List<int> agents, int group, string query)
    {
        MultipartFormDataContent content = new MultipartFormDataContent();
        content.Add(new StringContent(group.ToString()), "group");

        if (!string.IsNullOrEmpty(connectionId))
        {
            content.Add(new StringContent(connectionId), "connectionId");
        }

        if (agents != null)
        {
            foreach (int agent in agents)
            {
                content.Add(new StringContent(agent.ToString()), "agents");
            }
        }
        
        if (!string.IsNullOrEmpty(query))
        {
            content.Add(new StringContent(query), "query");
        }

        HttpResponseMessage response = await _httpClient.PostAsync("/api/v1/AutoGen/static", content);
        bool success = response.IsSuccessStatusCode;
        string json = await response.Content.ReadAsStringAsync();
        return (success, json);
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
