using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using McpClient.Models;
using McpClient.Services;
using System.Collections.Generic;

namespace McpClient.Views;

public partial class RunOfflineWindow : Window
{
    private readonly OfflineWorkflow _workflow;
    private readonly AiNexusService _service;
    private bool isRunning;

    public RunOfflineWindow()
    {
        InitializeComponent();
    }

    internal RunOfflineWindow(OfflineWorkflow workflow, AiNexusService aiNexusService)
    {
        InitializeComponent();

        _workflow = workflow;
        _service = aiNexusService;
        LbHeader.Content += " " + _workflow.Name;
        //TbDescription.Text = _group.Description;
        TbOutput.Text = string.Empty;
        CbModelName.ItemsSource = Constants.LOCAL_MODELS;
        CbModelName.SelectedIndex = 0;
    }

    private async void BtnRun_OnClick(object sender, RoutedEventArgs e)
    {
        BtnRun.IsEnabled = false;
        CbModelName.IsEnabled = false;
        TbOutput.Text = string.Empty;

        isRunning = true;
        TbOutput.Text += "Running...\n";
        string modelName = Constants.LOCAL_MODELS[CbModelName.SelectedIndex];
        var (success, result) = await _service.ExecuteOfflineWorkflow(_workflow.Id, modelName);
        if (success)
        {
            TbOutput.Text += "Run success.";
        }
        else
        {
            TbOutput.Text += "Run fail.\n";
            TbOutput.Text += result;
        }
        isRunning = false;

        CbModelName.IsEnabled = true;
        BtnRun.IsEnabled = true;
    }

    private void Window_OnClosing(object sender, WindowClosingEventArgs e)
    {
        if (isRunning)
        {
            e.Cancel = true;
        }
    }
}