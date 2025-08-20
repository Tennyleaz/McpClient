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

    public ReactiveCommand<Group, Unit> RunCommand { get; }
    public ReactiveCommand<Group, Unit> DownloadCommand { get; }

    public event EventHandler<Group> RunServer, Download;

    public GroupListViewModel()
    {
        RunCommand = ReactiveCommand.Create<Group>(PerformAction);
        DownloadCommand = ReactiveCommand.Create<Group>(OnDownload);
    }

    private void PerformAction(Group group)
    {
        //Debug.WriteLine("The action was called: " + serverName);
        RunServer?.Invoke(this, group);
    }

    private void OnDownload(Group group)
    {
        Download?.Invoke(this, group);
    }
}
