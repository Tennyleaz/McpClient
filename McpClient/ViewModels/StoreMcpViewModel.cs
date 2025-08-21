using McpClient.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;

namespace McpClient.ViewModels;

internal class StoreMcpViewModel : ReactiveObject
{
    public ObservableCollection<StoreMcpServer> Servers { get; set; } = new();

    public StoreMcpViewModel()
    {

    }

    public StoreMcpViewModel(List<StoreMcpServer> servers, List<string> installedNames)
    {
        if (servers != null && installedNames != null && installedNames.Count > 0)
        {
            foreach (StoreMcpServer server in servers)
            {
                if (installedNames.Contains(server.Name))
                {
                    server.IsInstalled = true;
                }
            }
        }

        Servers.AddRange(servers);
    }
}
