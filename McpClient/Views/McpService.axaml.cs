using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Dialogs;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace McpClient.Views;

public partial class McpService : UserControl
{
    private Settings settings;

    public McpService()
    {
        InitializeComponent();
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;
    }

    internal void LoadFromSettings()
    {
        settings = SettingsManager.Local.Load();
        TbRagFolder.Text = "RAG Folder: " + settings.RagFolder;
        if (Directory.Exists(settings.RagFolder))
        {
            TbRagStatus.Text = "Running";
        }
        else
        {
            TbRagStatus.Text = "Stopped";
        }
    }

    private async void BtnChangeRag_OnClick(object sender, RoutedEventArgs e)
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
            settings.RagFolder = folder;
            await SettingsManager.Local.SaveAsync(settings);

            TbRagFolder.Text = "RAG Folder: " + settings.RagFolder;
            TbRagStatus.Text = "Running";
        }
    }
}