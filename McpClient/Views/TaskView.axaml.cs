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
    private readonly McpConfigService _service;
    private McpServerListViewModel mcpListViewModel;
    private McpServerListViewModel myTasksViewModel;

    public TaskView()
    {
        InitializeComponent();
        _service = new McpConfigService(new HttpClient());
    }

    private async void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;
        //await LoadList();
    }

    public async Task LoadMcpList(bool forceRefresh)
    {
        // Do not refresh if there are items
        if (!forceRefresh && mcpListViewModel != null && mcpListViewModel.ServerNames.Count > 0)
        {
            LbEmptyList.IsVisible = false;
            DataContext = mcpListViewModel;
            return;
        }

        // Show progress
        ShowProgress();

        // Load server status
        McpServerListResponse listResponse = await _service.ListCurrent();
        if (listResponse != null)
        {
            // Merge each server's status into main view model;
            if (mcpListViewModel == null)
            {
                mcpListViewModel = new McpServerListViewModel();
                mcpListViewModel.RunServer += ViewModel_RunServer;
            }
            else
            {
                mcpListViewModel.ServerNames.Clear();
            }
            foreach (McpServerItem item in listResponse.data)
            {
                mcpListViewModel.ServerNames.Add(item.server_name);
            }
            DataContext = mcpListViewModel;
        }
        else
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Cannot get MCP list server.",
                ButtonEnum.Ok,
                Icon.Error);
            var result = await box.ShowAsync();
        }

        LbEmptyList.IsVisible = mcpListViewModel == null || mcpListViewModel.ServerNames.Count == 0;
        HideProgress();
    }

    private async Task LoadMyTasks(bool forceRefresh)
    {
        // Show progress
        ShowProgress();

        // Load server status
        await Task.Delay(1000);
        if (myTasksViewModel == null)
        {
            myTasksViewModel = new McpServerListViewModel();
            //myTasksViewModel.RunServer += ViewModel_RunServer;
        }
        else
        {
            myTasksViewModel.ServerNames.Clear();
        }
        DataContext = myTasksViewModel;

        LbEmptyList.IsVisible = myTasksViewModel == null || myTasksViewModel.ServerNames.Count == 0;
        HideProgress();
    }

    private async void ViewModel_RunServer(object sender, string e)
    {
        Window parent = TopLevel.GetTopLevel(this) as Window;
        RunTaskWindow runTaskWindow = new RunTaskWindow(e);
        await runTaskWindow.ShowDialog(parent);
    }

    private async void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        await RefreshCurrentTab(true);
    }

    private async void Tabs_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await RefreshCurrentTab(false);
    }

    private async Task RefreshCurrentTab(bool forceRefresh)
    {
        if (Tabs == null)
            return;
        if (Design.IsDesignMode)
            return;
        Tabs.IsEnabled = false;
        if (Tabs.SelectedIndex == 0)
        {
            // My tasks
            await LoadMyTasks(forceRefresh);
        }
        else
        {
            // MCP tools
            await LoadMcpList(forceRefresh);
        }
        Tabs.IsEnabled = true;
    }

    private void ShowProgress()
    {
        TaskList.IsVisible = false;
        ProgressRing.IsVisible = true;
        BtnRefresh.IsEnabled = false;
        LbEmptyList.IsVisible = false;
    }

    private void HideProgress()
    {
        ProgressRing.IsVisible = false;
        TaskList.IsVisible = true;
        BtnRefresh.IsEnabled = true;
    }
}