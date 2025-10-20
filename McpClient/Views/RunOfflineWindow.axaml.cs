using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using McpClient.Models;
using McpClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using McpClient.ViewModels;

namespace McpClient.Views;

public partial class RunOfflineWindow : Window
{
    private readonly OfflineWorkflow _workflow;
    private readonly AiNexusService _nexusService;
    private readonly McpConfigService _mcpService;
    private readonly JsonNode jsonNode;
    private bool isRunning;
    private const int STREAM_CHUNK_SIZE = 10;
    private List<ModelItem> availableModels;

    public RunOfflineWindow()
    {
        InitializeComponent();
    }

    internal RunOfflineWindow(OfflineWorkflow workflow, AiNexusService aiNexusService, McpConfigService mcpConfigService)
    {
        InitializeComponent();
        if (Design.IsDesignMode)
            return;

        _workflow = workflow;
        _nexusService = aiNexusService;
        _mcpService = mcpConfigService;
        LbHeader.Content += " " + _workflow.Name;
        //TbDescription.Text = _group.Description;
        TbOutput.Text = string.Empty;

        // Set custom message
        TbPayload.IsVisible = false;
        if (!string.IsNullOrWhiteSpace(_workflow.Payload))
        {
            TbPayload.IsVisible = true;
            // Parse "message" JSON field
            try
            {
                jsonNode = JsonNode.Parse(_workflow.Payload);
                if (jsonNode != null && jsonNode["message"] != null)
                {
                    TbPayload.Text = jsonNode["message"].ToString().Trim();
                    TbPayload.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                //TbPayload.Text = _workflow.Payload;
            }
        }
    }

    private async void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;

        // Load online model list
        ModelData data = await _mcpService.ListModels();
        if (data == null)
        {
            availableModels = Constants.LOCAL_MODELS;
        }
        else
        {
            availableModels = data.Data;
        }

        ModelViewModel qwen = null;
        List<ModelViewModel> viewModels = new List<ModelViewModel>();
        foreach (ModelItem modelItem in availableModels)
        {
            var vm = new ModelViewModel(modelItem);
            viewModels.Add(vm);
            if (vm.Name == "Qwen3-14B")
                qwen = vm;
        }
        CbModelName.ItemsSource = viewModels;
        if (qwen != null)
            CbModelName.SelectedItem = qwen;
        else
            CbModelName.SelectedIndex = availableModels.Count - 1;
    }

    private async void BtnRun_OnClick(object sender, RoutedEventArgs e)
    {
        BtnRun.IsEnabled = false;
        CbModelName.IsEnabled = false;
        TbPayload.IsEnabled = false;
        TbOutput.Text = string.Empty;

        isRunning = true;
        TbOutput.Text += "Running...\n";

        string modelName = availableModels[CbModelName.SelectedIndex].ModelName;
        string message = TbPayload.Text?.Trim();

        await Task.Run(async () =>
        {
            await ExecuteWorkflow(_workflow.Id, modelName, message);
        });
    }

    private async Task ExecuteWorkflow(int id, string modelName, string message)
    {
        try
        {
            IAsyncEnumerable<AutogenStreamResponse> responses = _nexusService.ExecuteOfflineWorkflow(id, modelName, message);
            string text = string.Empty;
            await foreach (AutogenStreamResponse response in responses)
            {
                AutogenChoice choice = response.Choices?.FirstOrDefault();
                if (choice == null)
                {
                    continue;
                }

                // Gather streamed text
                text += choice.Delta.Content;

                // Update to UI after text is long enough
                if (text.Length > STREAM_CHUNK_SIZE)
                {
                    // Clear the partial text
                    string tempText = text;
                    text = string.Empty;
                    // Update to UI
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        TbOutput.Text += tempText;
                        OutputScroller.ScrollToEnd();
                    });
                }
            }

            // After workflow is done, enable UI
            Dispatcher.UIThread.Invoke(() =>
            {
                // Remember to update last bit of text
                TbOutput.Text += text + "\n\nRun task done.";
                OutputScroller.ScrollToEnd();

                isRunning = false;
                CbModelName.IsEnabled = true;
                BtnRun.IsEnabled = true;
                TbPayload.IsEnabled = true;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Dispatcher.UIThread.Invoke(() =>
            {
                TbOutput.Text += "Error: " + ex;

                isRunning = false;
                CbModelName.IsEnabled = true;
                BtnRun.IsEnabled = true;
                TbPayload.IsEnabled = true;
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