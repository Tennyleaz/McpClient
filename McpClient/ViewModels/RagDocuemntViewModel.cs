using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace McpClient.ViewModels;

internal class RagDocuemntViewModel : ReactiveObject
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedTime { get; set; }

    public RagDocuemntViewModel()
    {

    }
}
