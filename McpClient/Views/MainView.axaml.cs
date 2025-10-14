using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using McpClient.Services;
using System;
using System.Threading.Tasks;

namespace McpClient.Views;

public partial class MainView : UserControl
{
    public event EventHandler LogoutClick;
    private McpConfigService _mcpService;
    private AiNexusService _nexusService;

    public MainView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            return;
        //BtnBack.IsVisible = false;
        //BtnSave.IsVisible = false;
        //BtnRefresh.IsVisible = false;

        MyAppList.DownloadGroup += MyAppList_DownloadGroup;
        DataContext = GlobalService.MainViewModel;
    }

    private void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        Chat.ReloadWebview();
    }

    //private async Task ShowSettings()
    //{
    //    BtnBack.Content = "Cancel";

    //    MainPanel.IsVisible = false;
    //    McpSetting.IsVisible = true;
    //    Chat.IsVisible = false;
    //    SystemSetting.IsVisible = false;
    //    McpStore.IsVisible = false;

    //    BtnBack.IsVisible = true;
    //    BtnSave.IsVisible = true;
    //    BtnRefresh.IsVisible = false;

    //    BtnBack.IsEnabled = BtnSave.IsEnabled = false;
    //    await McpSetting.LoadConfig();
    //    BtnBack.IsEnabled = BtnSave.IsEnabled = true;
    //}


    private void BtnLogout_OnClick(object sender, RoutedEventArgs e)
    {
        LogoutClick?.Invoke(this, EventArgs.Empty);
    }

    public async Task ReloadMainView()
    {
        // Called after mainwindow login
        if (MainListbox.SelectedIndex < 0)
        {
            // First login, we have token now
            Settings settings = SettingsManager.Local.Load();
            _mcpService = new McpConfigService(settings.McpConfigToken);
            _nexusService = new AiNexusService(settings.AiNexusToken);
            MyAppList.SetService(_nexusService);
            OfflineWorkflowList.SetServices(_nexusService, _mcpService);
            AgentList.SetServices(_nexusService, _mcpService);
            // Trigger RefreshCurrentTab()
            MainListbox.SelectedIndex = 0;
        }
        else
        {
            await RefreshCurrentTab(MainListbox.SelectedIndex);
        }
    }

    private void BtnShowMonitor_OnClick(object sender, RoutedEventArgs e)
    {
        MonitorWindow monitorWindow = new MonitorWindow();
        monitorWindow.Show(TopLevel.GetTopLevel(this) as Window);
    }

    private async void MainListbox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Design.IsDesignMode || MainListbox == null)
            return;

        await RefreshCurrentTab(MainListbox.SelectedIndex);
    }

    private async Task RefreshCurrentTab(int index)
    {
        switch (index)
        {
            case 0:
                // My Apps
                MyAppList.IsVisible = true;
                OfflineWorkflowList.IsVisible = false;
                AgentList.IsVisible = false;
                Chat.IsVisible = false;
                McpStore.IsVisible = false;
                SystemSetting.IsVisible = false;

                await MyAppList.LoadGroupList(true);
                break;
            case 1:
                // Local Workflows
                MyAppList.IsVisible = false;
                OfflineWorkflowList.IsVisible = true;
                AgentList.IsVisible = false;
                Chat.IsVisible = false;
                McpStore.IsVisible = false;
                SystemSetting.IsVisible = false;

                await OfflineWorkflowList.LoadOfflineList(true);
                break;
            case 2:
                // Agents
                MyAppList.IsVisible = false;
                OfflineWorkflowList.IsVisible = false;
                AgentList.IsVisible = true;
                Chat.IsVisible = false;
                McpStore.IsVisible = false;
                SystemSetting.IsVisible = false;

                await AgentList.LoadConfig();
                break;
            case 3:
                // Chat
                MyAppList.IsVisible = false;
                OfflineWorkflowList.IsVisible = false;
                AgentList.IsVisible = false;
                Chat.IsVisible = true;
                McpStore.IsVisible = false;
                SystemSetting.IsVisible = false;

                Chat.LoadChatServer();
                break;
            case 4:
                // Store
                MyAppList.IsVisible = false;
                OfflineWorkflowList.IsVisible = false;
                AgentList.IsVisible = false;
                Chat.IsVisible = false;
                McpStore.IsVisible = true;
                SystemSetting.IsVisible = false;

                await McpStore.LoadDefault();
                break;
            case 5:
                // Services
                MyAppList.IsVisible = false;
                OfflineWorkflowList.IsVisible = false;
                AgentList.IsVisible = false;
                Chat.IsVisible = false;
                McpStore.IsVisible = false;
                SystemSetting.IsVisible = true;

                SystemSetting.LoadFromSettings();
                break;
        }
    }

    private async void MyAppList_DownloadGroup(object sender, Models.Group e)
    {
        // Tell MCP tool list to add those groups
        await AgentList.AddMcpFromGroup(e);
        // Switch to MCP tool tab
        MainListbox.SelectedIndex = 2;
    }

    private async void BtnUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        const string url = "http://ainexus.phison.com/desktop";
        await TopLevel.GetTopLevel(this).Launcher.LaunchUriAsync(new Uri(url));
    }

    private void Chat_OnTokenExpired(object sender, EventArgs e)
    {
        // Tell main window to logout
        LogoutClick?.Invoke(this, EventArgs.Empty);
    }
}
