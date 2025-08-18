using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using McpClient.Models;
using McpClient.Services;
using McpClient.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Threading.Tasks;
using DynamicData;

namespace McpClient.Views;

public partial class TaskGroupList : UserControl
{
    private AiNexusService _service;
    private GroupListViewModel groupListViewModel;

    public TaskGroupList()
    {
        InitializeComponent();
    }

    internal void SetService(AiNexusService service)
    {
        _service = service;
    }

    public async Task LoadGroupList(bool forceRefresh)
    {
        // Do not refresh if there are items
        if (!forceRefresh && groupListViewModel != null && groupListViewModel.Groups.Count > 0)
        {
            LbEmptyList.IsVisible = false;
            DataContext = groupListViewModel;
            return;
        }

        // Show progress
        //ShowProgress();

        // Load server status
        List<Group> groups = await _service.GetAllGroups();
        if (groups != null)
        {
            // Merge each server's status into main view model;
            if (groupListViewModel == null)
            {
                groupListViewModel = new GroupListViewModel();
                groupListViewModel.RunServer += ViewModel_RunServer;
            }
            else
            {
                groupListViewModel.Groups.Clear();
            }
            groupListViewModel.Groups.AddRange(groups);
            DataContext = groupListViewModel;
        }
        else
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Cannot get group list.",
                ButtonEnum.Ok,
                Icon.Error);
            var result = await box.ShowAsync();
        }

        LbEmptyList.IsVisible = groupListViewModel == null || groupListViewModel.Groups.Count == 0;
        //HideProgress();
    }

    private async void ViewModel_RunServer(object sender, Group e)
    {
        Window parent = TopLevel.GetTopLevel(this) as Window;
        RunStaticTaskWindow runStaticTaskWindow = new RunStaticTaskWindow(e, _service);
        await runStaticTaskWindow.ShowDialog(parent);
    }
}