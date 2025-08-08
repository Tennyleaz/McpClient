using Avalonia;
using Avalonia.Styling;

namespace McpClient.ViewModels;

public class MainViewModel : ViewModelBase
{
    public string Greeting => "Welcome to Avalonia!";

    public bool IsNightMode
    {
        get
        {
            return Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        }
        set
        {
            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
            }
        }
    }
}
