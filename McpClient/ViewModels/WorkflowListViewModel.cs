using McpClient.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.ViewModels;

internal class WorkflowListViewModel
{
    public ObservableCollection<OfflineWorkflow> OfflineWorkflows { get; set; } = new();

    public ReactiveCommand<OfflineWorkflow, Unit> RunCommand { get; }

    public ReactiveCommand<OfflineWorkflow, Unit> DeleteCommand { get; }

    public event EventHandler<OfflineWorkflow> RunServer, Delete;

    public WorkflowListViewModel()
    {
        RunCommand = ReactiveCommand.Create<OfflineWorkflow>(PerformAction);
        DeleteCommand = ReactiveCommand.Create<OfflineWorkflow>(PerformDeleteAction);
    }

    private void PerformAction(OfflineWorkflow group)
    {
        RunServer?.Invoke(this, group);
    }

    private void PerformDeleteAction(OfflineWorkflow group)
    {
        Delete?.Invoke(this, group);
    }
}
