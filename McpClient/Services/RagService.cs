using HarfBuzzSharp;
using McpClient.Models;
using McpClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace McpClient.Services;

internal class RagService
{
    private const string BASE_URL = "http://localhost:6400";
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
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/v1/Documents", request, _options);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return false;
    }

    public async Task<List<RagDocuemntViewModel>> GetDocuments()
    {
        HttpResponseMessage response = await _httpClient.GetAsync("/api/v1/Documents");
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            List<RagDocuemntViewModel> documents = JsonSerializer.Deserialize<List<RagDocuemntViewModel>>(json, _options);
            return documents;
        }

        return null;
    }

    public async Task<bool> DeleteDocument(Guid id)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"/api/v1/Documents/{id}");
        return response.IsSuccessStatusCode;
    }
}
