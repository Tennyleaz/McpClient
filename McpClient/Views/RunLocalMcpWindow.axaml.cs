using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using McpClient.Services;

namespace McpClient.Views;

public partial class RunLocalMcpWindow : Window
{
    private readonly string _serverName;
    private readonly AiNexusService _service;

    public RunLocalMcpWindow()
    {
        InitializeComponent();
    }

    internal RunLocalMcpWindow(string serverName, AiNexusService aiNexusService)
    {
        InitializeComponent();

        _serverName = serverName;
        _service = aiNexusService;
        LbHeader.Content += " " + serverName;
        //TbDescription.Text = _group.Description;
        TbOutput.Text = string.Empty;
        CbModelName.ItemsSource = Constants.LOCAL_MODELS;
        CbModelName.SelectedIndex = 0;
    }

    private async void BtnRun_OnClick(object sender, RoutedEventArgs e)
    {
        BtnRun.IsEnabled = false;
        //TbQuery.IsEnabled = false;
        CbModelName.IsEnabled = false;
        TbOutput.Text = string.Empty;

        TbOutput.Text += "Setting offline group...\n";
        string modelName = Constants.LOCAL_MODELS[CbModelName.SelectedIndex];
        var (success, groupId) = await _service.SetOfflineGroup(_serverName, modelName, "", "");
        string result = string.Empty;
        if (success)
        {
            TbOutput.Text += "Running...\n";
            (success, result) = await _service.ExecuteOfflineWorkflow(groupId);
        }
        if (success)
        {
            TbOutput.Text += "Run success.";
        }
        else
        {
            TbOutput.Text += "Run fail.\n";
            TbOutput.Text += result;
        }

        BtnRun.IsEnabled = true;
        //TbQuery.IsEnabled = true;
        CbModelName.IsEnabled = true;
    }
}