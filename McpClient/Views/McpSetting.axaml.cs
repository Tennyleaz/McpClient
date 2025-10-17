using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaProgressRing;
using LibreHardwareMonitor.Hardware.Motherboard;
using McpClient.Models;
using McpClient.Services;
using McpClient.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace McpClient.Views;

public partial class McpSetting : UserControl
{
    private McpConfigService _mcpService;
    private AiNexusService _nexusService;
    private McpServerConfigViewModel viewModel;
    private readonly DispatcherTimer updateStatusTimer;

    public McpSetting()
    {
        InitializeComponent();

        updateStatusTimer = new DispatcherTimer();
        updateStatusTimer.Interval = TimeSpan.FromSeconds(3);
        updateStatusTimer.Tick += UpdateStatusTimer_Tick;
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;
    }

    internal void SetServices(AiNexusService aiNexusService, McpConfigService mcpConfigService)
    {
        _nexusService = aiNexusService;
        _mcpService = mcpConfigService;
    }

    public async Task LoadConfig()
    {
        if (Design.IsDesignMode)
            return;

        // Show progress
        ProgressRing.IsVisible = true;
        ScrollViewer.IsVisible = false;
        BtnAdd.IsVisible = false;
        // Scroll back to top
        ScrollViewer.ScrollToHome();
        // Load server config and status at once
        viewModel = await _mcpService.GetAllConfigAndStatus();
        if (viewModel != null)
        {
            DataContext = viewModel;

            ProgressRing.IsVisible = false;
            ScrollViewer.IsVisible = true;
            BtnAdd.IsVisible = true;

            updateStatusTimer.Start();
        }
        else
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Cannot get MCP list server.",
                ButtonEnum.Ok,
                Icon.Error);
            Window owner = TopLevel.GetTopLevel(this) as Window;
            await box.ShowWindowDialogAsync(owner);
        }
    }

    public async Task<bool> SaveConfig()
    {
        if (Design.IsDesignMode)
            return true;

        if (DataContext is McpServerConfigViewModel vm)
        {
            ProgressRing.IsVisible = true;
            ScrollViewer.IsVisible = false;
            BtnAdd.IsVisible = false;
            McpServerConfig config = vm.ToModel();
            // Send back to server
            bool success = await _mcpService.SetConfig(config);
            ProgressRing.IsVisible = false;
            ScrollViewer.IsVisible = false;
            BtnAdd.IsVisible = false;
            if (!success)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Cannot update MCP config to server.",
                    ButtonEnum.Ok,
                    Icon.Error);
                Window owner = TopLevel.GetTopLevel(this) as Window;
                await box.ShowWindowDialogAsync(owner);
                return false;
            }
        }
        return true;
    }

    internal async Task AddMcpFromGroup(Group group)
    {
        // If viewModel is null or empty, load it first
        if (viewModel == null || viewModel.McpServers.Count == 0)
        {
            await LoadConfig();
        }

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
                type = McpServerType.StreamableHttp,
                streamable_http_url = agent.Url,
                source = "cloud",
                //owner = "",
            });
            added = true;
        }

        Window owner = TopLevel.GetTopLevel(this) as Window;
        if (added)
        {
            // Update to server
            MsItems.IsEnabled = false;
            bool success = await _mcpService.SetConfig(config);
            if (!success)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Cannot add agents to MCP tools!",
                    ButtonEnum.Ok,
                    Icon.Error);
                await box.ShowWindowDialogAsync(owner);
                MsItems.IsEnabled = true;
                return;
            }

            // Reload the list
            await LoadConfig();
            MsItems.IsEnabled = true;
        }
        else
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Info", "You've already downlowd all the MCP tools.",
                ButtonEnum.Ok,
                Icon.Info);
            await box.ShowWindowDialogAsync(owner);
        }
    }

    private async Task UpdateServerStatus()
    {
        McpServerListResponse listResponse = await _mcpService.ListCurrent();
        if (listResponse != null)
        {
            foreach (McpViewModel server in viewModel.McpServers)
            {
                bool isActive = listResponse.data.Exists(x => x.server_name == server.ServerName);
                server.Active = isActive;
            }
        }
    }

    private async void UpdateStatusTimer_Tick(object sender, EventArgs e)
    {
        await UpdateServerStatus();
    }

    internal void StopUpdateStatus()
    {
        updateStatusTimer.Stop();
    }

    private async void BtnAdd_OnClick(object sender, RoutedEventArgs e)
    {
        Window parent = TopLevel.GetTopLevel(this) as Window;
        AddServerWindow addWindow = new AddServerWindow();
        await addWindow.ShowDialog(parent);
        McpServer newServer = addWindow.Result;
        if (newServer != null && DataContext is McpServerConfigViewModel vm)
        {
            // Add to server
            McpServerConfig config = vm.ToModel();
            config.mcp_servers.Add(newServer);
            bool success = await _mcpService.SetConfig(config);
            if (success)
            {
                // Add to UI
                vm.McpServers.Add(new McpViewModel(newServer, true));
                // Scroll to end
                ScrollViewer.ScrollToEnd();
            }
            else
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Cannot add new MCP config to server.",
                    ButtonEnum.Ok,
                    Icon.Error);
                Window owner = TopLevel.GetTopLevel(this) as Window;
                await box.ShowWindowDialogAsync(owner);
            }
        }
    }

    private async void BtnEditArgs_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is McpViewModel vm)
        {
            ArgsEditorViewModel argsEditorViewModel = new ArgsEditorViewModel();
            // Copy the data
            foreach (string arg in vm.Args)
            {
                argsEditorViewModel.Args.Add(new ArgsEditorItem(arg));
            }
            // Show edit window
            Window parent = TopLevel.GetTopLevel(this) as Window;
            ArgsEditor editWindow = new ArgsEditor();
            editWindow.DataContext = argsEditorViewModel;
            await editWindow.ShowDialog(parent);
            // Replace whole collection, so UI could change
            vm.Args = argsEditorViewModel.ToCollenction();
        }
    }

    private async void BtnEditEnv_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is McpViewModel vm)
        {
            EnvEditorViewModel envEditorViewModel = new EnvEditorViewModel();
            // Copy the data
            foreach (var pair in vm.Env)
            {
                envEditorViewModel.Env.Add(new EnvironmentItem()
                {
                    Name = pair.Key,
                    Value = pair.Value
                });
            }
            // Show edit window
            Window parent = TopLevel.GetTopLevel(this) as Window;
            EnvEditor editWindow = new EnvEditor(false);
            editWindow.DataContext = envEditorViewModel;
            await editWindow.ShowDialog(parent);
            // Replace whole collection, so UI could change
            vm.Env = envEditorViewModel.ToCollenction();
        }
    }

    private async void BtnRestart_OnClick(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;

        if (sender is Button btn && btn.DataContext is McpViewModel vm)
        {
            vm.IsBusy = true;
            // Set enable to false than true
            vm.Enabled = false;
            var configVm = DataContext as McpServerConfigViewModel;
            McpServerConfig config = configVm.ToModel();
            bool success = await _mcpService.SetConfig(config);
            if (success)
            {
                vm.Enabled = true;
                config = configVm.ToModel();
                success = await _mcpService.SetConfig(config);
            }

            vm.IsBusy = false;
        }
    }

    private async void BtnDelete_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is McpViewModel vm)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Delete Agent", $"Are you sure to delete \"{vm.ServerName}\"?",
                ButtonEnum.OkCancel,
                Icon.Question);
            Window owner = TopLevel.GetTopLevel(this) as Window;
            var result = await box.ShowWindowDialogAsync(owner);
            if (result != ButtonResult.Ok)
                return;

            // Remove from view model
            var configVm = DataContext as McpServerConfigViewModel;
            configVm.McpServers.Remove(vm);

            // Update to server
            McpServerConfig config = configVm.ToModel();
            bool success = await _mcpService.SetConfig(config);
            if (!success)
            {
                box = MessageBoxManager.GetMessageBoxStandard("Error", "Failed to delete!",
                    ButtonEnum.Ok,
                    Icon.Error);
                await box.ShowAsync();
            }
        }
    }

    private async void BtnEdit_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is McpViewModel mcpViewModel)
        {
            AddServerWindow editWindow = new AddServerWindow(mcpViewModel);
            Window parent = TopLevel.GetTopLevel(this) as Window;
            await editWindow.ShowDialog(parent);
            McpServer editServer = editWindow.Result;
            if (editServer != null && DataContext is McpServerConfigViewModel vm)
            {
                // Modify current viewmodel
                if (editServer.type == McpServerType.Stdio)
                {
                    // Command
                    mcpViewModel.Command = editServer.command;
                    // Args
                    mcpViewModel.Args = new ObservableCollection<string>(editServer.args);
                    // Env
                    ObservableCollection<KeyValuePair<string, string>> collection = new ObservableCollection<KeyValuePair<string, string>>();
                    foreach (var pair in editServer.env)
                    {
                        if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                        {
                            collection.Add(new KeyValuePair<string, string>(pair.Key, pair.Value));
                        }
                    }
                    mcpViewModel.Env = collection;
                    // Http headers
                    ObservableCollection<KeyValuePair<string, string>> headerCollection = new ObservableCollection<KeyValuePair<string, string>>();
                    foreach (var pair in editServer.http_headers)
                    {
                        if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                        {
                            headerCollection.Add(new KeyValuePair<string, string>(pair.Key, pair.Value));
                        }
                    }
                    mcpViewModel.HttpHeaders = headerCollection;
                }
                else if (editServer.type == McpServerType.SSE)
                {
                    mcpViewModel.SseUrl = editServer.sse_url;
                }
                else if (editServer.type == McpServerType.StreamableHttp)
                {
                    mcpViewModel.StreamableHttpUrl = editServer.streamable_http_url;
                }

                // Apply the change to server
                McpServerConfig config = vm.ToModel();
                bool success = await _mcpService.SetConfig(config);
                if (success)
                {

                }
                else
                {
                    var box = MessageBoxManager.GetMessageBoxStandard("Error", "Cannot add new MCP config to server.",
                        ButtonEnum.Ok,
                        Icon.Error);
                    Window owner = TopLevel.GetTopLevel(this) as Window;
                    await box.ShowWindowDialogAsync(owner);
                }
            }
        }
    }

    private async void Checkbox_OnClick(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;

        if (sender is CheckBox btn && btn.DataContext is McpViewModel mcpViewModel)
        {
            mcpViewModel.IsBusy = true;
            // Set enable status to server
            mcpViewModel.Enabled = btn.IsChecked ?? false;
            var configVm = DataContext as McpServerConfigViewModel;
            McpServerConfig config = configVm.ToModel();
            bool success = await _mcpService.SetConfig(config);
            if (!success)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Cannot set status to server!",
                    ButtonEnum.Ok,
                    Icon.Error);
                Window owner = TopLevel.GetTopLevel(this) as Window;
                await box.ShowWindowDialogAsync(owner);
            }
            else
            {
                await LoadConfig();
            }
            mcpViewModel.IsBusy = false;
        }
    }
}