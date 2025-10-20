using Avalonia.Controls;
using Avalonia.Interactivity;
using McpClient.Models;
using McpClient.Services;
using McpClient.Utils;
using McpClient.ViewModels;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace McpClient.Views;

public partial class MainWindow : Window
{
    private BackgroundWorker ragWorker;
    private SparkleUpdater sparkle;

    public MainWindow()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            return;

        // Hide main view and show login text
        LoginPanel.IsVisible = true;
        MainView.IsVisible = false;
    }

    private async void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;

        IsEnabled = false;

        // Create default MCP filesystem folder
        await CreateMcpFileSytemFolder();

        // Start the backend services
        StartServices();

        bool isLogin = await Login();
        IsEnabled = true;
        if (isLogin)
        {
            await MainView.ReloadMainView();
        }
        else
        {
            return;
        }

        // show main UI
        LoginPanel.IsVisible = false;
        MainView.IsVisible = true;

        // check MCP tool runtime dependency
        LocalCommandWizard wizard = new LocalCommandWizard();
        bool needInstall = wizard.GenerateInstalledRuntimeViewModel();
        if (needInstall)
        {
            await wizard.ShowDialog(this);
        }

        // start upload docments in background
        ragWorker = new BackgroundWorker();
        ragWorker.WorkerSupportsCancellation = true;
        ragWorker.DoWork += RagWorker_DoWork;
        ragWorker.RunWorkerAsync();

        GlobalService.KnownCommands = LocalServiceUtils.ListKnownLocalServices();

        // Start LLM service

        // Check for update
        const string pubKey = "0pYHDlcVVP0l2JhsjSjBqH6SW447IF7oqjcksn1UXjk=";
        const string URL = "http://192.168.41.173:8080/appcast.xml";
        sparkle = new SparkleUpdater(
            URL, // link to your app cast file - change extension to .json if using json
            new Ed25519Checker(SecurityMode.Unsafe, // security mode -- use .Unsafe to ignore all signature checking (NOT recommended!!)
                pubKey) // your base 64 public key
        )
        {
            UIFactory = new NetSparkleUpdater.UI.Avalonia.UIFactory(Icon), // or null, or choose some other UI factory, or build your own IUIFactory implementation!
            RelaunchAfterUpdate = false, // set to true if needed
        };
        sparkle.UpdateDetected += Sparkle_UpdateDetected;
        await sparkle.StartLoop(true, true);
    }

    private void Sparkle_UpdateDetected(object sender, NetSparkleUpdater.Events.UpdateDetectedEventArgs e)
    {
        MainView.BtnUpdate.IsVisible = true;
    }

    private void StartServices()
    {
        // Start MCP host service
        GlobalService.NodeJsService = McpNodeJsService.CreateMcpNodeJsService();
        GlobalService.NodeJsService.Start();

        // Start .net backend
        GlobalService.BackendService = DispatcherBackendService.CreateBackendService();
        GlobalService.BackendService.Start();
    }

    private void Window_OnClosing(object sender, WindowClosingEventArgs e)
    {
        // hide main UI
        Hide();

        sparkle?.StopLoop();
        sparkle?.Dispose();
        ragWorker?.CancelAsync();
        GlobalService.LlamaService?.Stop();
        GlobalService.LlamaService?.Dispose();
        GlobalService.NodeJsService?.Stop();
        GlobalService.BackendService?.Stop();

        // Save current darkmode state also
        bool isDark = GlobalService.MainViewModel.IsNightMode;
        Settings settings = SettingsManager.Local.Load();
        if (settings.IsDarkMode != isDark)
        {
            settings.IsDarkMode = isDark;
            SettingsManager.Local.Save(settings);
        }
    }

    private async void MainView_OnLogoutClick(object sender, EventArgs e)
    {
        // Delete saved token
        Settings settings = SettingsManager.Local.Load();
        settings.AiNexusToken = null;
        settings.McpConfigToken = null;
        settings.UserName = null;
        settings.ExpiredAt = default;
        await SettingsManager.Local.SaveAsync(settings);

        // Hide main UI
        TbLogin.Text = "You have been logged out.\nPlease login again.";
        LoginPanel.IsVisible = true;
        MainView.IsVisible = false;

        // Login again
        IsEnabled = false;
        bool isLogin = await Login();
        IsEnabled = true;
        if (isLogin)
        {
            await MainView.ReloadMainView();
        }
    }

    private async Task<bool> Login()
    {
        // Load darkmode
        Settings settings = SettingsManager.Local.Load();
        GlobalService.MainViewModel.IsNightMode = settings.IsDarkMode;

        // Show username
        if (!string.IsNullOrWhiteSpace(settings.UserName))
        {
            TbLogin.Text = $"Logging in AI Nexus as {settings.UserName} ...";
        }

        // Test saved token
        if (!string.IsNullOrWhiteSpace(settings.McpConfigToken))
        {
            bool success = await IsMcpConfigTokenValid(settings.McpConfigToken) &&
                           await IsAiNexusTokenValid(settings.AiNexusToken);
            if (success)
            {
                // Do not login again
                return true;
            }
            // Remove old token
            settings.McpConfigToken = null;
            settings.AiNexusToken = null;
            await SettingsManager.Local.SaveAsync(settings);
        }

        // Show login window
        LoginWindow loginWindow = new LoginWindow();
        await loginWindow.ShowDialog(this);
        string token = loginWindow.Token;
        if (string.IsNullOrEmpty(token))
        {
            // Close on no token
            Close();
            return false;
        }

        return true;
    }

    private async void RagWorker_DoWork(object sender, DoWorkEventArgs e)
    {
        List<DocumentHistory> histories = SettingsManager.Local.LoadDocumentHistories();
        RagService service = new RagService();

        do
        {
            Settings settings = SettingsManager.Local.Load();
            string folder = settings.RagFolder;
            if (Directory.Exists(folder) && !string.IsNullOrEmpty(settings.McpConfigToken))
            {
                IEnumerable<string> files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    if (file.EndsWith(".txt") || file.EndsWith(".pdf") || file.EndsWith(".docx"))
                    {
                        // check for duplicate
                        if (histories.Exists(x => string.Equals(x.FullPath, file, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            continue;
                        }

                        // upload
                        DateTime time = DateTime.Now;
                        byte[] buffer = await File.ReadAllBytesAsync(file);
                        DocumentDto dto = new DocumentDto
                        {
                            Name = Path.GetFileName(file),
                            CreatedTime = time,
                            Data = buffer,
                            ChatId = null
                        };
                        bool succss = await service.UploadDocument(dto);

                        // save in history
                        histories.Add(new DocumentHistory
                        {
                            FullPath = file,
                            UploadTime = time
                        });
                    }
                }
            }

            // wait 1 minutes
            await Task.Delay(TimeSpan.FromMinutes(1));
        } while (!ragWorker.CancellationPending);

        await SettingsManager.Local.SaveDocumentHistoriesAsync(histories);
    }

    private static async Task<bool> IsMcpConfigTokenValid(string token)
    {
        McpConfigService service = new McpConfigService(token);
        bool success = await service.IsLogin();
        return success;
    }

    private static async Task<bool> IsAiNexusTokenValid(string token)
    {
        AiNexusService service = new AiNexusService(token);
        var groups = await service.GetAllGroups();
        return groups != null;
    }

    private static async Task CreateMcpFileSytemFolder()
    {
        // Update this setting to MCP host config
        try
        {
            string path = GlobalService.McpHostConfigFile;
            string json = await File.ReadAllTextAsync(path);
            McpServerConfig config = JsonSerializer.Deserialize<McpServerConfig>(json);

            foreach (McpServer server in config.mcp_servers)
            {
                if (server.server_name == "filesystem" && server.type == McpServerType.Stdio)
                {
                    GlobalService.FileSystemFolders.Clear();
                    // "npx"
                    // "-y","@modelcontextprotocol/server-filesystem", "/path/to/allowed/dir", ...
                    // Update the 3rd parameter
                    const int index = 2;
                    if (server.args.Count <= index)
                    {
                        // Create default folder and save to it
                        string defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "McpFileSystem");
                        Directory.CreateDirectory(defaultDir);
                        server.args.Add(defaultDir);
                        // Update to global variable
                        GlobalService.FileSystemFolders.Add(defaultDir);
                    }
                    else
                    {
                        // Update to global variable
                        for (int i = index; i < server.args.Count; i++)
                        {
                            if (Path.IsPathFullyQualified(server.args[i]))
                            {
                                GlobalService.FileSystemFolders.Add(server.args[i]);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
