using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using McpClient.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace McpClient.Views;

public partial class PlaywrightSettingWindow : Window
{
    private readonly McpViewModel _mcpViewModel;

    public PlaywrightSettingWindow()
    {
        InitializeComponent();
    }

    internal PlaywrightSettingWindow(McpViewModel mcpViewModel)
    {
        InitializeComponent();
        _mcpViewModel = mcpViewModel;
        LoadFromSetting();
    }

    private void LoadFromSetting()
    {
        CbBrowser.SelectedIndex = 0;

        foreach (string arg in _mcpViewModel.Args)
        {
            if (arg.StartsWith("--browser="))
            {
                string browser = arg.Substring("--browser=".Length);
                switch (browser)
                {
                    default:
                        CbBrowser.SelectedIndex = 0;
                        break;
                    case "chrome":
                        CbBrowser.SelectedIndex = 1;
                        break;
                    case "firefox":
                        CbBrowser.SelectedIndex = 2;
                        break;
                    case "msedge":
                        CbBrowser.SelectedIndex = 3;
                        break;
                    case "webkit":
                        CbBrowser.SelectedIndex = 4;
                        break;
                }
            }
            else if (arg == "--headless")
            {
                ChkHeadless.IsChecked = true;
            }
            else if (arg == "--allowed-hosts *")
            {
                ChkDisableHostCheck.IsChecked = true;
            }
            else if (arg == "--block-service-workers")
            {
                ChkBlockServiceWorkers.IsChecked = true;
            }
            else if (arg == "--ignore-https-errors")
            {
                ChkIgnoreHttpsErrors.IsChecked = true;
            }
            else if (arg == "--isolated")
            {
                ChkIsolated.IsChecked = true;
            }
            else if (arg.StartsWith("--timeout-action="))
            {
                string timeout = arg.Substring("--timeout-action=".Length);
                TbTimeout.Text = timeout;
            }
            else if (arg.StartsWith("--viewport-size"))
            {
                // for example "1280x720"
                Regex regex = new Regex(@"--viewport-size=""(\d+)x(\d+)""");
                Match match = regex.Match(arg);
                if (match.Success)
                {
                    // Group starts at 1
                    TbViewportX.Text = match.Groups[1].Value;
                    TbViewportY.Text = match.Groups[2].Value;
                }
            }
        }
    }

    private bool SaveToSettings()
    {
        // parse text to number
        if (!int.TryParse(TbTimeout.Text, out int timeout) || timeout <= 0)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please input valid timeout text.",
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            box.ShowWindowDialogAsync(this);
            return false;
        }
        if (!int.TryParse(TbViewportX.Text, out int viewportX) || viewportX <= 0)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please input valid viewport X text.",
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            box.ShowWindowDialogAsync(this);
            return false;
        }
        if (!int.TryParse(TbViewportY.Text, out int viewportY) || viewportY <= 0)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please input valid viewport Y text.",
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            box.ShowWindowDialogAsync(this);
            return false;
        }

        // Clear options, only leave the first argument "@playwright/mcp@latest"
        string firstArg = _mcpViewModel.Args.First();
        _mcpViewModel.Args.Clear();
        _mcpViewModel.Args.Add(firstArg);
        _mcpViewModel.Args.Add($"--timeout-action={timeout}");
        _mcpViewModel.Args.Add($"--viewport-size={viewportX}x{viewportY}");

        // Other options
        if (CbBrowser.SelectedIndex <= 0)
        {
            // default browser
        }
        else
        {
            _mcpViewModel.Args.Add($"--browser={(CbBrowser.SelectedItem as ComboBoxItem)?.Content}");
        }

        if (ChkHeadless.IsChecked == true)
        {
            _mcpViewModel.Args.Add("--headless");
        }

        if (ChkDisableHostCheck.IsChecked == true)
        {
            _mcpViewModel.Args.Add("--allowed-hosts *");
        }

        if (ChkBlockServiceWorkers.IsChecked == true)
        {
            _mcpViewModel.Args.Add("--block-service-workers");
        }

        if (ChkIgnoreHttpsErrors.IsChecked == true)
        {
            _mcpViewModel.Args.Add("--ignore-https-errors");
        }

        if (ChkIsolated.IsChecked == true)
        {
            _mcpViewModel.Args.Add("--isolated");
        }

        return true;
    }

    private void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
    }

    private void Window_OnClosing(object sender, WindowClosingEventArgs e)
    {
        if (!SaveToSettings())
        {
            e.Cancel = true;
        }
    }
}