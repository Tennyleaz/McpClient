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
    private readonly string _settingsPath;
    public static SettingsManager Local { get; } = new SettingsManager();

    private SettingsManager()
    {
        string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _settingsPath = Path.Combine(appdata, "McpClient", "settings.json");
    }

    public async Task SaveAsync(Settings settings)
    {
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
            return new Settings();
        }
    }
}

internal sealed record Settings
{
    public string UserName { get; init; }
    public string Token { get; set; }
}
