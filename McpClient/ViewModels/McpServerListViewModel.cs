using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.ViewModels;

internal class McpServerListViewModel : ReactiveObject
{
    public ObservableCollection<string> ServerNames { get; set; } = new();

    public ReactiveCommand<string, Unit> RunCommand { get; }

    public ReactiveCommand<string, Unit> RestartCommand { get; }

    public event EventHandler<string> RunServer, Restart;

    public McpServerListViewModel()
    {
        RunCommand = ReactiveCommand.Create<string>(PerformAction);
        RestartCommand = ReactiveCommand.Create<string>(RestartAction);
    }

    private void PerformAction(string serverName)
    {
        //Debug.WriteLine("The action was called: " + serverName);
        RunServer?.Invoke(this, serverName);
    }

    private void RestartAction(string serverName)
    {
        Restart?.Invoke(this, serverName);
    }
}

//internal class McpServeItem
//{
//    private string _serverName;

//    public string ServerName
//    {
//        get => _serverName
//        set
//        {

//        }
//    }
//}