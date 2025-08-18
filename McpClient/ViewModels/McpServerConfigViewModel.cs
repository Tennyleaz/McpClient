using McpClient.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.ViewModels;

internal class McpServerConfigViewModel: ReactiveObject
{
    public ObservableCollection<McpViewModel> McpServers { get; } = new();

    public ReactiveCommand<McpViewModel, Unit> EditArgsCommand { get; }
    //public Interaction<McpViewModel, List<string>?> EditArgsInteraction { get; } = new();
    public ReactiveCommand<McpViewModel, Unit> RestartCommand { get; }

    public event EventHandler<McpViewModel> Restart;

    public McpServerConfigViewModel()
    {
        //EditArgsCommand = ReactiveCommand.CreateFromTask<McpViewModel>(EditArgsForServer);
        EditArgsCommand = ReactiveCommand.Create((McpViewModel vm) =>
        {
            Console.WriteLine("edit!");
        });
        RestartCommand = ReactiveCommand.Create<McpViewModel>(RestartAction);
    }

    private void RestartAction(McpViewModel vm)
    {
        Restart?.Invoke(this, vm);
    }

    // This method is called by any child when 'edit args' is required
    //public async Task EditArgsForServer(McpViewModel server)
    //{
    //    var result = await EditArgsInteraction.Handle(server);
    //    if (result != null)
    //    {
    //        server.Args.Clear();
    //        foreach (var arg in result)
    //            server.Args.Add(arg);
    //    }
    //}

    public McpServerConfigViewModel(McpServerConfig model, List<McpServerItem> activeItems)
    {
        foreach (var server in model.mcp_servers)
        {
            bool isActive = activeItems.Exists(x => x.server_name == server.server_name);
            McpServers.Add(new McpViewModel(server, isActive));
        }
    }

    public McpServerConfig ToModel()
    {
        return new McpServerConfig
        {
            mcp_servers = McpServers.Select(vm => vm.ToModel()).ToList()
        };
    }
}
