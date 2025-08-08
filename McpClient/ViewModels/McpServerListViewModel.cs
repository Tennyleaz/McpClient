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

    public event EventHandler<string> RunServer;

    public McpServerListViewModel()
    {
        RunCommand = ReactiveCommand.Create<string>(PerformAction);
    }

    private void PerformAction(string serverName)
    {
        //Debug.WriteLine("The action was called: " + serverName);
        RunServer?.Invoke(this, serverName);
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