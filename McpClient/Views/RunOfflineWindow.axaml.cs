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

namespace McpClient.Views;

public partial class RunOfflineWindow : Window
{
    private readonly OfflineWorkflow _workflow;
    private readonly AiNexusService _service;
    private readonly JsonNode jsonNode;
    private bool isRunning;
    private const int STREAM_CHUNK_SIZE = 10;

    public RunOfflineWindow()
    {
        InitializeComponent();
    }

    internal RunOfflineWindow(OfflineWorkflow workflow, AiNexusService aiNexusService)
    {
        InitializeComponent();
        if (Design.IsDesignMode)
            return;

        _workflow = workflow;
        _service = aiNexusService;
        LbHeader.Content += " " + _workflow.Name;
        //TbDescription.Text = _group.Description;
        TbOutput.Text = string.Empty;
        CbModelName.ItemsSource = Constants.LOCAL_MODELS;
        CbModelName.SelectedIndex = 0;
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
                }
                else
                {
                    TbPayload.Text = _workflow.Payload;
                }
            }
            catch (Exception ex)
            {
                TbPayload.Text = _workflow.Payload;
            }
        }
        else
        {
            TbPayload.IsVisible = false;
        }
    }

    private async void BtnRun_OnClick(object sender, RoutedEventArgs e)
    {
        BtnRun.IsEnabled = false;
        CbModelName.IsEnabled = false;
        TbPayload.IsEnabled = false;
        TbOutput.Text = string.Empty;

        isRunning = true;
        TbOutput.Text += "Running...\n";

        string modelName = Constants.LOCAL_MODELS[CbModelName.SelectedIndex];
        string payload;
        if (jsonNode != null)
        {
            jsonNode["message"] = TbPayload.Text?.Trim();
            payload = jsonNode.ToJsonString();
        }
        else
        {
            payload = TbPayload.Text;
        }

        await Task.Run(async () =>
        {
            await ExecuteWorkflow(_workflow.Id, modelName, payload);
        });
    }

    private async Task ExecuteWorkflow(int id, string modelName, string payload)
    {
        try
        {
            IAsyncEnumerable<AutogenStreamResponse> responses = _service.ExecuteOfflineWorkflow(id, modelName, payload);
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