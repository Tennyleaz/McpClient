using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using McpClient.Models;
using McpClient.Services;
using McpClient.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace McpClient.Views;

public partial class McpShareFolderSetting : UserControl
{
    private readonly List<FileSystemFolderViewModel> _shareFolders = new();
    private McpConfigService _mcpConfigService;

    public McpShareFolderSetting()
    {
        InitializeComponent();
    }

    internal void SetServices(McpConfigService mcpConfigService)
    {
        _mcpConfigService = mcpConfigService;
    }

    internal void UpdateShareFolderToUi()
    {
        _shareFolders.Clear();
        foreach (string f in GlobalService.FileSystemFolders)
        {
            _shareFolders.Add(new FileSystemFolderViewModel { Folder = f });
        }
        ShareFoldersControl.ItemsSource = _shareFolders;
    }

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
            await UpdateShareFoldersToServer(GlobalService.FileSystemFolders);
        }
    }

    private async void BtnRemoveFolder_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is FileSystemFolderViewModel vm)
        {
            string removed = vm.Folder;

            GlobalService.FileSystemFolders.RemoveAll(x => x == removed);
            _shareFolders.RemoveAll(x => x.Folder == removed);

            // Update to MCP host
            await UpdateShareFoldersToServer(GlobalService.FileSystemFolders);
        }
    }

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
}