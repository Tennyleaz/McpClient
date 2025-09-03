using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using McpClient.Models;
using McpClient.Services;
using McpClient.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

namespace McpClient.Views;

public partial class TaskView : UserControl
{
    private McpConfigService _mcpService;
    private AiNexusService _nexusService;

    public TaskView()
    {
        InitializeComponent();
    }

    private void TryCreateService()
    {
        if (_mcpService != null && _nexusService != null)
            return;
        Settings settings = SettingsManager.Local.Load();
        _mcpService = new McpConfigService(settings.McpConfigToken);
        _nexusService = new AiNexusService(settings.AiNexusToken);
        GroupList.SetService(_nexusService);
        OfflineWorkflowList.SetServices(_nexusService, _mcpService);
        McpToolList.SetServices(_nexusService, _mcpService);
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;
        GroupList.DownloadGroup += GroupList_OnDownloadGroup;
    }

    private async void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        await RefreshCurrentTab(true);
    }

    private async void Tabs_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await RefreshCurrentTab(false);
    }

    public async Task RefreshCurrentTab(bool forceRefresh)
    {
        if (Tabs == null)
            return;
        if (Design.IsDesignMode)
            return;
        
        ShowProgress();
        TryCreateService();
        if (Tabs.SelectedIndex == 0)
        {
            // My tasks
            GroupList.IsVisible = true;
            OfflineWorkflowList.IsVisible = false;
            McpToolList.IsVisible = false;
            await GroupList.LoadGroupList(forceRefresh);
        }
        else if (Tabs.SelectedIndex == 1)
        {
            // Offline workflow
            GroupList.IsVisible = false;
            OfflineWorkflowList.IsVisible = true;
            McpToolList.IsVisible = false;
            await OfflineWorkflowList.LoadOfflineList(forceRefresh);
        }
        else
        {
            // MCP tools
            GroupList.IsVisible = false;
            OfflineWorkflowList.IsVisible = false;
            McpToolList.IsVisible = true;
            await McpToolList.LoadMcpList(forceRefresh);
        }
        HideProgress();
    }

    public async Task RefreshMcpTools()
    {
        if (Tabs.SelectedIndex == 2)
        {
            await RefreshCurrentTab(true);
        }
        else
        {
            // Just clear the list, will refresh on next focus
            McpToolList.ClearMcpList();
        }
    }

    public async Task RefreshOfflineWorkflows()
    {
        if (Tabs.SelectedIndex == 1)
        {
            await RefreshCurrentTab(true);
        }
        else
        {
            // Just clear the list, will refresh on next focus
            OfflineWorkflowList.ClearWorkflowList();
        }
    }

    private void ShowProgress()
    {
        ProgressRing.IsVisible = true;
        BtnRefresh.IsEnabled = false;
        Tabs.IsEnabled = false;
    }

    private void HideProgress()
    {
        ProgressRing.IsVisible = false;
        BtnRefresh.IsEnabled = true;
        Tabs.IsEnabled = true;
    }

    internal async void GroupList_OnDownloadGroup(object sender, Group e)
    {
        // Switch to MCP tool tab
        Tabs.SelectedIndex = 2;
        // Tell MCP tool list to add those groups
        ShowProgress();
        await McpToolList.AddMcpFromGroup(e);
        HideProgress();
    }
}