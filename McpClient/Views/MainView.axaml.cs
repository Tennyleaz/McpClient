using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace McpClient.Views;

public partial class MainView : UserControl
{
    public event EventHandler LogoutClick;

    public MainView()
    {
        InitializeComponent();
    }

    private async void BtnSetting_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowSettings();
    }

    private async void BtnBack_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowMain();
    }

    private void BtnChat_OnClick(object sender, RoutedEventArgs e)
    {
        ShowChat();
    }

    private void BtnService_OnClick(object sender, RoutedEventArgs e)
    {
        ShowServices();
    }

    private async Task ShowSettings()
    {
        BtnBack.Content = "Save";

        MainPanel.IsVisible = false;
        McpSetting.IsVisible = true;
        Chat.IsVisible = false;
        McpService.IsVisible = false;
        BtnBack.IsVisible = true;

        await McpSetting.LoadConfig();
    }

    private void ShowChat()
    {
        BtnBack.Content = "Go Back";

        MainPanel.IsVisible = false;
        McpSetting.IsVisible = false;
        Chat.IsVisible = true;
        McpService.IsVisible = false;
        BtnBack.IsVisible = true;
    }

    private void ShowServices()
    {
        BtnBack.Content = "Go Back";

        MainPanel.IsVisible = false;
        McpSetting.IsVisible = false;
        Chat.IsVisible = false;
        McpService.IsVisible = true;
        BtnBack.IsVisible = true;
    }

    private async Task ShowMain()
    {
        // save each settings if was visible
        if (McpSetting.IsVisible)
        {
            bool success = await McpSetting.SaveConfig();
            if (!success)
            {
                return;
            }
        }

        MainPanel.IsVisible = true;
        McpSetting.IsVisible = false;
        Chat.IsVisible = false;
        McpService.IsVisible = false;
        BtnBack.IsVisible = false;
    }

    private void BtnLogout_OnClick(object sender, RoutedEventArgs e)
    {
        LogoutClick?.Invoke(this, EventArgs.Empty);
    }
}
