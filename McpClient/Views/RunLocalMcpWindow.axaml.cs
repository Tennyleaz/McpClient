using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using McpClient.Models;
using McpClient.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            await Task.Run(async () =>
            {
                await ExecuteWorkflow(groupId, modelName);
            });
        }
        else
        {
            TbOutput.Text += "Run fail.\n";
            TbOutput.Text += result;

            BtnRun.IsEnabled = true;
            //TbQuery.IsEnabled = true;
            CbModelName.IsEnabled = true;
        }
    }

    private async Task ExecuteWorkflow(int id, string modelName)
    {
        //IAsyncEnumerable<AutogenResponse> responses = _service.ExecuteOfflineWorkflow(id, modelName, null);
        //await foreach (AutogenResponse response in responses)
        //{
        //    Dispatcher.UIThread.Invoke(() =>
        //    {
        //        TbOutput.Text += response + "\n";
        //        if (response.IsTerminated)
        //        {
        //            TbOutput.Text += "Run task done.";

        //            //isRunning = false;
        //            CbModelName.IsEnabled = true;
        //            BtnRun.IsEnabled = true;
        //        }
        //    });
        //}
    }
}