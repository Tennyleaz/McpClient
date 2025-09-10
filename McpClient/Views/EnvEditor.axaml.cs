using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace McpClient.Views;

public partial class EnvEditor : Window
{
    private readonly bool _isEditHttpHeader = false;

    public EnvEditor(bool isEditHttpHeader)
    {
        InitializeComponent();
        _isEditHttpHeader = isEditHttpHeader;

        if (isEditHttpHeader)
        {
            Title = "Edit HTTP Headers";
            TitleTextbox.Text = "HTTP Headers:";
            BtnAdd.Content = "Add HTTP Header";
        }
    }
}