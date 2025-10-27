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
            Icon = "🔥",
            ItemType = MainListType.Apps
        },
        new MainListboxItem
        {
            Text = "工作流程",
            Icon = "🏠",
            ItemType = MainListType.LocalWorkflow
        },
        new MainListboxItem
        {
            Text = "AI 工具",
            Icon = "🤖",
            ItemType = MainListType.McpTools
        },
        new MainListboxItem
        {
            Text = "智能聊天",
            Icon = "📣",
            ItemType = MainListType.Chat
        },
        new MainListboxItem
        {
            Text = "工具市集",
            Icon = "🏪",
            ItemType = MainListType.McpStore
        },
        new MainListboxItem
        {
            Text = "系統服務",
            Icon = "⚙️",
            ItemType = MainListType.SystemService
        }
    };
}

public class MainListboxItem
{
    public string Text { get; set; }
    public string Icon { get; set; }
    public MainListType ItemType { get; set; }
}

public enum MainListType
{
    Apps = 0,
    LocalWorkflow,
    McpTools,
    Chat,
    McpStore,
    AppStore,
    SystemService
}
