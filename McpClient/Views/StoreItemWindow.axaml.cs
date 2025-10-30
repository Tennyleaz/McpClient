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
    private StoreMcpServerDetailBase detailBase;
    private McpServer parsedMcpServer;
    private bool isBusy;

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

    private async void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;

        detailBase = await _service.GetStoreMcpServerDetail(_mcpServer.Url);
        if (detailBase != null)
        {
            // Set url
            BtnGithub.IsVisible = !string.IsNullOrEmpty(detailBase.GithubUrl);
            BtnMcpSo.IsVisible = !string.IsNullOrEmpty(detailBase.Url);
            MarkdownViewer.Markdown = detailBase.Overview?.Trim() ?? "*No description provided.*";

            // Check if detail is real or not
            if (detailBase is StoreMcpServerDetail detailReal && detailReal.ServerConfig?.McpServers != null)
            {
                TbMcpSetting.Text = detailReal.ServerConfig.McpServers.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                parsedMcpServer = detailReal.SeverTypeToLocalType();
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
                        // Special case: Check default MCP servers
                        if (_mcpServer.Url == "https://mcp.so/server/filesystem")
                        {
                            BtnInstall.Content = "Installed";
                            BtnInstall.IsEnabled = false;
                        }
                    }

                    return;
                }
            }
            LabelInvalidJson.IsVisible = true;
            TbMcpSetting.Text = detailBase.ServerConfig;
            BtnInstall.Content = "Not Competible";
            BtnInstall.IsEnabled = false;
            // Special case: Check default MCP servers
            if (_mcpServer.Url == "https://mcp.so/server/filesystem")
            {
                BtnInstall.Content = "Installed";
                BtnInstall.IsEnabled = false;
            }
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
        isBusy = true;

        // check local command
        if (parsedMcpServer.type == McpServerType.Stdio)
        {
            /*if (!LocalServiceUtils.FindCommand(parsedMcpServer.command))
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Info", $"You need command \"{parsedMcpServer.command}\" before installing this agent.",
                    ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
                await box.ShowWindowDialogAsync(this);

                BtnInstall.IsEnabled = true;
                BtnInstall.Content = "Install ¡õ";
                return;
            }*/

            LocalCommandWizard wizard = new LocalCommandWizard();
            if (wizard.GenerateMcpStoreRuntimeViewModel(parsedMcpServer.command))
            {
                await wizard.ShowDialog(this);
                if (!wizard.IsAllRuntimeInstalled)
                {
                    // TODO: warn user again?
                    BtnInstall.IsEnabled = true;
                    BtnInstall.Content = "Install ¡õ";
                    isBusy = false;
                    return;
                }
            }
        }

        Settings settings = SettingsManager.Local.Load();
        McpConfigService configService = new McpConfigService(settings.McpConfigToken);
        McpServerConfig serverConfig = await configService.GetConfig();
        if (serverConfig == null)
        {
            BtnInstall.IsEnabled = true;
            BtnInstall.Content = "Install ¡õ";
            isBusy = false;
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
        isBusy = false;
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
                case "bun":
                case "bunx":
                    requiredApps.Add("NodeJS");
                    requiredApps.Add(mcpServer.command);
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
        Launcher.LaunchUriAsync(new Uri(detailBase.GithubUrl));
    }

    private void BtnMcpSo_OnClick(object sender, RoutedEventArgs e)
    {
        Launcher.LaunchUriAsync(new Uri(detailBase.Url));
    }

    private void Window_OnClosing(object sender, WindowClosingEventArgs e)
    {
        if (isBusy)
        {
            e.Cancel = true;
        }
    }
}