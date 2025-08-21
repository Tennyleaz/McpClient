using McpClient.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.ViewModels;

internal class StoreMcpViewModel : ReactiveObject
{
    public ObservableCollection<StoreMcpServer> Servers { get; set; } = new();
}
