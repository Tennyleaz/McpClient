using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using McpClient.Models;

namespace McpClient.ViewModels;

internal class GroupListViewModel : ReactiveObject
{
    public ObservableCollection<Group> Groups { get; set; } = new();

    public ReactiveCommand<string, Unit> RunCommand { get; }

    public event EventHandler<string> RunServer;

    public GroupListViewModel()
    {
        RunCommand = ReactiveCommand.Create<string>(PerformAction);
    }

    private void PerformAction(string serverName)
    {
        //Debug.WriteLine("The action was called: " + serverName);
        RunServer?.Invoke(this, serverName);
    }
}
