using System.Collections.Generic;
using Avalonia;
using Avalonia.Styling;
using ReactiveUI;

namespace McpClient.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {

    }

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
                this.RaisePropertyChanged();
            }
        }
    }

    public List<MainListboxItem> MainItems { get; } = new()
    {
        new MainListboxItem
        {
            Text = "My Apps",
            Icon = "🔥"
        },
        new MainListboxItem
        {
            Text = "Local Workflows",
            Icon = "🏠"
        },
        new MainListboxItem
        {
            Text = "Agents",
            Icon = "🤖"
        },
        new MainListboxItem
        {
            Text = "Chat",
            Icon = "📣"
        },
        new MainListboxItem
        {
            Text = "Store",
            Icon = "🏪"
        },
        new MainListboxItem
        {
            Text = "Services",
            Icon = "⚙️"
        }
    };
}

public class MainListboxItem
{
    public string Text { get; set; }
    public string Icon { get; set; }
}
