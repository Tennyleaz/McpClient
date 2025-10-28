using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using McpClient.Services;
using McpClient.ViewModels;
using System;
using System.Linq;
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

        MyAppList.DownloadGroup += MyAppList_DownloadGroup;
        DataContext = GlobalService.MainViewModel;
    }

    private void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        Chat.ReloadWebview();
    }

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
            SystemSetting.SetServices(_mcpService);
            // Trigger RefreshCurrentTab()
            //MainListbox.SelectedIndex = 0;
            SelectMainList(MainListType.Apps);
        }
        else
        {
            await RefreshCurrentTab();
        }
    }

    private void SelectMainList(MainListType listType)
    {
        MainListboxItem select = GlobalService.MainViewModel.MainItems.FirstOrDefault(x => x.ItemType == listType);
        MainListbox.SelectedItem = select;
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

        await RefreshCurrentTab();
    }

    private async Task RefreshCurrentTab()
    {
        MainListboxItem selected = MainListbox.SelectedItem as MainListboxItem;
        switch (selected.ItemType)
        {
            case MainListType.Apps:
            default:
                // My Apps
                MyAppList.IsVisible = true;
                OfflineWorkflowList.IsVisible = false;
                AgentList.IsVisible = false;
                Chat.IsVisible = false;
                McpStore.IsVisible = false;
                SystemSetting.IsVisible = false;

                await MyAppList.LoadGroupList(true);
                break;
            case MainListType.LocalWorkflow:
                // Local Workflows
                MyAppList.IsVisible = false;
                OfflineWorkflowList.IsVisible = true;
                AgentList.IsVisible = false;
                Chat.IsVisible = false;
                McpStore.IsVisible = false;
                SystemSetting.IsVisible = false;

                await OfflineWorkflowList.LoadOfflineList(true);
                break;
            case MainListType.McpTools:
                // Agents
                MyAppList.IsVisible = false;
                OfflineWorkflowList.IsVisible = false;
                AgentList.IsVisible = true;
                Chat.IsVisible = false;
                McpStore.IsVisible = false;
                SystemSetting.IsVisible = false;

                await AgentList.LoadConfig();
                break;
            case MainListType.Chat:
                // Chat
                MyAppList.IsVisible = false;
                OfflineWorkflowList.IsVisible = false;
                AgentList.IsVisible = false;
                Chat.IsVisible = true;
                McpStore.IsVisible = false;
                SystemSetting.IsVisible = false;

                Chat.LoadChatServer();
                break;
            case MainListType.McpStore:
                // Store
                MyAppList.IsVisible = false;
                OfflineWorkflowList.IsVisible = false;
                AgentList.IsVisible = false;
                Chat.IsVisible = false;
                McpStore.IsVisible = true;
                SystemSetting.IsVisible = false;

                await McpStore.LoadDefault();
                break;
            case MainListType.SystemService:
                // Services
                MyAppList.IsVisible = false;
                OfflineWorkflowList.IsVisible = false;
                AgentList.IsVisible = false;
                Chat.IsVisible = false;
                McpStore.IsVisible = false;
                SystemSetting.IsVisible = true;

                await SystemSetting.LoadFromSettings();
                break;
        }

        // stop monitoring
        if (selected.ItemType != MainListType.SystemService)
        {
            SystemSetting.StopMonitorServices();
        }
        if (selected.ItemType != MainListType.McpTools)
        {
            AgentList.StopUpdateStatus();
        }
        if (selected.ItemType != MainListType.Chat)
        {
            Chat.Deactivate();
        }
    }

    private async void MyAppList_DownloadGroup(object sender, Models.Group e)
    {
        // Tell MCP tool list to add those groups
        await AgentList.AddMcpFromGroup(e);
        // Switch to MCP tool tab
        //MainListbox.SelectedIndex = 2;
        SelectMainList(MainListType.McpTools);
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

    private async void Chat_OnReDirectMcpStore(object sender, string url)
    {
        // Invoke when chat webview recommend a MCP to download
        SelectMainList(MainListType.McpStore);
        await McpStore.GoToRecommend(url);
    }
}
