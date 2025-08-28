using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using McpClient.Models;
using McpClient.Services;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace McpClient.Views;

public partial class StoreItemWindow : Window
{
    private readonly StoreMcpServer _mcpServer;
    private readonly AiNexusService _service;
    private readonly List<string> _installedNames;
    private StoreMcpServerDetail detail;
    private McpServer parsedMcpServer;

    public bool IsInstalled { get; private set; }

    public StoreItemWindow()
    {
        InitializeComponent();
    }

    internal StoreItemWindow(StoreMcpServer server, AiNexusService service, List<string> installedNames)
    {
        InitializeComponent();

        _mcpServer = server;
        _service = service;
        _installedNames = installedNames;
        DataContext = _mcpServer;
    }

    private async void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;

        StoreMcpServerDetailBase detailBase = await _service.GetStoreMcpServerDetail(_mcpServer.Url);
        if (detailBase != null)
        {
            // Set url
            BtnGithub.IsVisible = !string.IsNullOrEmpty(detailBase.GithubUrl);
            BtnMcpSo.IsVisible = !string.IsNullOrEmpty(detailBase.Url);

            // Check if detail is real or not
            if (detailBase is StoreMcpServerDetail detailReal && detailReal.ServerConfig?.McpServers != null)
            {
                detail = detailReal;
                TbMcpSetting.Text = detailReal.ServerConfig.McpServers.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                parsedMcpServer = SeverTypeToLocalType(detailReal.ServerConfig.McpServers);
                if (parsedMcpServer != null)
                {
                    TbMcpType.Text = "Type: " + parsedMcpServer.type;

                    // Check if already installed
                    if (_mcpServer.IsInstalled || _installedNames.Contains(parsedMcpServer.server_name))
                    {
                        BtnInstall.Content = "Installed";
                        BtnInstall.IsEnabled = false;
                    }
                    else
                    {
                        BtnInstall.IsEnabled = true;
                    }

                    return;
                }
            }
            LabelInvalidJson.IsVisible = true;
            TbMcpSetting.Text = detailBase.ServerConfig;
            BtnInstall.IsEnabled = false;
        }
        else
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Failed to get item details from server.",
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await box.ShowWindowDialogAsync(this);
            BtnInstall.IsEnabled = false;
        }
    }

    private async void BtnInstall_OnClick(object sender, RoutedEventArgs e)
    {
        BtnInstall.IsEnabled = false;
        BtnInstall.Content = "Installing...";

        Settings settings = SettingsManager.Local.Load();
        McpConfigService configService = new McpConfigService(settings.McpConfigToken);
        McpServerConfig serverConfig = await configService.GetConfig();
        if (serverConfig == null)
        {
            return;
        }

        serverConfig.mcp_servers.Add(parsedMcpServer);

        bool success = await configService.SetConfig(serverConfig);
        if (success)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Info", $"\"{_mcpServer.Name}\" successfully installed.",
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowWindowDialogAsync(this);

            BtnInstall.Content = "Installed";
            BtnInstall.IsEnabled = false;
            IsInstalled = true;
        }
        else
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Failed to install MCP tool.",
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await box.ShowWindowDialogAsync(this);

            BtnInstall.Content = "Install ¡õ";
            BtnInstall.IsEnabled = true;
        }
    }

    private McpServer SeverTypeToLocalType(JsonNode node)
    {
        if (node == null)
            return null;

        string name, url, type;
        try
        {
            name = node.AsObject().First().Key;
            url = node.Root["url"]?.ToJsonString();
            type = node.Root["type"]?.ToJsonString();
        }
        catch (JsonException ex)
        {
            Console.WriteLine(ex);
            return null;
        }

        McpServer mcpServer = new McpServer
        {
            enabled = true,
            server_name = name,
            owner = _mcpServer.Author,
            source = "cloud"
        };

        switch (type)
        {
            case "streamable-http":
                mcpServer.type = "streamableHttp";
                break;
            case "sse":
                mcpServer.type = "sse";
                break;
            default:
                mcpServer.type = "stdio";
                break;
        }

        if (mcpServer.type == "stdio")
        {
            try
            {
                JsonNode firstChild = node.Root[name];
                mcpServer.command = firstChild["command"]?.ToString();
                if (firstChild["env"] != null)
                {
                    string json = firstChild["env"].ToJsonString();
                    mcpServer.env = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                }

                if (firstChild["args"] != null)
                {
                    string json = firstChild["args"].ToJsonString();
                    mcpServer.args = JsonSerializer.Deserialize<List<string>>(json);
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine(ex);
            }
        }
        else if (mcpServer.type == "streamableHttp")
        {
            mcpServer.streamable_http_url = url;
        }
        else if (mcpServer.type == "sse")
        {
            mcpServer.sse_url = url;
        }

        return mcpServer;
    }

    private void BtnGithub_OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        Launcher.LaunchUriAsync(new Uri(detail.GithubUrl));
    }

    private void BtnMcpSo_OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        Launcher.LaunchUriAsync(new Uri(detail.Url));
    }
}