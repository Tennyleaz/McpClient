using McpClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace McpClient.Services;

internal class RagService
{
    private const string BASE_URL = "http://192.168.41.173:8080";
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _options;

    public RagService()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(BASE_URL);

        _options = new JsonSerializerOptions();
        _options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        _options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }

    public async Task<bool> UploadDocument(DocumentDto request)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/v1/Documents", request, _options);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        return false;
    }
}
