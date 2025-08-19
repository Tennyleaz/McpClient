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

        if (Design.IsDesignMode)
            return;
        BtnBack.IsVisible = false;
        BtnSave.IsVisible = false;
        BtnRefresh.IsVisible = false;
    }

    private async void BtnSetting_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowSettings();
    }

    private void BtnChat_OnClick(object sender, RoutedEventArgs e)
    {
        ShowChat();
    }

    private void BtnService_OnClick(object sender, RoutedEventArgs e)
    {
        ShowServices();
    }

    private async void BtnBack_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowMain(false);
    }

    private async void BtnSave_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowMain(true);
    }

    private void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        Chat.ReloadWebview();
    }

    private async Task ShowSettings()
    {
        BtnBack.Content = "Cancel";

        MainPanel.IsVisible = false;
        McpSetting.IsVisible = true;
        Chat.IsVisible = false;
        McpService.IsVisible = false;
        BtnBack.IsVisible = true;
        BtnSave.IsVisible = true;
        BtnRefresh.IsVisible = false;

        BtnBack.IsEnabled = BtnSave.IsEnabled = false;
        await McpSetting.LoadConfig();
        BtnBack.IsEnabled = BtnSave.IsEnabled = true;
    }

    private void ShowChat()
    {
        BtnBack.Content = "Go Back";

        MainPanel.IsVisible = false;
        McpSetting.IsVisible = false;
        Chat.IsVisible = true;
        McpService.IsVisible = false;
        BtnBack.IsVisible = true;
        BtnSave.IsVisible = false;
        BtnRefresh.IsVisible = true;
    }

    private void ShowServices()
    {
        BtnBack.Content = "Go Back";

        MainPanel.IsVisible = false;
        McpSetting.IsVisible = false;
        Chat.IsVisible = false;
        McpService.IsVisible = true;
        BtnBack.IsVisible = true;
        BtnSave.IsVisible = false;
        BtnRefresh.IsVisible = false;

        McpService.LoadFromSettings();
    }

    private async Task ShowMain(bool isSave)
    {
        // save each settings if was visible
        if (isSave && McpSetting.IsVisible)
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
        BtnSave.IsVisible = false;
        BtnRefresh.IsVisible = false;
    }

    private void BtnLogout_OnClick(object sender, RoutedEventArgs e)
    {
        LogoutClick?.Invoke(this, EventArgs.Empty);
    }

    public async Task ReloadMainView()
    {
        await TaskView.RefreshCurrentTab(true);
    }

    private void BtnStore_OnClick(object sender, RoutedEventArgs e)
    {
        Uri uri = new Uri("https://www.google.com");
        var launcher = TopLevel.GetTopLevel(this).Launcher;
        launcher.LaunchUriAsync(uri);
    }
}
