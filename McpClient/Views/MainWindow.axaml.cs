using Avalonia.Controls;
using Avalonia.Interactivity;

namespace McpClient.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Control_OnLoaded(object sender, RoutedEventArgs e)
    {        
        // show login window
        Settings settings = SettingsManager.Local.Load();
        if (string.IsNullOrWhiteSpace(settings.UserName) || string.IsNullOrWhiteSpace(settings.Token))
        {
            LoginWindow loginWindow = new LoginWindow();
            await loginWindow.ShowDialog(this);
        }
    }
}
