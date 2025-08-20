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
    private McpConfigService _mcpService;
    private AiNexusService _nexusService;
    //private McpServerListViewModel mcpListViewModel;
    private McpServerConfigViewModel viewModel;

    public TaskMcpToolList()
    {
        InitializeComponent();
    }

    internal void SetServices(AiNexusService aiNexusService, McpConfigService mcpConfigService)
    {
        _nexusService = aiNexusService;
        _mcpService = mcpConfigService;
    }

    public async Task LoadMcpList(bool forceRefresh)
    {
        // Do not refresh if there are items
        if (!forceRefresh && viewModel != null && viewModel.McpServers.Count > 0)
        {
            LbEmptyList.IsVisible = false;
            DataContext = viewModel;
            return;
        }

        // Show progress
        //ShowProgress();

        // Load server status
        //McpServerListResponse listResponse = await _mcpService.ListCurrent();
        TaskList.IsEnabled = false;
        viewModel = await _mcpService.GetAllConfigAndStatus();
        if (viewModel != null)
        {
            viewModel.Restart += ViewModel_Restart;
            DataContext = viewModel;
        }
        else
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Cannot get MCP list server.",
                ButtonEnum.Ok,
                Icon.Error);
            var result = await box.ShowAsync();
        }

        LbEmptyList.IsVisible = viewModel == null || viewModel.McpServers.Count == 0;
        TaskList.IsEnabled = true;
        //HideProgress();
    }

    internal async Task AddMcpFromGroup(Group group)
    {
        McpServerConfig config = viewModel.ToModel();
        // Check for duplicate name
        bool added = false;
        foreach (Agent agent in group.Workflows)
        {
            if (config.mcp_servers.Exists(x => x.server_name == agent.Name))
            {
                continue;
            }
            // Agents are SSE
            config.mcp_servers.Add(new McpServer
            {
                enabled = true,
                server_name = agent.Name,
                type = "streamableHttp",
                streamable_http_url = agent.Url,
                source = "cloud",
                //owner = "",
            });
            added = true;
        }

        if (added)
        {
            // Update to server
            TaskList.IsEnabled = false;
            bool success = await _mcpService.SetConfig(config);
            if (!success)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Cannot add agents to MCP tools!",
                    ButtonEnum.Ok,
                    Icon.Error);
                await box.ShowAsync();
                TaskList.IsEnabled = true;
                return;
            }

            // Reload the list
            await LoadMcpList(true);
            TaskList.IsEnabled = true;
        }
        else
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Info", "You've already downlowd all the MCP tools.",
                ButtonEnum.Ok,
                Icon.Info);
            await box.ShowAsync();
        }
    }

    private async void ViewModel_Restart(object sender, McpViewModel e)
    {
        //IsEnabled = false;
        e.IsBusy = true;

        bool success = true;
        if (e.Enabled)
        {
            e.Enabled = false;
            McpServerConfig config = viewModel.ToModel();
            success = await _mcpService.SetConfig(config);
        }
        e.Enabled = true;

        if (success)
        {
            McpServerConfig config = viewModel.ToModel();
            success = await _mcpService.SetConfig(config);
        }

        e.IsBusy = false;
        //IsEnabled = true;
    }

    //private async void ViewModel_RunServer(object sender, string e)
    //{
    //    Window parent = TopLevel.GetTopLevel(this) as Window;
    //    RunLocalMcpWindow runTaskWindow = new RunLocalMcpWindow(e, _nexusService);
    //    await runTaskWindow.ShowDialog(parent);
    //}
}