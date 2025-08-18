using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using McpClient.Models;
using McpClient.Services;

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
        var (success, result) = await _service.ExecuteStaticWorkflow("0", null, _group.Id, TbQuery.Text);
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

        BtnRun.IsEnabled = true;
        TbQuery.IsEnabled = true;
    }

    private void Window_OnClosing(object sender, WindowClosingEventArgs e)
    {
        if (isRunning)
        {
            e.Cancel = true;
        }
    }
}