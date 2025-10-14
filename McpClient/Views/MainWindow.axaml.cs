using Avalonia.Controls;
using Avalonia.Interactivity;
using McpClient.Models;
using McpClient.Services;
using McpClient.Utils;
using McpClient.ViewModels;
using NetSparkleUpdater;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;

namespace McpClient.Views;

public partial class MainWindow : Window
{
    private BackgroundWorker ragWorker;
    private SparkleUpdater sparkle;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;

        IsEnabled = false;
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
        TbLogin.IsVisible = false;
        MainView.IsVisible = true;

        // Create default MCP filesystem folder
        await CreateMcpFileSytemFolder();

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

        // Start MCP host service
        GlobalService.NodeJsService = McpNodeJsService.CreateMcpNodeJsService();
        GlobalService.NodeJsService.Start();

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
        TbLogin.IsVisible = true;
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
        Settings settings = SettingsManager.Local.Load();
        if (string.IsNullOrEmpty(settings.FileSystemFolder))
        {
            settings.FileSystemFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "MCP File System");
            await SettingsManager.Local.SaveAsync(settings);
        }
        if (!Directory.Exists(settings.FileSystemFolder))
        {
            try
            {
                Directory.CreateDirectory(settings.FileSystemFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
