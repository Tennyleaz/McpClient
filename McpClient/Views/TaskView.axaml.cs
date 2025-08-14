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
    public TaskView()
    {
        InitializeComponent();
    }

    private async void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;
        //await LoadList();
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
        if (Tabs.SelectedIndex == 0)
        {
            // My tasks
            GroupList.IsVisible = true;
            McpToolList.IsVisible = false;
            await GroupList.LoadGroupList(forceRefresh);
        }
        else
        {
            // MCP tools
            GroupList.IsVisible = false;
            McpToolList.IsVisible = true;
            await McpToolList.LoadMcpList(forceRefresh);
        }
        HideProgress();
        
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
}