using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using McpClient.Models;
using McpClient.Services;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using WebViewControl;

namespace McpClient.Views;

public partial class RunStaticTaskWindow : Window
{
    private readonly Group _group;
    private readonly AiNexusService _service;
    private bool isRunning;

    public RunStaticTaskWindow()
    {
        InitializeComponent();
    }

    internal RunStaticTaskWindow(Group group, AiNexusService service)
    {
        InitializeComponent();
        _group = group;
        _service = service;
        LbHeader.Content += ": " + _group.Name;
        TbDescription.Text = _group.Description;
        TbOutput.Text = string.Empty;
    }

    private async void BtnRun_OnClick(object sender, RoutedEventArgs e)
    {
        BtnRun.IsEnabled = false;
        TbQuery.IsEnabled = false;
        TbOutput.Text = string.Empty;

        isRunning = true;
        TbOutput.Text += "Running...\n";

        AutoGenRequest request = new AutoGenRequest
        {
            connectionId = "0",
            agents = null,
            group = _group.Id,
            query = TbQuery.Text
        };

        await Task.Run(async () =>
        {
            await ExecuteWorkflow(request);
        });
    }

    private async Task ExecuteWorkflow(AutoGenRequest request)
    {
        IAsyncEnumerable<AutogenResponse> responses = _service.ExecuteStaticWorkflow(request);
        await foreach (AutogenResponse response in responses)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                TbOutput.Text += response + "\n";
                if (response.IsTerminated)
                {
                    TbOutput.Text += "Run task done.";

                    isRunning = false;
                    BtnRun.IsEnabled = true;
                    TbQuery.IsEnabled = true;
                }
            });
        }
    }

    private void Window_OnClosing(object sender, WindowClosingEventArgs e)
    {
        if (isRunning)
        {
            e.Cancel = true;
        }
    }
}