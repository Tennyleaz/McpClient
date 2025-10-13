using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using McpClient.Utils;
using McpClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace McpClient.Views;

public partial class LocalCommandWizard : Window
{
    private WizardViewModel viewModel;
    private WizardState state = WizardState.Check;
    private int installIndex = 0;
    private readonly string platform;
    private readonly JsonNode allDepConfig;

    public LocalCommandWizard()
    {
        InitializeComponent();

        viewModel = new WizardViewModel();
        viewModel.DetectedRuntimes = new List<RuntimeItem>()
        {
            new RuntimeItem("nodejs", "✔ installed"),
            new RuntimeItem("npx", "✔ installed"),
            new RuntimeItem("python3", "❌ missing"),
            new RuntimeItem("uvx", "❌ missing"),
        };

        // update installed status
        CheckInstalled();

        DataContext = viewModel;

        if (Design.IsDesignMode)
            return;

        string fileName = "packagemanager.json";
        string text = System.IO.File.ReadAllText(fileName);
        allDepConfig = JsonNode.Parse(text);
        platform = DetectPlatform();
        ShowNextStep();
    }

    private void CheckInstalled()
    {
        viewModel.MissingRuntimes.Clear();
        foreach (RuntimeItem runtime in viewModel.DetectedRuntimes)
        {
            if (LocalServiceUtils.FindCommand(runtime.Name))
            {
                runtime.Status = "✔ installed";
                runtime.IsCommandExist = true;
            }
            else
            {
                runtime.Status = "❌ missing";
                runtime.IsCommandExist = false;
                viewModel.MissingRuntimes.Add(runtime);
            }
        }
    }

    private async void BtnNext_OnClick(object sender, RoutedEventArgs e)
    {
        if (state == WizardState.Check)
        {
            // this is the initial state
            state = WizardState.PreInstall;
            // go to install the first item
            ShowNextStep();
        }
        else if (state == WizardState.PreInstall)
        {
            state = WizardState.Install;
            ShowNextStep();
            bool autoNext = await ExecuteInstallCommand();
            BtnNext.IsEnabled = true;
            if (autoNext)
            {
                BtnNext_OnClick(null, null);
            }
        }
        else if (state == WizardState.Install)
        {
            // go to next item
            installIndex++;
            if (installIndex >= viewModel.MissingRuntimes.Count)
            {
                // go to verify if install the last item
                state = WizardState.Verify;
            }
            else
            {
                // go to next item's preinstall step
                state = WizardState.PreInstall;
            }
            ShowNextStep();
        }
        else if (state == WizardState.Verify)
        {
            // if verify success, close this window
            Close();
        }
    }

    private void ShowNextStep()
    {
        if (state == WizardState.Check)
        {
            // this is the initial state, do nothing
            PanelMissingInfo.IsVisible = true;
            PanelPreInstall.IsVisible = false;
            PanelInstall.IsVisible = false;
            PanelVerify.IsVisible = false;
        }
        else if (state == WizardState.PreInstall)
        {
            // Change title
            TbWinTitle.Text = $"Install MCP Runtimes ({installIndex + 1} of {viewModel.MissingRuntimes.Count})";

            // show install methods, and let user select
            PanelMissingInfo.IsVisible = false;
            PanelPreInstall.IsVisible = true;
            PanelInstall.IsVisible = false;
            PanelVerify.IsVisible = false;

            RuntimeItem currentItem = viewModel.MissingRuntimes[installIndex];
            TbPreInstallHeader.Text = $"Install: {currentItem.Name}";
            TbDownloadUrl.Text = GetDownloadUrl(currentItem.Name);
            TbPackageScript.Text = GetInstructionsCommand(currentItem.Name);
        }
        else if (state == WizardState.Install)
        {
            // install via package manager and show progress
            PanelMissingInfo.IsVisible = false;
            PanelPreInstall.IsVisible = false;
            PanelInstall.IsVisible = true;
            PanelVerify.IsVisible = false;

            RuntimeItem currentItem = viewModel.MissingRuntimes[installIndex];
            TbInstallHeader.Text = $"Installing: {currentItem.Name}";
            if (RadioDownload.IsChecked == true)
            {
                IbInstallProgress.Text = $"Please click \"next\" after you download and install {currentItem.Name}.";
            }
            else
            {
                IbInstallProgress.Text = "Please wait...";
                BtnNext.IsEnabled = false;
            }
            // go to next step automatically after install success
        }
        else if (state == WizardState.Verify)
        {
            // Change title
            TbWinTitle.Text = "Install MCP Runtimes";

            // if verify success, close this window
            PanelMissingInfo.IsVisible = false;
            PanelPreInstall.IsVisible = false;
            PanelInstall.IsVisible = false;
            PanelVerify.IsVisible = true;

            BtnClose.IsVisible = true;
            BtnNext.IsVisible = false;

            CheckInstalled();
            List<string> successes = new List<string>();
            List<string> fails = new List<string>();
            foreach (RuntimeItem runtime in viewModel.MissingRuntimes)
            {
                if (runtime.IsCommandExist)
                    successes.Add(runtime.Name);
                else
                    fails.Add(runtime.Name);
            }
            if (fails.Count == 0)
            {
                VerifyResult.Text = "All required runtimes are now installed.";
            }
            else
            {
                VerifyResult.Text = $"These runtime faied to install:\n\n{string.Join(", ", fails)}\n\nPlease install them manually.";
            }
        }
    }

    /// <summary>
    /// Return true if install via package manager finish, can auto go next.
    /// </summary>
    /// <returns></returns>
    private async Task<bool> ExecuteInstallCommand()
    {
        RuntimeItem currentItem = viewModel.MissingRuntimes[installIndex];
        // do the selected command, or goto download URL
        if (RadioInstallPacakge.IsChecked == true)
        {
            currentItem.IsManualDownload = false;
            // run the command via admin
            InstallDependency(currentItem.Name);
            return true;
        }
        else
        {
            currentItem.IsManualDownload = true;
            // goto download URL
            if (!string.IsNullOrEmpty(TbDownloadUrl.Text))
            {
                Uri uri = new Uri(TbDownloadUrl.Text);
                await Launcher.LaunchUriAsync(uri);
            }
        }
        return false;
    }

    private string GetDownloadUrl(string depName)
    {
        JsonNode depConfig = LoadDependencyConfig(depName);
        return depConfig["url"]?.ToString();
    }

    private List<Instruction> GetInstructions(string depName)
    {
        JsonNode depConfig = LoadDependencyConfig(depName);
        JsonNode instructionsJson = depConfig[platform];
        List<Instruction> instructions = instructionsJson.Deserialize<List<Instruction>>();
        return instructions;
    }

    private string GetInstructionsCommand(string depName)
    {
        List<Instruction> instructions = GetInstructions(depName);

        string command = string.Empty;
        foreach (Instruction step in instructions)
        {
            PackageManager manager = PackageManagerFactory.Create(step.Manager);
            if (!manager.IsAvailable())
                continue;

            //if (!manager.IsPackageInstalled(step.Package))
            if (!LocalServiceUtils.FindCommand(step.Package))
            {
                string cmd = manager.InstallCommand(step.Package);
                command += cmd + "\n";
            }
        }

        return command;
    }

    private void InstallDependency(string depName)
    {
        List<Instruction> instructions = GetInstructions(depName);

        foreach (Instruction step in instructions)
        {
            PackageManager manager = PackageManagerFactory.Create(step.Manager);
            if (!manager.IsAvailable()) 
                continue;

            if (!manager.IsPackageInstalled(step.Package))
            {
                string cmd = manager.InstallCommand(step.Package);
                var result = ShellHelper.RunShellCommand(cmd);
            }
        }
    }

    private JsonNode LoadDependencyConfig(string name)
    {
        return allDepConfig[name];
    }

    private static string DetectPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macos";
        // check via release name and package manager
        return PackageManagerFactory.CheckLinuxVersion();
    }

    private void BtnPrev_OnClick(object sender, RoutedEventArgs e)
    {
    }

    private void BtnClose_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_OnClosing(object sender, WindowClosingEventArgs e)
    {
        if (state != WizardState.Verify)
        {
            e.Cancel = true;
            return;
        }
    }
}

internal enum WizardState
{
    Check,
    PreInstall,
    Install,
    Verify
}