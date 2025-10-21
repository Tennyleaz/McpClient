using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace McpClient.Views;

public partial class McpShareFolderSettingWindow : Window
{
    public McpShareFolderSettingWindow()
    {
        InitializeComponent();
    }

    private async void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;

        //ShareFolderSetting.UpdateShareFolderToUi();
        await ShareFolderSetting.UpdateShareFolderToUiFromServer();
    }
}