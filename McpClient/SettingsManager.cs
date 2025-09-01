using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace McpClient;

internal sealed class SettingsManager
{
    private readonly string _settingDir;
    private readonly string _settingsPath;
    private readonly string _historyPath;
    public static SettingsManager Local { get; } = new SettingsManager();

    private SettingsManager()
    {
        string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _settingDir = Path.Combine(appdata, "McpClient");
        _settingsPath = Path.Combine(_settingDir, "settings.json");
        _historyPath = Path.Combine(_settingDir, "documentHistory.json");
    }

    public async Task SaveAsync(Settings settings)
    {
        if (!Directory.Exists(_settingDir))
        {
            Directory.CreateDirectory(_settingDir);
        }

        using (var fs = new FileStream(_settingsPath, FileMode.Create))
        {
            await JsonSerializer.SerializeAsync(fs, settings);
        }
    }

    public void Save(Settings settings)
    {
        if (!Directory.Exists(_settingDir))
        {
            Directory.CreateDirectory(_settingDir);
        }

        using (var fs = new FileStream(_settingsPath, FileMode.Create))
        {
            JsonSerializer.Serialize(fs, settings);
        }
    }

    public Settings Load()
    {
        if (!File.Exists(_settingsPath))
            return new Settings();
        try
        {
            using (var fs = new FileStream(_settingsPath, FileMode.Open))
            {
                return JsonSerializer.Deserialize<Settings>(fs);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new Settings();
        }
    }

    public List<DocumentHistory> LoadDocumentHistories()
    {
        if (!File.Exists(_historyPath))
            return new List<DocumentHistory>();

        try
        {
            using (var fs = new FileStream(_historyPath, FileMode.Open))
            {
                return JsonSerializer.Deserialize<List<DocumentHistory>>(fs);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new List<DocumentHistory>();
        }
    }

    public async Task SaveDocumentHistoriesAsync(List<DocumentHistory> histories)
    {
        if (!Directory.Exists(_settingDir))
        {
            Directory.CreateDirectory(_settingDir);
        }

        using (var fs = new FileStream(_historyPath, FileMode.Create))
        {
            await JsonSerializer.SerializeAsync(fs, histories);
        }
    }
}

internal sealed record Settings
{
    public string UserName { get; set; }
    public string AiNexusToken { get; set; }
    public string McpConfigToken { get; set; }
    public DateTime ExpiredAt { get; set; }
    public string RagFolder { get; set; }
    public string LlmModelFile { get; set; }
    public string LlmRemoteUrl { get; set; }
    public bool IsUseRemoteLlm { get; set; }
    public bool IsDarkMode { get; set; }
}

internal sealed record DocumentHistory
{
    public string FullPath { get; set; }
    public DateTime UploadTime { get; set; }
}
