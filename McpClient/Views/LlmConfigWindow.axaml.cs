using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using McpClient.Models;
using McpClient.Services;
using McpClient.Utils;
using McpClient.ViewModels;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using AvaloniaEdit.Utils;

namespace McpClient.Views;

public partial class LlmConfigWindow : Window
{
    private List<GpuInfoViewModel> gpuList = new ();
    private ObservableCollection<KnownGguf> modelList = new();
    private Settings settings;
    private ModelSettings modelSettings;
    private int hgRecommendSize;
    private DispatcherTimer checkServerTimer;
    private int maxContextLength = 0, currentContextLength = 4096;

    public LlmConfigWindow()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            return;
        ClearLlmData();
        IsEnabled = false;
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;

        settings = SettingsManager.Local.Load();
        RadioExternal.IsChecked = settings.IsUseRemoteLlm;
        RadioLocal.IsChecked = !settings.IsUseRemoteLlm;
        DetectLlamaBinary();
        DetectDevice();
        IsEnabled = true;
    }

    private void DetectLlamaBinary()
    {
        // Check "llama-server"
        string dir = GlobalService.LlamaInstallFolder;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        
        if (!File.Exists(GlobalService.LlamaServerBin))
        {
            TbLlamaInstallHint.Text = "You don't have llama.cpp backend installed yet. Put files in:\n" + dir;
            LlamaDetectGrid.IsVisible = true;
            LocalGrid.IsVisible = ModelConfigGrid.IsVisible = false;
            return;
        }
        else
        {
            LbLlamaPath.Content = GlobalService.LlamaServerBin;
            LlamaDetectGrid.IsVisible = false;
            LocalGrid.IsVisible = ModelConfigGrid.IsVisible = true;
        }

        // Check model file
        CbModel.ItemsSource = modelList;
        modelSettings = SettingsManager.Local.LoadModelSettings();
        if (modelSettings.KnownGgufs.Count > 0)
        {
            KnownGguf lastSelectedModel;

            // Copy list to viewmodel
            modelList.AddRange(modelSettings.KnownGgufs);

            if (modelSettings.KnownGgufs.Count > modelSettings.LastSelectedModel)
            {
                lastSelectedModel = modelSettings.KnownGgufs[modelSettings.LastSelectedModel];
                CbModel.SelectedIndex = modelSettings.LastSelectedModel;
            }
            else
            {
                lastSelectedModel = modelSettings.KnownGgufs.First();
                CbModel.SelectedIndex = 0;
            }

            // Check the service
            StartCheckServerStatus();
            ModelConfigGridInner.IsVisible = true;
            LbNoModelSelected.IsVisible = false;
        }
        else
        {
            // Disable start section if no model could be used
            ModelConfigGridInner.IsVisible = false;
            LbNoModelSelected.IsVisible = true;
        }
    }

    private void DetectDevice()
    {
        gpuList.Clear();
        int selectedIndex = -1;
        long maxVram = 0;
        /*if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            List<GpuInfoWindows> gpus = DeviceDetect.GetGpuInfoWindows();
            for (int i = 0; i < gpus.Count; i++)
            {
                long vram = gpus[i].AdapterRAM ?? 0;
                gpuList.Add(new GpuInfoViewModel(i, gpus[i].Name, (int)(vram / 1024 / 1024)));
                // Selecte the max vram
                if (gpus[i].AdapterRAM > maxVram)
                {
                    maxVram = vram;
                    selectedIndex = i;
                }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            List<GpuInfoLinux> gpus = DeviceDetect.GetGpuInfoLinux();
            for (int i = 0; i < gpus.Count; i++)
            {
                gpuList.Add(new GpuInfoViewModel(i, gpus[i].Name, (int)(gpus[i].MemoryMiB / 1024)));
                // Selecte the max vram
                if (gpus[i].MemoryMiB > maxVram)
                {
                    maxVram = gpus[i].MemoryMiB;
                    selectedIndex = i;
                }
            }
        }*/

        var llamaDevices = LlamaDeviceUtils.ListLlamaCppDevices(GlobalService.LlamaServerBin);
        for (int i = 0; i < llamaDevices.Count; i++)
        {
            int vramMb = llamaDevices[i].TotalMemoryMiB ?? 0;
            gpuList.Add(new GpuInfoViewModel(llamaDevices[i].Index, llamaDevices[i].Name, vramMb, llamaDevices[i].Backend));

            if (vramMb > maxVram)
            {
                maxVram = vramMb;
                selectedIndex = i;
            }
        }

        CbDevice.ItemsSource = gpuList;
        CbDevice.SelectedIndex = selectedIndex;
    }

    private void RadioLocal_OnIsCheckedChanged(object sender, RoutedEventArgs e)
    {
        if (RadioLocal.IsChecked == true)
        {
            LocalGrid.IsEnabled = ModelConfigGrid.IsEnabled = true;
            ExternalPanel.IsEnabled = false;
        }
    }

    private void RadioExternal_OnIsCheckedChanged(object sender, RoutedEventArgs e)
    {
        if (RadioExternal.IsChecked == true)
        {
            LocalGrid.IsEnabled = ModelConfigGrid.IsEnabled = false;
            ExternalPanel.IsEnabled = true;

            // Stop any running local llama
            if (GlobalService.LlamaService != null)
            {
                GlobalService.LlamaService.Stop();
                GlobalService.LlamaService.Dispose();
                GlobalService.LlamaService = null;
            }
        }
    }

    private async void BtnOpenLlamaDir_OnClick(object sender, RoutedEventArgs e)
    {
        string dir = GlobalService.LlamaInstallFolder;
        var folder = await StorageProvider.TryGetFolderFromPathAsync(new Uri(dir));
        await Launcher.LaunchFileAsync(folder);
    }

    private void BtnDetectLlama_OnClick(object sender, RoutedEventArgs e)
    {
        DetectLlamaBinary();
    }

    private async void BtnDownloadLlama_OnClick(object sender, RoutedEventArgs e)
    {
        const string url = "https://github.com/ggml-org/llama.cpp/releases";
        await Launcher.LaunchUriAsync(new Uri(url));
    }

    private void BtnSearchHg_OnClick(object sender, RoutedEventArgs e)
    {
        GoSeachHuggingFace(hgRecommendSize);
    }

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

    private void CbDevice_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CbDevice.SelectedItem is GpuInfoViewModel vm)
        {
            LlmRecommendation recommended = LlmLookup.GetRecommendation(vm.VramMb);
            TbRecommend.Text = recommended.MaxModel;
            hgRecommendSize = recommended.hugginFaceSize;
            if (hgRecommendSize > 0)
            {
                BtnSearchHg.Content = $"Go seach huggingface: >{recommended.hugginFaceSize}B";
            }
        }
    }

    private async void BtnSelectModel_OnClick(object sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            FileTypeFilter = new List<FilePickerFileType> { new FilePickerFileType("GGUF model")
            {
                MimeTypes = ["application/gguf"],
                Patterns = ["*.gguf"]
            } },
            AllowMultiple = false,
            Title = "Select GGUF model"
        });

        if (files.Count == 0)
            return;

        string file = files.First().TryGetLocalPath();
        if (!File.Exists(file))
            return;

        if (settings.LlmModelFile != file)
        {
            KnownGguf newGGUF = LoadModelMetadata(file);
            if (modelSettings.KnownGgufs.All(x => x.GgufFilePath != file))
            {
                // Only save new model if not the same file
                modelList.Add(newGGUF);
                modelSettings.LastSelectedModel = CbModel.SelectedIndex = modelList.Count - 1;
                modelSettings.KnownGgufs.Add(newGGUF);
                SettingsManager.Local.SaveModelSettings(modelSettings);
            }

            // Save the model to setting
            settings.LlmModelFile = file;
            await SettingsManager.Local.SaveAsync(settings);

            // Enable start section
            ModelConfigGridInner.IsVisible = true;
            LbNoModelSelected.IsVisible = false;

            // Load the model now
            BtnStartLlama_OnClick(null, null);
        }
    }

    private KnownGguf LoadModelMetadata(string modelFile)
    {
        GGUFReader reader = new GGUFReader(modelFile);

        KnownGguf newGguf = new KnownGguf();
        newGguf.GgufFilePath = modelFile;

        foreach (var metadata in reader.Header.Metadata)
        {
            if (metadata.Key == "general.name")
            {
                newGguf.Name = metadata.Value.ToString();
            }
            else if (metadata.Key == "general.size_label")
            {
                newGguf.Size = metadata.Value.ToString();
            }
            else if (metadata.Key == "general.architecture")
            {
                newGguf.Architecture = metadata.Value.ToString();
            }
            else if (metadata.Key.EndsWith(".context_length"))
            {
                string len = metadata.Value.ToString();
                int.TryParse(len, out maxContextLength);
                newGguf.MaxContextLength = maxContextLength;
                TbContextLenghtHeader.Content = $"Context Length: (max {len})";
            }
            else if (metadata.Key == "general.file_type")
            {
                if (int.TryParse(metadata.Value.ToString(), out int iValue))
                {
                    newGguf.FileType = (LlamaFileType)iValue;
                }
            }
        }
        return newGguf;
    }

    private void ClearLlmData()
    {
        LbLlamaPath.Content = "---";
    }

    private void BtnApplyUrl_OnClick(object sender, RoutedEventArgs e)
    {
    }

    private void BtnStartLlama_OnClick(object sender, RoutedEventArgs e)
    {
        if (CbDevice.SelectedIndex < 0 || CbModel.SelectedIndex < 0)
        {
            return;
        }

        bool isOffload = ToggleOffloadKvCache.IsChecked == true;
        if (!int.TryParse(TbContextLenght.Text, out int contentSize))
            contentSize = 4096;

        if (GlobalService.LlamaService == null)
        {
            // Create new service
        }
        else
        {
            // Restart service
            GlobalService.LlamaService.Stop();
            GlobalService.LlamaService.Dispose();
        }

        KnownGguf selected = modelList[CbModel.SelectedIndex];
        GlobalService.LlamaService = LlamaService.CreateLlamaService(selected.GgufFilePath, selected.Architecture, CbDevice.SelectedIndex,
            isOffload, contentSize);
        GlobalService.LlamaService.Start();

        StartCheckServerStatus();
    }

    private void StartCheckServerStatus()
    {
        if (checkServerTimer != null)
            return;

        checkServerTimer = new DispatcherTimer();
        checkServerTimer.Interval = TimeSpan.FromSeconds(1);
        checkServerTimer.Tick += CheckServerTimer_Tick;
        checkServerTimer.Start();
    }

    private void CheckServerTimer_Tick(object sender, EventArgs e)
    {
        if (GlobalService.LlamaService == null)
        {
            LbServiceStatus.Content = "Stopped";
            LbServiceStatus.Foreground = Brushes.Red;
            LbLocalServerUrl.Content = "---";
        }
        else
        {
            LbServiceStatus.Content = GlobalService.LlamaService.State;
            switch (GlobalService.LlamaService.State)
            {
                case CliServiceState.Running:
                    LbServiceStatus.Foreground = Brushes.Green;
                    LbLocalServerUrl.Content = GlobalService.LlamaService.Address;
                    break;
                case CliServiceState.Starting:
                    LbServiceStatus.Foreground = Brushes.GreenYellow;
                    LbLocalServerUrl.Content = "---";
                    break;
                case CliServiceState.Stopping:
                    LbServiceStatus.Foreground = Brushes.Orange;
                    LbLocalServerUrl.Content = "---";
                    break;
                default:
                    LbServiceStatus.Foreground = Brushes.Red;
                    LbLocalServerUrl.Content = "---";
                    break;
            }
        }
    }

    private void StopCheckServerStatus()
    {
        checkServerTimer?.Stop();
        checkServerTimer = null;
    }

    private void Window_OnClosing(object sender, WindowClosingEventArgs e)
    {
        StopCheckServerStatus();
    }

    private void BtnStopLlama_OnClick(object sender, RoutedEventArgs e)
    {
        GlobalService.LlamaService?.Stop();
        GlobalService.LlamaService?.Dispose();
    }

    private void TbContextLenght_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (maxContextLength <= 0)
        {
            return;
        }

        if (int.TryParse(TbContextLenght.Text, out int length))
        {
            if (length > 0 && length <= maxContextLength)
            {
                currentContextLength = length;
                return;
            }
        }

        TbContextLenght.TextChanged -= TbContextLenght_OnTextChanged;
        TbContextLenght.Text = currentContextLength.ToString();
        TbContextLenght.TextChanged += TbContextLenght_OnTextChanged;
    }

    private void CbModel_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CbModel == null)
            return;

        // Save the selected index
        if (CbModel.SelectedIndex != modelSettings.LastSelectedModel)
        {
            modelSettings.LastSelectedModel = CbModel.SelectedIndex;
            SettingsManager.Local.SaveModelSettings(modelSettings);
        }
    }
}