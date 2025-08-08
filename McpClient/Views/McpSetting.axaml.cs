using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaProgressRing;
using McpClient.Models;
using McpClient.Services;
using McpClient.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace McpClient.Views;

public partial class McpSetting : UserControl
{
    private readonly McpConfigService _service;

    public McpSetting()
    {
        InitializeComponent();
        _service = new McpConfigService(new HttpClient());
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;
    }

    public async Task LoadConfig()
    {
        // Show progress
        ProgressRing.IsVisible = true;
        ScrollViewer.IsVisible = false;
        BtnAdd.IsVisible = false;
        // Scroll back to top
        ScrollViewer.ScrollToHome();
        // Load all configs
        McpServerConfig config = await _service.GetConfig();
        // Load server status
        McpServerListResponse listResponse = await _service.ListCurrent();
        if (listResponse != null && config != null)
        {
            // Merge each server's status into main view model;
            McpServerConfigViewModel viewModel = new McpServerConfigViewModel(config, listResponse.data);
            DataContext = viewModel;

            ProgressRing.IsVisible = false;
            ScrollViewer.IsVisible = true;
            BtnAdd.IsVisible = true;
        }
        else
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Cannot get MCP list server.",
                ButtonEnum.Ok,
                Icon.Error);
            var result = await box.ShowAsync();
        }
    }

    public async Task<bool> SaveConfig()
    {
        if (DataContext is McpServerConfigViewModel vm)
        {
            ProgressRing.IsVisible = true;
            ScrollViewer.IsVisible = false;
            BtnAdd.IsVisible = false;
            McpServerConfig config = vm.ToModel();
            // Send back to server
            bool success = await _service.SetConfig(config);
            ProgressRing.IsVisible = false;
            ScrollViewer.IsVisible = false;
            BtnAdd.IsVisible = false;
            if (!success)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Cannot update MCP config to server.",
                    ButtonEnum.Ok,
                    Icon.Error);
                var result = await box.ShowAsync();
                return false;
            }
        }
        return true;
    }

    private async void BtnAdd_OnClick(object sender, RoutedEventArgs e)
    {
        Window parent = TopLevel.GetTopLevel(this) as Window;
        AddServerWindow addWindow = new AddServerWindow();
        await addWindow.ShowDialog(parent);
    }

    private async void BtnEditArgs_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is McpViewModel vm)
        {
            Window parent = TopLevel.GetTopLevel(this) as Window;
            ArgsEditorViewModel argsEditorViewModel = new ArgsEditorViewModel();
            // Copy the data
            foreach (string arg in vm.Args)
            {
                argsEditorViewModel.Args.Add(new ArgsEditorItem(arg));
            }
            // Show edit window
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
            Window parent = TopLevel.GetTopLevel(this) as Window;
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
            EnvEditor editWindow = new EnvEditor();
            editWindow.DataContext = envEditorViewModel;
            await editWindow.ShowDialog(parent);
            // Replace whole collection, so UI could change
            vm.Env = envEditorViewModel.ToCollenction();
        }
    }
}