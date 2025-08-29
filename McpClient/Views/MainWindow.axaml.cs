using Avalonia.Controls;
using Avalonia.Interactivity;
using McpClient.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using McpClient.Models;

namespace McpClient.Views;

public partial class MainWindow : Window
{
    private BackgroundWorker ragWorker;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        IsEnabled = false;
        bool isLogin = await Login();
        IsEnabled = true;
        if (isLogin)
        {
            await MainView.ReloadMainView();
        }

        // start upload docments in background

        ragWorker = new BackgroundWorker();
        ragWorker.WorkerSupportsCancellation = true;
        ragWorker.DoWork += RagWorker_DoWork;
        ragWorker.RunWorkerAsync();
    }

    private void Window_OnClosing(object sender, WindowClosingEventArgs e)
    {
        ragWorker?.CancelAsync();
        GlobalService.LlamaService?.Stop();
        GlobalService.LlamaService?.Dispose();
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
        // Test saved token
        Settings settings = SettingsManager.Local.Load();
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
}
