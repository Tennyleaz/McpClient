using Avalonia.Controls;
using Avalonia.Interactivity;
using McpClient.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace McpClient.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        await Login();
    }

    private async void MainView_OnLogoutClick(object sender, EventArgs e)
    {
        // Delete saved token
        Settings settings = SettingsManager.Local.Load();
        settings.Token = null;
        settings.UserName = null;
        settings.ExpiredAt = default;
        await SettingsManager.Local.SaveAsync(settings);
        // Login again
        await Login();
    }

    private async Task Login()
    {
        // Test saved token
        Settings settings = SettingsManager.Local.Load();
        if (!string.IsNullOrWhiteSpace(settings.Token))
        {
            McpConfigService service = new McpConfigService(new HttpClient());
            bool success = await service.IsLogin(settings.Token);
            if (success)
            {
                // Do not login again
                return;
            }
            // Remove old token
            settings.Token = null;
            await SettingsManager.Local.SaveAsync(settings);
        }

        // Show login window
        LoginWindow loginWindow = new LoginWindow();
        await loginWindow.ShowDialog(this);
        string token = loginWindow.Token;
        if (string.IsNullOrEmpty(token))
        {
            // Close on no token
            Close();
        }
    }
}
