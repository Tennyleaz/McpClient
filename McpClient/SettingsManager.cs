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
    public static SettingsManager Local { get; } = new SettingsManager();

    private SettingsManager()
    {
        string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _settingDir = Path.Combine(appdata, "McpClient");
        _settingsPath = Path.Combine(_settingDir, "settings.json");
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
}

internal sealed record Settings
{
    public string UserName { get; set; }
    public string AiNexusToken { get; set; }
    public string McpConfigToken { get; set; }
    public DateTime ExpiredAt { get; set; }
}
