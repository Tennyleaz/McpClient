//using DryIoc;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.ViewModels;

public class ArgsEditorViewModel : ReactiveObject
{
    public ObservableCollection<ArgsEditorItem> Args { get; set; } = new();
    public ReactiveCommand<Unit, Unit> AddArgCommand { get; }
    public ReactiveCommand<ArgsEditorItem, Unit> RemoveArgCommand { get; }

    public ArgsEditorViewModel()
    {
        AddArgCommand = ReactiveCommand.Create(() => Args.Add(new ArgsEditorItem("")));
        RemoveArgCommand = ReactiveCommand.Create<ArgsEditorItem>(DeleteItemAction);
    }

    private void DeleteItemAction(ArgsEditorItem item)
    {
        for (int i = 0; i < Args.Count; i++)
        {
            if (Args[i].Id == item.Id)
            {
                Args.RemoveAt(i);
                break;
            }
        }
    }

    public ObservableCollection<string> ToCollenction()
    {
        ObservableCollection<string> collection = new ObservableCollection<string>();
        foreach (ArgsEditorItem item in Args)
        {
            if (string.IsNullOrWhiteSpace(item.Arg))
                continue;
            collection.Add(item.Arg);
        }
        return collection;
    }
}

public class ArgsEditorItem
{
    public string Arg { get; set; }
    public Guid Id { get; set; }

    public ArgsEditorItem(string arg)
    {
        Arg = arg;
        Id = Guid.NewGuid();
    }
}

