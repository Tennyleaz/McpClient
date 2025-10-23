using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Dialogs;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using McpClient.Models;
using McpClient.Services;
using McpClient.Utils;
using McpClient.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace McpClient.Views;

public partial class SystemSetting : UserControl
{
    private Settings settings;
    private readonly DispatcherTimer checkStatusTimer;

    public SystemSetting()
    {
        InitializeComponent();

        checkStatusTimer = new DispatcherTimer();
        checkStatusTimer.Interval = TimeSpan.FromSeconds(1);
        checkStatusTimer.Tick += CheckStatusTimer_Tick;
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;

        //LoadGpuStatus();
    }

    internal void SetServices(McpConfigService mcpConfigService)
    {
        McpShareFolderSetting.SetServices(mcpConfigService);
    }

    internal async Task LoadFromSettings()
    {
        settings = SettingsManager.Local.Load();
        TbRagFolder.Text = settings.RagFolder;
        if (Directory.Exists(settings.RagFolder))
        {
            TbRagStatus.Text = "Running";
            TbRagStatus.Foreground = Brushes.Green;
        }
        else
        {
            TbRagStatus.Text = "Stopped";
            TbRagStatus.Foreground = Brushes.Red;
            TbRagFolder.Text = "RAG folder not set.";
        }

        await McpShareFolderSetting.UpdateShareFolderToUiFromSetting();

        // llama service status
        UpdateCliServiceStatus(GlobalService.LlamaService, TbllmStatus);
        UpdateCliServiceStatus(GlobalService.NodeJsService, TbNodeJsStatus);
        UpdateCliServiceStatus(GlobalService.BackendService, TbDotnetBackendStatus);
        UpdateCliServiceStatus(GlobalService.RagBackendService, TbRagBackendStatus);
        UpdateCliServiceStatus(GlobalService.ChromaDbService, TbChromaDbStatus);
        StartMonitorServices();
    }

    internal void StartMonitorServices()
    {
        checkStatusTimer.Start();
    }

    internal void StopMonitorServices()
    {
        checkStatusTimer.Stop();
    }

    private async void BtnChangeRag_OnClick(object sender, RoutedEventArgs e)
    {
        IStorageProvider storage = TopLevel.GetTopLevel(this).StorageProvider;
        IStorageFolder documentsFolder = await storage.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
        var selectedFolders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            SuggestedStartLocation = documentsFolder
        });

        if (selectedFolders.Count > 0)
        {
            IStorageFolder selected = selectedFolders.First();
            string folder = selected.TryGetLocalPath();
            settings.RagFolder = folder;
            await SettingsManager.Local.SaveAsync(settings);

            TbRagFolder.Text = settings.RagFolder;
            TbRagStatus.Text = "Running";
            TbRagStatus.Foreground = Brushes.Green;
        }
    }

    #region Test audio recording

    private AudioService audioService;

    private void BtnStartRecord_OnClick(object sender, RoutedEventArgs e)
    {
        audioService = new AudioService();
        var (deviceName, deviceId) = audioService.GetFirstDevice();
        if (deviceId >= 0)
        {
            Console.WriteLine("Recording: " + deviceName);
            audioService.StartRecord(deviceId);
        }
    }

    private async void BtnStopRecord_OnClick(object sender, RoutedEventArgs e)
    {
        if (audioService != null)
        {
            audioService.StopRecord();
            audioService.Dispose();
            audioService = null;
        }

        string file = $"chunk_0.wav";
        if (File.Exists(file))
        {
            AiNexusService service = new AiNexusService(settings.AiNexusToken);
            var response = await service.PostTranscriptAsync(file);
            if (response != null)
            {
                foreach (var t in response.Transcript)
                {
                    Debug.WriteLine(t.Text);
                }
            }
        }
    }

    #endregion

    //private void LoadGpuStatus()
    //{
    //    int maxVram = 0;
    //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    //    {
    //        List<GpuInfoWindows> gpus = DeviceDetect.GetGpuInfoWindows();
    //        if (gpus.Count > 0)
    //        {
    //            GpuInfoWindows max = gpus.OrderByDescending(x => x.AdapterRAM).First();
    //            TbGpuName.Text = max.Name;
    //            maxVram = (int)(max.AdapterRAM / 1024 / 1024);
    //            TbGpuVram.Text = maxVram + " MB";
    //        }
    //    }
    //    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    //    {
    //        List<GpuInfoLinux> gpus = DeviceDetect.GetGpuInfoLinux();
    //        if (gpus.Count > 0)
    //        {
    //            GpuInfoLinux max = gpus.OrderByDescending(x => x.MemoryMiB).First();
    //            TbGpuName.Text = max.Name;
    //            maxVram = max.MemoryMiB;
    //            TbGpuVram.Text = maxVram + " MB";
    //        }
    //    }

    //    LlmRecommendation recommended = LlmLookup.GetRecommendation(maxVram);
    //    TbRecommend.Text = recommended.MaxModel;
    //    hgRecommendSize = recommended.hugginFaceSize;
    //    if (hgRecommendSize > 0)
    //    {
    //        BtnSearchHg.Content = $"Go seach huggingface: >{recommended.hugginFaceSize}B";
    //    }
    //}

    private void GoSeachHuggingFace(int size)
    {
        // https://huggingface.co/models?pipeline_tag=text-generation&num_parameters=min:0,max:6B&library=transformers&sort=trending
        string url = "https://huggingface.co/models?pipeline_tag=text-generation&library=transformers&sort=trending";
        if (size > 0)
        {
            url = $"https://huggingface.co/models?pipeline_tag=text-generation&num_parameters=min:0,max:{size}B&library=transformers&sort=trending";
        }

        var launcher = TopLevel.GetTopLevel(this).Launcher;
        launcher.LaunchUriAsync(new Uri(url));
    }

    private static void UpdateCliServiceStatus(CliService service, TextBlock textBlock)
    {
        if (service == null)
        {
            textBlock.Text = "Stopped";
            textBlock.Foreground = Brushes.Red;
        }
        else
        {
            textBlock.Text = service.State.ToString();
            switch (service.State)
            {
                case CliServiceState.Running:
                    textBlock.Foreground = Brushes.Green;
                    break;
                case CliServiceState.Starting:
                    textBlock.Foreground = Brushes.GreenYellow;
                    break;
                case CliServiceState.Stopping:
                    textBlock.Foreground = Brushes.Orange;
                    break;
                default:
                    textBlock.Foreground = Brushes.Red;
                    break;
            }
        }
    }

    private void CheckStatusTimer_Tick(object sender, EventArgs e)
    {
        if (!IsVisible)
            return;

        UpdateCliServiceStatus(GlobalService.LlamaService, TbllmStatus);
        UpdateCliServiceStatus(GlobalService.NodeJsService, TbNodeJsStatus);
        UpdateCliServiceStatus(GlobalService.BackendService, TbDotnetBackendStatus);
        UpdateCliServiceStatus(GlobalService.RagBackendService, TbRagBackendStatus);
        UpdateCliServiceStatus(GlobalService.ChromaDbService, TbChromaDbStatus);
    }

    private async void BtnLlmConfig_OnClick(object sender, RoutedEventArgs e)
    {
        LlmConfigWindow window = new LlmConfigWindow();
        await window.ShowDialog(TopLevel.GetTopLevel(this) as Window);
        UpdateCliServiceStatus(GlobalService.LlamaService, TbllmStatus);
    }

    private void BtnRestartNodeJs_OnClick(object sender, RoutedEventArgs e)
    {
        GlobalService.NodeJsService.Stop();
        GlobalService.NodeJsService.Start();
    }

    private void BtnRestartDotNet_OnClick(object sender, RoutedEventArgs e)
    {
        GlobalService.BackendService.Stop();
        GlobalService.BackendService.Start();
    }

    private void Control_OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopMonitorServices();
    }

    private async void BtnResutartRagBackend_OnClick(object sender, RoutedEventArgs e)
    {
        if (GlobalService.RagBackendService == null || GlobalService.RagBackendService.State != CliServiceState.Running)
        {
            // If there is no service, check if chroma DB runtime is not installed
            const string command = "chroma";
            if (!LocalServiceUtils.FindCommand(command))
            {
                var box = MessageBoxManager.GetMessageBoxStandard("RAG Setting", 
                    "You don't have ChromaDB installed yet.\nIn order to use RAG, ChromaDB is required.\nDo you want to install now?",
                    ButtonEnum.YesNo, Icon.Question);
                Window owner = TopLevel.GetTopLevel(this) as Window;
                var messageBoxResult = await box.ShowWindowDialogAsync(owner);
                if (messageBoxResult != ButtonResult.Yes)
                {
                    // Do not restart
                    return;
                }

                // Show install wizard
                LocalCommandWizard wizard = new LocalCommandWizard();
                wizard.GenerateMcpStoreRuntimeViewModel(command);
                await wizard.ShowDialog(owner);
                if (!wizard.IsAllRuntimeInstalled)
                {
                    // Do not restart
                    return;
                }
            }

            GlobalService.ChromaDbService?.Dispose();
            GlobalService.RagBackendService?.Dispose();
            GlobalService.ChromaDbService = ChromaDbService.CreateChromaDbService();
            GlobalService.RagBackendService = RagBackendService.CreateBackendService();
        }
        else
        {
            GlobalService.ChromaDbService.Stop();
            GlobalService.RagBackendService.Stop();
        }

        GlobalService.ChromaDbService.Start();
        GlobalService.RagBackendService.Start();
    }

    private async void BtnManageDocuments_OnClick(object sender, RoutedEventArgs e)
    {
        RagManageWindow manager = new RagManageWindow();
        await manager.ShowDialog(TopLevel.GetTopLevel(this) as Window);
    }
}