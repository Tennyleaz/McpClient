using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace McpClient.Views;

public partial class RunTaskWindow : Window
{
    private readonly string _serverName;

    public RunTaskWindow()
    {
        InitializeComponent();
    }

    public RunTaskWindow(string serverName)
    {
        InitializeComponent();
        _serverName = serverName;
        LbHeader.Content += ": " + serverName;
    }
}