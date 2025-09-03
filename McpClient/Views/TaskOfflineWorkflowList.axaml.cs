using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using McpClient.Services;
using McpClient.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicData;
using McpClient.Models;

namespace McpClient.Views;

public partial class TaskOfflineWorkflowList : UserControl
{
    private AiNexusService _nexusService;
    private McpConfigService _mcpService;
    private WorkflowListViewModel groupListViewModel;

    public TaskOfflineWorkflowList()
    {
        InitializeComponent();
    }

    internal void SetServices(AiNexusService aiNexusService, McpConfigService mcpConfigService)
    {
        _nexusService = aiNexusService;
        _mcpService = mcpConfigService;
    }

    public async Task LoadOfflineList(bool forceRefresh)
    {
        // Do not refresh if there are items
        if (!forceRefresh && groupListViewModel != null && groupListViewModel.OfflineWorkflows.Count > 0)
        {
            LbEmptyList.IsVisible = false;
            DataContext = groupListViewModel;
            return;
        }

        // Load server status
        List<OfflineWorkflow> groups = await _nexusService.GetOfflineGroups();
        if (groups != null)
        {
            // Merge each server's status into main view model;
            if (groupListViewModel == null)
            {
                groupListViewModel = new WorkflowListViewModel();
                groupListViewModel.RunServer += GroupListViewModel_RunServer;
                groupListViewModel.Delete += GroupListViewModel_Delete;
            }
            else
            {
                groupListViewModel.OfflineWorkflows.Clear();
            }
            groupListViewModel.OfflineWorkflows.AddRange(groups);
            DataContext = groupListViewModel;
        }
        else
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Cannot get group list.",
                ButtonEnum.Ok,
                Icon.Error);
            Window owner = TopLevel.GetTopLevel(this) as Window;
            await box.ShowWindowDialogAsync(owner);
        }

        LbEmptyList.IsVisible = groupListViewModel == null || groupListViewModel.OfflineWorkflows.Count == 0;
    }

    private async void GroupListViewModel_Delete(object sender, OfflineWorkflow e)
    {
        var box = MessageBoxManager.GetMessageBoxStandard("Delete Workflow", $"Are you sure to delete \"{e.Name}\"?",
            ButtonEnum.YesNo,
            Icon.Question);
        Window owner = TopLevel.GetTopLevel(this) as Window;
        var result = await box.ShowWindowDialogAsync(owner);
        if (result == ButtonResult.Yes)
        {
            // Delete from server
            bool success = await _nexusService.DeleteOfflineGroupById(e.Id);
            if (success)
            {
                groupListViewModel.OfflineWorkflows.Remove(e);
            }
        }
    }

    private async void GroupListViewModel_RunServer(object sender, OfflineWorkflow e)
    {
        Window parent = TopLevel.GetTopLevel(this) as Window;
        RunOfflineWindow runOfflineWindow = new RunOfflineWindow(e, _nexusService, _mcpService);
        await runOfflineWindow.ShowDialog(parent);
    }

    public void ClearWorkflowList()
    {
        if (groupListViewModel != null)
            groupListViewModel.OfflineWorkflows.Clear();
    }
}