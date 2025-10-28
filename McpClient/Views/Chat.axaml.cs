using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using McpClient.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebViewControl;
using Xilium.CefGlue;

namespace McpClient.Views;

public partial class Chat : UserControl
{
    private TennyObject tennyObject;
    private string _token;
    private const string SERVER_URL = "http://192.168.41.60";

    public bool IsNeedRefreshWorkflow { get; private set; }

    public event EventHandler TokenExpired;
    public event EventHandler<string> ReDirectMcpStore;

    public Chat()
    {
        if (Design.IsDesignMode)
            return;

        // Create cache
        string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string cacheDir = Path.Combine(appdata, "McpClient", "Webview");
        if (!Directory.Exists(cacheDir))
            Directory.CreateDirectory(cacheDir);
        WebView.Settings.CachePath = cacheDir;
        WebView.Settings.PersistCache = true;  // must set before webview created
        WebView.Settings.AddCommandLineSwitch("disable-features", "BlockInsecurePrivateNetworkRequests");
        WebView.Settings.AddCommandLineSwitch("disable-web-security", "true");
        InitializeComponent();
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;

        ChatWebView.IsSecurityDisabled = true;
        ChatWebView.IgnoreCertificateErrors = true;
        ChatWebView.BeforeNavigate += ChatWebView_OnBeforeNavigate;
        ChatWebView.Navigated += ChatWebView_Navigated;
        //ChatWebView.BeforeResourceLoad += ChatWebView_BeforeResourceLoad;
        ChatWebView.DisableBuiltinContextMenus = true;

        // see
        // https://github.com/chromiumembedded/cef/issues/3739
        var value = CefValue.Create();
        value.SetBool(true);
        var context = CefRequestContext.GetGlobalContext();
        string name = "plugins.always_open_pdf_externally";
        if (context.CanSetPreference(name))
        {
            bool success = context.SetPreference(name, value, out string errr);
            Debug.WriteLine(errr);
        }
    }

    private void ChatWebView_Navigated(string url, string frameName)
    {
        ProgressRing.IsVisible = false;
        ChatWebView.IsVisible = true;
    }

    public void LoadChatServer()
    {
        // Reset every time chat is visible
        IsNeedRefreshWorkflow = false;

        // Only inject object once
        if (tennyObject != null)
            return;

        ChatWebView.IsVisible = false;
        ProgressRing.IsVisible = true;

        //ChatWebView.Address = SERVER_URL;
        //ChatWebView.Address = "http://localhost:5174/";
        string file = Path.Combine(GlobalService.ChatFrontendFolder, "index.html");
        if (File.Exists(file))
        {
            ChatWebView.LoadUrl(file);
            Console.WriteLine("LoadChatServer() loaded URL:\n" + file);
        }
        else
        {
            //ChatWebView.Address = SERVER_URL;
            Console.WriteLine("LoadChatServer() cannot load local website:\n" + file);
        }
        //ChatWebView.Address = "http://myapp/index.html";

        tennyObject = new TennyObject();
        bool b = ChatWebView.RegisterJavascriptObject("injectedObject", tennyObject);
        tennyObject.OnRefresh += (o, args) =>
        {
            //Dispatcher.UIThread.Invoke(() =>
            //{
            //    var box = MessageBoxManager.GetMessageBoxStandard("Test", "Refresh method is called",
            //        ButtonEnum.Ok,
            //        MsBox.Avalonia.Enums.Icon.Info);
            //    box.ShowWindowDialogAsync(TopLevel.GetTopLevel(this) as Window);
            //});
            IsNeedRefreshWorkflow = true;
        };
        tennyObject.OnDownload += (o, args) =>
        {
            Dispatcher.UIThread.Invoke(async () =>
            {
                await OnDownloadFile(args);
            });
        };
        tennyObject.OnTokenExpired += (o, args) =>
        {
            // This event is raised on background thread
            Dispatcher.UIThread.Invoke(() =>
            {
                // Tell main window to logout
                TokenExpired?.Invoke(this, EventArgs.Empty);
            });
        };
        tennyObject.OnRedirectMcpStore += (o, url) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                // Tell main window go to MCP store
                ReDirectMcpStore?.Invoke(this, url);
            });
        };
    }

    public void ReloadWebview()
    {
        ChatWebView.Reload();
    }

    public void Deactivate()
    {
        
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

        try
        {
            string script = $"window.localStorage.setItem('user-auth-storage', '{json}');";
            object result = await ChatWebView.EvaluateScript<object>(script);
            Debug.WriteLine(result);

            script = $"window.localStorage.setItem('test-item', 'test');";
            result = await ChatWebView.EvaluateScript<object>(script);
            Debug.WriteLine(result);

            ChatWebView.Reload();
            _token = token;
        }
        catch (Exception ex)
        {
            Console.WriteLine("SetToken() error: " + ex);
        }
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
        //if (request.Url.EndsWith(".pdf"))
        //{
        //    request.Cancel();
        //    return;
        //}

        //if (request.Url.StartsWith(SERVER_URL))
        {
            Settings settings = SettingsManager.Local.Load();
            await SetToken(settings.UserName, settings.McpConfigToken);
        }

        // Remove event handler beacause we only need to set token once
        //ChatWebView.BeforeNavigate -= ChatWebView_OnBeforeNavigate;
    }

    private void ChatWebView_BeforeResourceLoad(ResourceHandler resourceHandler)
    {
        // http://myapp/index.html -> file:///D:/tenny_lu/Downloads/dist/index.html
        Debug.WriteLine(resourceHandler.Url);
        if (resourceHandler.Url.StartsWith("http://myapp/")) 
        {
            string newUrl = resourceHandler.Url.Replace("http://myapp/", "file:///D:/tenny_lu/Downloads/dist/");
            resourceHandler.Redirect(newUrl);
        }
    }

    internal class TennyObject
    {
        public event EventHandler OnRefresh, OnTokenExpired;
        public event EventHandler<string> OnDownload, OnRedirectMcpStore;

        public void NotifyRefreshMcp()
        {
            OnRefresh?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyFileDownload(string url)
        {
            OnDownload?.Invoke(this, url);
        }

        public void NotifyTokenExpired()
        {
            OnTokenExpired?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyReDirectMcpStore(string url)
        {
            OnRedirectMcpStore?.Invoke(this, url);
        }
    }

    private async Task OnDownloadFile(string url)
    {
        using HttpClient httpClient = new HttpClient();
        if (!string.IsNullOrEmpty(_token))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        try
        {
            // create a temp file
            string tempFile = System.IO.Path.GetTempFileName();

            using HttpResponseMessage response = await httpClient.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead);
            if (response.IsSuccessStatusCode)
            {
                string filename = GetFileNameFromContentDisposition(response.Content.Headers)
                                  ?? GetFileNameFromUrl(url);

                //byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
                using FileStream fs = new FileStream(tempFile, FileMode.Create);
                await response.Content.CopyToAsync(fs);
                await fs.FlushAsync();
                fs.Close();

                // ask user where to save
                IStorageProvider storage = TopLevel.GetTopLevel(this).StorageProvider;
                IStorageFolder documentsFolder = await storage.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
                var selectedFiles = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    ShowOverwritePrompt = true,
                    SuggestedStartLocation = documentsFolder,
                    SuggestedFileName = filename,
                    Title = "Save file as..."
                });

                if (selectedFiles != null)
                {
                    string localPath = selectedFiles.TryGetLocalPath();
                    File.Move(tempFile, localPath);
                    return;
                }

                // delete temp file
                File.Delete(tempFile);
            }
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Failed to save file: " + ex.Message,
                ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Error);
            await box.ShowWindowDialogAsync(TopLevel.GetTopLevel(this) as Window);
        }
    }

    // Extract filename from Content-Disposition header, if present
    private static string GetFileNameFromContentDisposition(HttpContentHeaders headers)
    {
        if (headers.ContentDisposition != null && !string.IsNullOrEmpty(headers.ContentDisposition.FileNameStar))
        {
            return Unquote(headers.ContentDisposition.FileNameStar);
        }
        if (headers.ContentDisposition != null && !string.IsNullOrEmpty(headers.ContentDisposition.FileName))
        {
            return Unquote(headers.ContentDisposition.FileName);
        }

        // Fallback: check raw header (some servers send non-standard headers)
        if (headers.TryGetValues("Content-Disposition", out var values))
        {
            var value = string.Join("; ", values);
            var match = Regex.Match(value, @"filename\*?=(?:UTF-8''|\""?)(?<file>[^\"";\r\n]+)");
            if (match.Success)
                return match.Groups["file"].Value;
        }
        return null;

        static string Unquote(string value) => value?.Trim('\"');
    }

    // Fallback: Extract from URL path
    private static string GetFileNameFromUrl(string url)
    {
        var uri = new Uri(url);
        return System.IO.Path.GetFileName(uri.LocalPath);
    }
}