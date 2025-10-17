using McpClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
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
    private const string BASE_URL = "http://192.168.41.133:5155";
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _options;

    public AiNexusService(string token)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(BASE_URL);
        //_httpClient.Timeout = TimeSpan.FromSeconds(10);
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
        try
        {
            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/v1/login", body);
            string json = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return new LoginResponse { ErrorMessage = json };
            }
            
            LoginResponse result = JsonSerializer.Deserialize<LoginResponse>(json, _options);

            // Also set self's token for future use
            if (!string.IsNullOrEmpty(result.Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new LoginResponse { ErrorMessage = ex.Message };
        }
    }

    #region Groups

    public async Task<List<Group>> GetAllGroups()
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/Groups");
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Group>>(json, _options);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return null;
    }

    public async Task<List<OfflineWorkflow>> GetOfflineGroups()
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/Groups/offline");
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<OfflineWorkflow>>(json, _options);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
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

        HttpResponseMessage response = await _httpClient.PostAsync($"/api/v1/Group/offline", content);
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
        HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/Group/{groupId}");
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Group>(json, _options);
        }

        return null;
    }

    public async Task<bool> CreateGroup(Group group)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"/api/v1/Group", group, _options);
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> SetGroupById(Group group)
    {
        HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"/api/v1/Group/{group.Id}", group, _options);
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> DeleteGroupById(int groupId)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"/api/v1/Group/{groupId}");
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> DeleteOfflineGroupById(int groupId)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"/api/v1/Group/offline/{groupId}");
        if (response.IsSuccessStatusCode)
        {
            //string json = await response.Content.ReadAsStringAsync();
            return true;
        }

        return false;
    }

    #endregion

    #region Execute workflow

    public async IAsyncEnumerable<AutogenStreamResponse> ExecuteOfflineWorkflow(int groupId, string model, string message)
    {
        MultipartFormDataContent content = new MultipartFormDataContent();
        content.Add(new StringContent(groupId.ToString()), "group_id");
        content.Add(new StringContent(model), "model");
        if (!string.IsNullOrWhiteSpace(message))
            content.Add(new StringContent(message), "message");

        // Making a SSE request
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/AutoGen/offline");
        request.Content = content;

        using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        using Stream stream = await response.Content.ReadAsStreamAsync();
        using StreamReader reader = new StreamReader(stream);
        string sseEvent = null;
        string sseData = null;

        while (!reader.EndOfStream)
        {
            string line = await reader.ReadLineAsync();

            // Check if line is empty, ready to parse
            if (string.IsNullOrWhiteSpace(line))
            {
                // Finished reading one SSE event, now process it
                if (!string.IsNullOrEmpty(sseData))
                {
                    // Some events (like :heartbeat) may not be JSON
                    AutogenStreamResponse autogenResponse = null;
                    try
                    {
                        autogenResponse = JsonSerializer.Deserialize<AutogenStreamResponse>(sseData, _options);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("Cannot parse sseData: " + sseData);
                    }

                    if (autogenResponse != null)
                        yield return autogenResponse;

                    // You can add your own logic/handlers here, e.g. break if IsTerminated is true
                    //if (autogenResponse.IsTerminated)
                    //{
                    //    Console.WriteLine("Stream terminated by server. Exiting SSE loop.");
                    //    break;
                    //}
                }
                // Reset event/data for next message
                sseEvent = null;
                sseData = null;
                continue;
            }

            // Each SSE line is of the form: "field: value"
            if (line.StartsWith("event:"))
            {
                sseEvent = line.Substring("event:".Length).Trim();
            }
            else if (line.StartsWith("data:"))
            {
                string dataLine = line.Substring("data:".Length).Trim();
                if (sseData == null)
                    sseData = dataLine;
                else
                    sseData += "\n" + dataLine; // multi-line data support
            }
            else if (line.StartsWith("[DONE]"))
            {
                sseEvent = line.Substring("[DONE]".Length).Trim();
            }
        }
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

    public async IAsyncEnumerable<AutogenResponse> ExecuteStaticWorkflow(AutoGenRequest autoGenRequest)
    {
        MultipartFormDataContent content = new MultipartFormDataContent();
        content.Add(new StringContent(autoGenRequest.group.ToString()), "group");

        if (!string.IsNullOrEmpty(autoGenRequest.connectionId))
        {
            content.Add(new StringContent(autoGenRequest.connectionId), "connectionId");
        }

        if (autoGenRequest.agents != null)
        {
            foreach (int agent in autoGenRequest.agents)
            {
                content.Add(new StringContent(agent.ToString()), "agents");
            }
        }

        if (!string.IsNullOrEmpty(autoGenRequest.query))
        {
            content.Add(new StringContent(autoGenRequest.query), "query");
        }

        // Making a SSE request
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/AutoGen/static");
        request.Content = content;

        using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        using Stream stream = await response.Content.ReadAsStreamAsync();
        using StreamReader reader = new StreamReader(stream);
        string sseEvent = null;
        string sseData = null;

        while (!reader.EndOfStream)
        {
            string line = await reader.ReadLineAsync();

            // Check if line is empty, ready to parse
            if (string.IsNullOrWhiteSpace(line))
            {
                // Finished reading one SSE event, now process it
                if (!string.IsNullOrEmpty(sseData))
                {
                    // Some events (like :heartbeat) may not be JSON
                    AutogenResponse autogenResponse;
                    try
                    {
                        autogenResponse = JsonSerializer.Deserialize<AutogenResponse>(sseData, _options);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        break;
                    }

                    //Console.WriteLine($"EVENT: {sseEvent ?? "data"}");
                    //Console.WriteLine($"Type={autogenResponse.Type}, From={autogenResponse.From}, Response={autogenResponse.Response}, IsTerminated={autogenResponse.IsTerminated}");

                    yield return autogenResponse;

                    // You can add your own logic/handlers here, e.g. break if IsTerminated is true
                    if (autogenResponse.IsTerminated)
                    {
                        Console.WriteLine("Stream terminated by server. Exiting SSE loop.");
                        break;
                    }
                }
                // Reset event/data for next message
                sseEvent = null;
                sseData = null;
                continue;
            }

            // Each SSE line is of the form: "field: value"
            if (line.StartsWith("event:"))
            {
                sseEvent = line.Substring("event:".Length).Trim();
            }
            else if (line.StartsWith("data:"))
            {
                string dataLine = line.Substring("data:".Length).Trim();
                if (sseData == null)
                    sseData = dataLine;
                else
                    sseData += "\n" + dataLine; // multi-line data support
            }
        }
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

    #region MCP store

    public async Task<StoreMcpResponse> GetStoreMcpServers(string tag, string category, string type, int page)
    {
        string url = "/api/v1/mcpServer";
        if (!string.IsNullOrEmpty(tag))
            url += $"?tag={tag}";
        else if (!string.IsNullOrEmpty(type))
            url += $"?type={type}";
        else
            url += $"?category={category}";
        url += $"&page={page}&debug=false";

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                StoreMcpResponse result = JsonSerializer.Deserialize<StoreMcpResponse>(json, _options);
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return null;
    }

    public async Task<StoreMcpResponse> SearchMcpServers(string query, int page)
    {
        string url = "/api/v1/mcpServer";
        url += $"?query={Uri.EscapeDataString(query)}";
        url += $"&page={page}&debug=false";

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                StoreMcpResponse result = JsonSerializer.Deserialize<StoreMcpResponse>(json, _options);
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return null;
    }

    public async Task<StoreMcpServerDetailBase> GetStoreMcpServerDetail(string serverUrl)
    {
        string url = $"api/v1/mcpServerDetail?url={Uri.EscapeDataString(serverUrl)}";

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                try
                {
                    StoreMcpServerDetail result = JsonSerializer.Deserialize<StoreMcpServerDetail>(json, _options);
                    return result;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine(ex);
                    StoreMcpServerDetailBase baseResult = JsonSerializer.Deserialize<StoreMcpServerDetailBase>(json, _options);
                    return baseResult;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return null;
    }

    #endregion

    #region Chat

    public async Task<TranscriptResponse> PostTranscriptAsync(string fileName)
    {
        MultipartFormDataContent content = new MultipartFormDataContent();

        byte[] buffer = await File.ReadAllBytesAsync(fileName);
        ByteArrayContent fileContent = new ByteArrayContent(buffer);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(fileContent, "audioFile", Path.GetFileName(fileName));

        string url = $"api/v1/chat/transcript";
        using HttpResponseMessage response = await _httpClient.PostAsync(url, content);
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TranscriptResponse>(json, _options);
        }

        return null;
    }

    #endregion
}
