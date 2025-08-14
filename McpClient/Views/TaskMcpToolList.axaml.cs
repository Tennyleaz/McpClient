using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using McpClient.Models;
using McpClient.Services;
using McpClient.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Net.Http;
using System.Threading.Tasks;

namespace McpClient.Views;

public partial class TaskMcpToolList : UserControl
{
    private readonly McpConfigService _service;
    private McpServerListViewModel mcpListViewModel;

    public TaskMcpToolList()
    {
        InitializeComponent();
        _service = new McpConfigService(new HttpClient());
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
        //ShowProgress();

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
        //HideProgress();
    }

    private async void ViewModel_RunServer(object sender, string e)
    {
        Window parent = TopLevel.GetTopLevel(this) as Window;
        RunTaskWindow runTaskWindow = new RunTaskWindow(e);
        await runTaskWindow.ShowDialog(parent);
    }
}