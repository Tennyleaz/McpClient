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
using McpClient.Utils;
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

        server.Description = server.Description.Trim();
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
            MarkdownViewer.Markdown = detailBase.Overview?.Trim() ?? "*No description provided.*";

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
                    List<string> requirements = ParseSystemRequirements(parsedMcpServer);
                    TbSystemReq.Text = string.Join(", ", requirements);

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

        // check local command
        if (parsedMcpServer.type == McpServerType.Stdio)
        {
            if (!LocalServiceUtils.FindCommand(parsedMcpServer.command))
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Info", $"You need command \"{parsedMcpServer.command}\" before installing this agent.",
                    ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
                await box.ShowWindowDialogAsync(this);

                BtnInstall.IsEnabled = true;
                BtnInstall.Content = "Install ¡õ";
                return;
            }
        }

        Settings settings = SettingsManager.Local.Load();
        McpConfigService configService = new McpConfigService(settings.McpConfigToken);
        McpServerConfig serverConfig = await configService.GetConfig();
        if (serverConfig == null)
        {
            BtnInstall.IsEnabled = true;
            BtnInstall.Content = "Install ¡õ";
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

        string name, url, type, command;
        JsonNode firstChild;
        try
        {
            name = node.AsObject().First().Key;
            firstChild = node.Root[name];
            if (firstChild == null)
                return null;

            url = firstChild["url"]?.ToString();
            type = firstChild["type"]?.ToString();
            command = firstChild["command"]?.ToString();
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
                mcpServer.type = McpServerType.StreamableHttp;
                break;
            case "sse":
                mcpServer.type = McpServerType.SSE;
                break;
            default:
                if (string.IsNullOrEmpty(command))
                    mcpServer.type = McpServerType.SSE;
                else
                    mcpServer.type = McpServerType.Stdio;
                break;
        }

        if (mcpServer.type == McpServerType.Stdio)
        {
            try
            {
                mcpServer.command = command;
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
        else if (mcpServer.type == McpServerType.StreamableHttp)
        {
            mcpServer.streamable_http_url = url;
            if (firstChild["headers"] != null)
            {
                string json = firstChild["headers"].ToJsonString();
                mcpServer.http_headers = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
        }
        else if (mcpServer.type == McpServerType.SSE)
        {
            mcpServer.sse_url = url;
            if (firstChild["headers"] != null)
            {
                string json = firstChild["headers"].ToJsonString();
                mcpServer.http_headers = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
        }

        return mcpServer;
    }

    private static List<string> ParseSystemRequirements(McpServer mcpServer)
    {
        List<string> requiredApps = new List<string>();

        if (mcpServer.type == McpServerType.Stdio)
        {
            switch (mcpServer.command)
            {
                case "npm":
                case "npx":
                    requiredApps.Add("NodeJS");
                    requiredApps.Add(mcpServer.command);
                    break;
                case "node":
                    requiredApps.Add("NodeJS");
                    break;
                case "uv":
                case "uvx":
                    requiredApps.Add("Python 3");
                    requiredApps.Add(mcpServer.command);
                    break;
                case "python":
                case "python3":
                    requiredApps.Add("Python 3");
                    break;
                case "docker":
                    requiredApps.Add("Docker");
                    break;
                default:
                    requiredApps.Add(mcpServer.command);
                    break;
            }
        }

        return requiredApps;
    }

    private void BtnGithub_OnClick(object sender, RoutedEventArgs e)
    {
        Launcher.LaunchUriAsync(new Uri(detail.GithubUrl));
    }

    private void BtnMcpSo_OnClick(object sender, RoutedEventArgs e)
    {
        Launcher.LaunchUriAsync(new Uri(detail.Url));
    }
}