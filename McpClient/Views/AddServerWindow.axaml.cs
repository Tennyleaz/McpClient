using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using McpClient.Models;
using McpClient.ViewModels;
using ModelContextProtocol.Client;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace McpClient.Views;

public partial class AddServerWindow : Window
{
    private readonly bool _isEdit = false;
    private List<string> args = new();
    private Dictionary<string, string> env = new();
    private Dictionary<string, string> headers = new();
    private readonly McpViewModel _mcpViewModel;

    internal McpServer Result { get; private set; }

    public AddServerWindow()
    {
        InitializeComponent();
        _isEdit = false;
        BtnReset.IsVisible = false;
    }

    internal AddServerWindow(McpViewModel mcpViewModel)
    {
        InitializeComponent();
        _isEdit = true;
        BtnReset.IsVisible = mcpViewModel.IsCloud;
        _mcpViewModel = mcpViewModel;
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isEdit)
        {
            TbHeader.Text = Title = "Edit Agent";
            TbServerName.IsVisible = false;
            EditAgentName.Text = _mcpViewModel.ServerName;
            EditAgentName.IsVisible = true;

            if (!string.IsNullOrEmpty(_mcpViewModel.Owner))
            {
                TbOwner.IsEnabled = false;
                TbOwner.Text = _mcpViewModel.Owner;
            }

            CbTypes.IsEnabled = false;
            if (_mcpViewModel.Type == McpServerType.Stdio)
            {
                CbTypes.SelectedIndex = 0;
                TbCommand.Text = _mcpViewModel.Command;
            }
            else if (_mcpViewModel.Type == McpServerType.SSE)
            {
                CbTypes.SelectedIndex = 1;
                TbUrl.Text = _mcpViewModel.SseUrl;
            }
            else if (_mcpViewModel.Type == McpServerType.StreamableHttp)
            {
                CbTypes.SelectedIndex = 2;
                TbUrl.Text = _mcpViewModel.StreamableHttpUrl;
            }

            env = _mcpViewModel.Env.ToDictionary();
            TbEnv.Text = string.Join(", ", env);

            headers = _mcpViewModel.HttpHeaders.ToDictionary();
            TbEnv.Text = string.Join(", ", headers);

            args = _mcpViewModel.Args.ToList();
            TbArgs.Text = string.Join(", ", args);
        }
        else
        {
            CbTypes.SelectedIndex = -1;
            CbTypes_OnSelectionChanged(null, null);
        }
    }

    private async void ButtonApply_OnClick(object sender, RoutedEventArgs e)
    {
        bool isValid = await Validate();
        if (!isValid)
        {
            return;
        }

        McpServer newServer = new McpServer();
        newServer.enabled = true;
        newServer.server_name = TbServerName.Text;
        newServer.command = TbCommand.Text;
        newServer.owner = TbOwner.Text;
        newServer.args = args;
        newServer.env = env;
        newServer.http_headers = headers;
        if (CbTypes.SelectedIndex == 1)
        {
            newServer.type = McpServerType.SSE;
            newServer.sse_url = TbUrl.Text;
        }
        else if (CbTypes.SelectedIndex == 2)
        {
            newServer.type = McpServerType.StreamableHttp;
            newServer.streamable_http_url = TbUrl.Text;
        }
        else
        {
            newServer.type = McpServerType.Stdio;
        }

        try
        {
            bool mcpReuslt = await ValidateMcp(newServer);
            if (!mcpReuslt)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("MCP", "Failed to find tools in MCP server.",
                    ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
                await box.ShowWindowDialogAsync(this);
            }
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("MCP", "Failed to find tools in MCP server:\n" + ex.Message,
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        Result = newServer;
        Close();
    }

    private async void BtnEditArgs_OnClick(object sender, RoutedEventArgs e)
    {
        ArgsEditorViewModel argsEditorViewModel = new ArgsEditorViewModel();
        // Copy the data
        foreach (string arg in args)
        {
            argsEditorViewModel.Args.Add(new ArgsEditorItem(arg));
        }
        // Show edit window
        ArgsEditor editWin = new ArgsEditor();
        editWin.DataContext = argsEditorViewModel;
        await editWin.ShowDialog(this);
        // Replace whole collection, so UI could change
        args = argsEditorViewModel.ToCollenction().ToList();
        TbArgs.Text = string.Join(' ', args);
    }

    private async void BtnEditEnv_OnClick(object sender, RoutedEventArgs e)
    {
        EnvEditorViewModel envEditorViewModel = new EnvEditorViewModel();
        // Copy the data
        foreach (var pair in env)
        {
            envEditorViewModel.Env.Add(new EnvironmentItem()
            {
                Name = pair.Key,
                Value = pair.Value
            });
        }
        // Show edit window
        EnvEditor editWindow = new EnvEditor(false);
        editWindow.DataContext = envEditorViewModel;
        await editWindow.ShowDialog(this);
        // Replace whole collection, so UI could change
        env = envEditorViewModel.ToDictionary();
        TbEnv.Text = string.Join(", ", env);
    }

    private async void BtnEditHeader_OnClick(object sender, RoutedEventArgs e)
    {
        EnvEditorViewModel envEditorViewModel = new EnvEditorViewModel();
        // Copy the data
        foreach (var pair in headers)
        {
            envEditorViewModel.Env.Add(new EnvironmentItem
            {
                Name = pair.Key,
                Value = pair.Value
            });
        }
        // Show edit window
        EnvEditor editWindow = new EnvEditor(true);
        editWindow.DataContext = envEditorViewModel;
        await editWindow.ShowDialog(this);
        // Replace whole collection, so UI could change
        headers = envEditorViewModel.ToDictionary();
        TbHttpHeader.Text = string.Join(", ", headers);
    }

    private void CbTypes_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;

        if (CbTypes.SelectedIndex <= 0)
        {
            // stdio, has command, no URL
            TbUrlHeader.IsVisible = TbUrl.IsVisible = false;
            MainGrid.RowDefinitions[4].Height = GridLength.Parse("0");

            TbCommandHeader.IsVisible = TbCommand.IsVisible = true;
            MainGrid.RowDefinitions[3].Height = GridLength.Parse("40");

            // Show env/args
            TbArgsHeader.IsVisible = TbArgs.IsVisible = BtnEditArgs.IsVisible = true;
            TbEnvHeader.IsVisible = TbEnv.IsVisible = BtnEditEnv.IsVisible = true;
            MainGrid.RowDefinitions[5].Height = GridLength.Parse("40");
            MainGrid.RowDefinitions[6].Height = GridLength.Parse("40");

            // Hide http headers
            TbHttpHeader.IsVisible = TbHttpHeaderTitle.IsVisible = BtnEditHeader.IsVisible = false;
            MainGrid.RowDefinitions[7].Height = GridLength.Parse("0");
        }
        else
        {
            // sse, streamableHttp has URL, no command
            TbUrlHeader.IsVisible = TbUrl.IsVisible = true;
            MainGrid.RowDefinitions[4].Height = GridLength.Parse("40");

            TbCommandHeader.IsVisible = TbCommand.IsVisible = false;
            MainGrid.RowDefinitions[3].Height = GridLength.Parse("0");

            // Hide env/args
            TbArgsHeader.IsVisible = TbArgs.IsVisible = BtnEditArgs.IsVisible = false;
            TbEnvHeader.IsVisible = TbEnv.IsVisible = BtnEditEnv.IsVisible = false;
            MainGrid.RowDefinitions[5].Height = GridLength.Parse("0");
            MainGrid.RowDefinitions[6].Height = GridLength.Parse("0");

            // Show http headers
            TbHttpHeader.IsVisible = TbHttpHeaderTitle.IsVisible = BtnEditHeader.IsVisible = true;
            MainGrid.RowDefinitions[7].Height = GridLength.Parse("40");
        }
    }

    private async Task<bool> Validate()
    {
        if (string.IsNullOrWhiteSpace(TbServerName.Text))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Info", "Please input your server name.",
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowWindowDialogAsync(this);
            return false;
        }

        if (CbTypes.SelectedIndex == 0 && string.IsNullOrWhiteSpace(TbCommand.Text))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Info", "Please input your command.",
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowWindowDialogAsync(this);
            return false;
        }

        if (CbTypes.SelectedIndex < 0)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Info", "Please select a server type.",
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowWindowDialogAsync(this);
            return false;
        }

        if (CbTypes.SelectedIndex > 0 && string.IsNullOrWhiteSpace(TbUrl.Text))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Info", "Please input a valid URL.",
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowWindowDialogAsync(this);
            return false;
        }

        return true;
    }

    private async Task<bool> ValidateMcp(McpServer server)
    {
        IClientTransport clientTransport;
        if (server.type == McpServerType.Stdio)
        {
            clientTransport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Command = server.command,
                Arguments = server.args,
                EnvironmentVariables = server.env,
                Name = server.server_name
            });
        }
        else if (server.type == McpServerType.StreamableHttp)
        {
            clientTransport = new SseClientTransport(new SseClientTransportOptions
            {
                Endpoint = new Uri(server.streamable_http_url),
                TransportMode = HttpTransportMode.StreamableHttp,
                Name = server.server_name,
                AdditionalHeaders = server.http_headers
            });
        }
        else
        {
            clientTransport = new SseClientTransport(new SseClientTransportOptions
            {
                Endpoint = new Uri(server.sse_url),
                TransportMode = HttpTransportMode.Sse,
                Name = server.server_name,
                AdditionalHeaders = server.http_headers
            });
        }

        IMcpClient mcpClient = await McpClientFactory.CreateAsync(clientTransport);
        IList<McpClientTool> tools = await mcpClient.ListToolsAsync();
        if (tools.Count == 0)
        {
            return false;
        }

        return true;
    }

    private void BtnReset_OnClick(object sender, RoutedEventArgs e)
    {

    }
}