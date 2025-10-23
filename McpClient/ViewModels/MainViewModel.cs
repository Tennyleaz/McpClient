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
            Text = "我的應用程式",
            Icon = "🔥"
        },
        new MainListboxItem
        {
            Text = "工作流程",
            Icon = "🏠"
        },
        new MainListboxItem
        {
            Text = "AI 工具",
            Icon = "🤖"
        },
        new MainListboxItem
        {
            Text = "智能聊天",
            Icon = "📣"
        },
        new MainListboxItem
        {
            Text = "工具市集",
            Icon = "🏪"
        },
        new MainListboxItem
        {
            Text = "系統服務",
            Icon = "⚙️"
        }
    };
}

public class MainListboxItem
{
    public string Text { get; set; }
    public string Icon { get; set; }
}
