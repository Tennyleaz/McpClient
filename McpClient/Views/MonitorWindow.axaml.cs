using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LibreHardwareMonitor.Hardware;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using LiveChartsCore.SkiaSharpView;
using McpClient.ViewModels;

namespace McpClient.Views;

public partial class MonitorWindow : Window
{
    private BackgroundWorker worker;
    private MonitorViewModel viewModel = new MonitorViewModel();
    private readonly PlatformID _platform;
    private const int INTERVAL_MS = 1500;

    public MonitorWindow()
    {
        InitializeComponent();
        _platform = Environment.OSVersion.Platform;
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
        {
            return;
        }

        viewModel.Series.Add(new ColumnSeries<int>
        {
            //Name = "CPU",
            Values = [0, 0],
        });
        //viewModel.Series.Add(new ColumnSeries<int>
        //{
        //    Name = "GPU",
        //    Values = [0]
        //});
        DataContext = viewModel;

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
        HardwareType type = (HardwareType)e.ProgressPercentage;
        if (type == HardwareType.Cpu)
        {

        }
        else if (type == HardwareType.GpuNvidia || type == HardwareType.GpuAmd)
        {

        }
    }

    private void Worker_DoWork(object sender, DoWorkEventArgs e)
    {
        Computer computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = false,
            IsMotherboardEnabled = false,
            IsControllerEnabled = false,
            IsNetworkEnabled = false,
            IsStorageEnabled = false,
            IsBatteryEnabled = false,
            IsPsuEnabled = false,
        };
        computer.Open();

        // detect GPU number first
        computer.Accept(new UpdateVisitor());
        bool isGpuDetectd = false;
        foreach (IHardware hardware in computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.GpuAmd || hardware.HardwareType == HardwareType.GpuIntel || hardware.HardwareType == HardwareType.GpuNvidia)
            {
                isGpuDetectd = true;
            }
        }
        if (!isGpuDetectd)
        {
            viewModel.XAxes[0].Labels[1] = "GPU X";
        }

        while (!worker.CancellationPending)
        {
            computer.Accept(new UpdateVisitor());
            int cpu = 0, gpu = 0;
            foreach (IHardware hardware in computer.Hardware)
            {
                foreach (ISensor sensor in hardware.Sensors)
                {
                    if (hardware.HardwareType == HardwareType.Cpu)
                    {
                        if (sensor.Name == "CPU Total" && sensor.Value.HasValue)
                        {
                            cpu = (int)sensor.Value;
                            break;
                        }
                    }
                    else if (hardware.HardwareType == HardwareType.GpuAmd || /*hardware.HardwareType == HardwareType.GpuIntel ||*/ hardware.HardwareType == HardwareType.GpuNvidia)
                    {
                        if (sensor.Name == "D3D 3D" && sensor.Value.HasValue)
                        {
                            gpu = (int)sensor.Value;
                            break;
                        }
                    }
                }
            }

            viewModel.Series[0].Values = [cpu, gpu];
            Thread.Sleep(INTERVAL_MS);
        }

        computer.Close();
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

    private class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            //foreach (IHardware subHardware in hardware.SubHardware)
            //{
            //    subHardware.Accept(this);
            //}
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}