using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Dialogs;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using McpClient.Services;
using McpClient.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace McpClient.Views;

public partial class SystemSetting : UserControl
{
    private Settings settings;
    private int hgRecommendSize = 0;

    public SystemSetting()
    {
        InitializeComponent();
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;

        //LoadGpuStatus();
    }

    internal void LoadFromSettings()
    {
        settings = SettingsManager.Local.Load();
        TbRagFolder.Text = settings.RagFolder;
        TbFileSystemFolder.Text = settings.FileSystemFolder;
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

        // llama service status
        UpdateLlmStatus();
        UpdateMcpNodeJsStatus();
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

    private void BtnSearchHg_OnClick(object sender, RoutedEventArgs e)
    {
        GoSeachHuggingFace(hgRecommendSize);
    }

    private void UpdateLlmStatus()
    {
        if (GlobalService.LlamaService?.State == CliServiceState.Running)
        {
            TbllmStatus.Text = "Running";
            TbllmStatus.Foreground = Brushes.Green;
        }
        else
        {
            TbllmStatus.Text = "Stopped";
            TbllmStatus.Foreground = Brushes.Red;
        }
    }

    private void UpdateMcpNodeJsStatus()
    {
        if (GlobalService.LlamaService?.State == CliServiceState.Running)
        {
            TbllmStatus.Text = "Running";
            TbllmStatus.Foreground = Brushes.Green;
        }
        else
        {
            TbllmStatus.Text = "Stopped";
            TbllmStatus.Foreground = Brushes.Red;
        }
    }

    private async void BtnLlmConfig_OnClick(object sender, RoutedEventArgs e)
    {
        LlmConfigWindow window = new LlmConfigWindow();
        await window.ShowDialog(TopLevel.GetTopLevel(this) as Window);
        UpdateLlmStatus();
    }

    private async void BtnChangeFileSystem_OnClick(object sender, RoutedEventArgs e)
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
            settings.FileSystemFolder = folder;
            await SettingsManager.Local.SaveAsync(settings);

            TbFileSystemFolder.Text = settings.RagFolder;
        }
    }
}