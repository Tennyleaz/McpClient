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

internal class EnvEditorViewModel
{
    public ObservableCollection<EnvironmentItem> Env { get; set; } = new();
    public ReactiveCommand<Unit, Unit> AddArgCommand { get; }
    public ReactiveCommand<EnvironmentItem, Unit> RemoveArgCommand { get; }

    public EnvEditorViewModel()
    {
        AddArgCommand = ReactiveCommand.Create(() => Env.Add(new EnvironmentItem()));
        RemoveArgCommand = ReactiveCommand.Create<EnvironmentItem>(DeleteItemAction);
    }

    private void DeleteItemAction(EnvironmentItem item)
    {
        Env.Remove(item);
    }

    public ObservableCollection<KeyValuePair<string, string>> ToCollenction()
    {
        ObservableCollection<KeyValuePair<string, string>> collection = new ObservableCollection<KeyValuePair<string, string>>();
        foreach (EnvironmentItem item in Env)
        {
            if (item.IsValid())
            {
                collection.Add(new KeyValuePair<string, string>(item.Name, item.Value));
            }
        }
        return collection;
    }

    public Dictionary<string, string> ToDictionary()
    {
        Dictionary<string, string> dict = new Dictionary<string, string>();
        foreach (EnvironmentItem item in Env)
        {
            if (item.IsValid())
            {
                if (dict.ContainsKey(item.Name))
                    dict[item.Name] = item.Value;
                else
                    dict.Add(item.Name, item.Value);
            }
        }
        return dict;
    }
}

internal class EnvironmentItem
{
    public string Name { get; set; }
    public string Value { get; set; }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Value);
    }
}