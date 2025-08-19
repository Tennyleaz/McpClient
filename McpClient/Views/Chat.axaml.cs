using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using McpClient.Models;
using WebViewControl;

namespace McpClient.Views;

public partial class Chat : UserControl
{
    public Chat()
    {
        InitializeComponent();
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        ChatWebView.AllowDeveloperTools = true;
    }

    public void ReloadWebview()
    {
        ChatWebView.Reload();
    }

    public async Task SetToken(string username, string token)
    {
        WebviewToken webviewToken = new WebviewToken
        {
            State = new WebviewTokenState
            {
                Username = username,
                Token = token,
            },
            Version = 0
        };

        string json = JsonSerializer.Serialize(webviewToken, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        string script = $"window.localStorage.setItem('user-auth-storage', '{json}');";
        object result = await ChatWebView.EvaluateScript<object>(script);
        Debug.WriteLine(result);

        ChatWebView.Reload();
    }

    private async Task RunScript()
    {
        // test print
        string script = "console.log(window.localStorage.getItem('user-auth-storage'));";
        await ChatWebView.EvaluateScript<object>(script);

        // test get
        script = "window.localStorage.getItem('user-auth-storage');";
        string result = await ChatWebView.EvaluateScript<string>(script);
        Debug.WriteLine(result);

        result = await ChatWebView.EvaluateScript<string>("'hello';");
        Debug.WriteLine(result);
    }

    private async void BtnDebug_OnClick(object sender, RoutedEventArgs e)
    {
        await RunScript();
    }

    private void BtnF12_OnClick(object sender, RoutedEventArgs e)
    {
        ChatWebView.AllowDeveloperTools = true;
        ChatWebView.ShowDeveloperTools();
    }

    private async void ChatWebView_OnBeforeNavigate(Request request)
    {
        Settings settings = SettingsManager.Local.Load();
        await SetToken(settings.UserName, settings.McpConfigToken);

        // Remove event handler beacause we only need to set token once
        ChatWebView.BeforeNavigate -= ChatWebView_OnBeforeNavigate;
    }
}