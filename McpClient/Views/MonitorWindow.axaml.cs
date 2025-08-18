using System;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace McpClient.Views;

public partial class MonitorWindow : Window
{
    private BackgroundWorker worker;
    private readonly PlatformID _platform;

    public MonitorWindow()
    {
        InitializeComponent();
        _platform = Environment.OSVersion.Platform;
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        worker = new BackgroundWorker();
        worker.WorkerSupportsCancellation = true;
        worker.WorkerReportsProgress = true;
        worker.DoWork += Worker_DoWork;
        worker.ProgressChanged += Worker_ProgressChanged;
        worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        worker.RunWorkerAsync();
    }

    private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        
    }

    private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {

    }

    private void Worker_DoWork(object sender, DoWorkEventArgs e)
    {
        while (!worker.CancellationPending)
        {
            int cpuUsage = 0;
            if (_platform == PlatformID.Win32NT)
            {
                cpuUsage = GetCpuUsageLinux();
            }
            else if (_platform == PlatformID.Unix)
            {

            }

            worker.ReportProgress(cpuUsage);
        }
    }

    private void Window_OnClosing(object sender, WindowClosingEventArgs e)
    {
        worker.CancelAsync();
    }

    private static int GetCpuUsageLinux()
    {
        const string command = @"top -bn2 | grep ""Cpu(s)"" | sed ""s/.*, *\([0-9.]*\)%* id.*/\1/""";
        string idleText = RunCommandLinux(command);
        if (int.TryParse(idleText, out int idlePercentage))
        {
            return 100 - idlePercentage;
        }

        return 0;
    }

    private static string RunCommandLinux(string args)
    {
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = "/bin/bash";
        psi.Arguments = args;
        psi.UseShellExecute = true;

        Process p = new Process();
        p.StartInfo = psi;
        p.Start();
        p.WaitForExit();
        return p.StandardOutput.ReadToEnd();
    }
}