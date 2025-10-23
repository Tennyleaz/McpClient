using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using McpClient.Models;
using McpClient.Services;
using McpClient.Utils;
using McpClient.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace McpClient.Views;

/// <summary>
/// See:
/// https://modelcontextprotocol.io/specification/2025-06-18/client/roots
/// </summary>
public partial class McpShareFolderSetting : UserControl
{
    private readonly ObservableCollection<FileSystemFolderViewModel> _shareFolders = new();
    private McpConfigService _mcpConfigService;

    public McpShareFolderSetting()
    {
        InitializeComponent();
    }

    internal void SetServices(McpConfigService mcpConfigService)
    {
        _mcpConfigService = mcpConfigService;
    }

    [Obsolete("Use UpdateShareFolderToUiFromSetting", true)]
    internal void UpdateShareFolderToUi()
    {
        _shareFolders.Clear();
        foreach (string f in GlobalService.FileSystemFolders)
        {
            _shareFolders.Add(new FileSystemFolderViewModel { Folder = f });
        }
        ShareFoldersControl.ItemsSource = _shareFolders;
    }

    [Obsolete("Use UpdateShareFolderToUiFromSetting", true)]
    internal async Task UpdateShareFolderToUiFromServer()
    {
        _shareFolders.Clear();

        McpServerConfig config = await _mcpConfigService.GetConfig();
        McpServer fsServer = config.mcp_servers.FirstOrDefault(x => x.IsFileSystem());
        if (fsServer != null)
        {
            const int startIndex = 2;
            for (int i = startIndex; i < fsServer.args.Count; i++)
            {
                _shareFolders.Add(new FileSystemFolderViewModel { Folder = fsServer.args[i] }); 
            }
        }

        ShareFoldersControl.ItemsSource = _shareFolders;
    }

    internal async Task UpdateShareFolderToUiFromSetting()
    {
        _shareFolders.Clear();
        List<string> roots = await McpFileSystemRootUtils.GetRootsFromSetting();
        foreach (string f in roots)
        {
            _shareFolders.Add(new FileSystemFolderViewModel { Folder = f });
        }

        GlobalService.FileSystemFolders = roots;
        ShareFoldersControl.ItemsSource = _shareFolders;
    }

    private async void BtnChangeFileSystem_OnClick(object sender, RoutedEventArgs e)
    {
        IStorageProvider storage = TopLevel.GetTopLevel(this).StorageProvider;
        IStorageFolder documentsFolder = await storage.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
        var selectedFolders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            SuggestedStartLocation = documentsFolder
        });

        if (selectedFolders.Count > 0)
        {
            IStorageFolder selected = selectedFolders.First();
            string folder = selected.TryGetLocalPath();

            // Add to global settings
            GlobalService.FileSystemFolders.Add(folder);
            _shareFolders.Add(new FileSystemFolderViewModel { Folder = folder });

            // Update to MCP host
            await UpdateShareFoldersToSetting(GlobalService.FileSystemFolders);
        }
    }

    private async void BtnRemoveFolder_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is FileSystemFolderViewModel vm)
        {
            string removed = vm.Folder;

            GlobalService.FileSystemFolders.RemoveAll(x => x == removed);
            _shareFolders.Remove(vm);

            // Update to MCP host
            await UpdateShareFoldersToSetting(GlobalService.FileSystemFolders);
        }
    }

    [Obsolete("Use UpdateShareFoldersToSetting", true)]
    private async Task<bool> UpdateShareFoldersToServer(List<string> folders)
    {
        McpServerConfig config = await _mcpConfigService.GetConfig();
        McpServer fsServer = config.mcp_servers.FirstOrDefault(x => x.IsFileSystem());
        if (fsServer != null)
        {
            int startIndex = 2;
            foreach (string f in folders)
            {
                if (fsServer.args.Count > startIndex)
                {
                    fsServer.args[startIndex] = f;
                }
                else
                {
                    fsServer.args.Add(f);
                }
                startIndex++;
            }

            fsServer.enabled = true;
            return await _mcpConfigService.SetConfig(config);
        }

        return false;
    }

    private async Task<bool> UpdateShareFoldersToSetting(List<string> folders)
    {
        if (await McpFileSystemRootUtils.SetRootsToSetting(folders))
        {
            return await GlobalService.NodeJsService.NotifyFileSystemRootListChanged();
        }

        return false;
    }
}