using Avalonia.Controls;
using McpClient.Models;
using McpClient.Views;
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

internal class McpViewModel : ReactiveObject
{
    public McpViewModel(McpServer model, bool isActive)
    {
        _enabled = model.enabled;
        _active = isActive;
        _type = model.type;
        _serverName = model.server_name;
        _owner = model.owner;
        _sseUrl = model.sse_url;
        _streamableHttpUrl = model.streamable_http_url;
        _command = model.command;
        _args = new ObservableCollection<string>(model.args);
        _env = new ObservableCollection<KeyValuePair<string, string>>();
        foreach (var kv in model.env)
        {
            _env.Add(new KeyValuePair<string, string>(kv.Key, kv.Value));
        }
    }

    private bool _enabled;
    private bool _active;
    private string _type;
    private string _serverName;
    private string _owner;
    private string _sseUrl;
    private string _streamableHttpUrl;
    private string _command;
    private ObservableCollection<string> _args;
    private ObservableCollection<KeyValuePair<string, string>> _env;
    private bool _isBusy;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _enabled, value);
        }
    }

    //public string Type
    //{
    //    get => _type;
    //    set
    //    {
    //        this.RaiseAndSetIfChanged(ref _type, value);
    //        this.RaisePropertyChanged(nameof(ByType));
    //    }
    //}

    public string ServerName
    {
        get => _serverName;
        set
        {
            this.RaiseAndSetIfChanged(ref _serverName, value);
        }
    }

    //public string Owner
    //{
    //    get => _owner;
    //    set
    //    {
    //        this.RaiseAndSetIfChanged(ref _owner, value);
    //        this.RaisePropertyChanged(nameof(ByOwner));
    //    }
    //}

    public bool Active => _active;

    public string ByOwner => "By: " + _owner;

    public bool HasOwner => !string.IsNullOrEmpty(_owner);

    public string ByType => "Type: " + _type;

    public string DisplayArgs
    {
        get
        {
            if (_args == null || _args.Count == 0)
                return "(empty)";
            return string.Join(" ", _args);
        }
    }

    public string DisplayEnv
    {
        get
        {
            if (_env == null || _env.Count == 0)
                return "(empty)";
            return string.Join(" ", _env);
        }
    }

    public bool IsShowCommand => _type == "stdio";

    public bool IsShowSseUrl => _type == "sse";

    public bool IsShowStreamablUrl => _type == "streamableHttp";

    public string SseUrl
    {
        get => _sseUrl;
        set
        {
            this.RaiseAndSetIfChanged(ref _sseUrl, value);
        }
    }

    public string StreamableHttpUrl
    {
        get => _streamableHttpUrl;
        set
        {
            this.RaiseAndSetIfChanged(ref _streamableHttpUrl, value);
        }
    }

    public string Command
    {
        get => _command;
        set
        {
            this.RaiseAndSetIfChanged(ref _command, value);
        }
    }

    public ObservableCollection<string> Args
    {
        get => _args;
        set
        {
            this.RaiseAndSetIfChanged(ref _args, value);
            this.RaisePropertyChanged(nameof(DisplayArgs));
        }
    }

    // To enable easy editing, use collection of key/value pairs.
    public ObservableCollection<KeyValuePair<string, string>> Env
    {
        get => _env;
        set
        {
            this.RaiseAndSetIfChanged(ref _env, value);
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            this.RaiseAndSetIfChanged(ref _isBusy, value);
        }
    }

    public McpServer ToModel()
    {
        return new McpServer
        {
            enabled = Enabled,
            type = _type,
            server_name = ServerName,
            owner = _owner,
            sse_url = SseUrl,
            streamable_http_url = StreamableHttpUrl,
            command = Command,
            args = new List<string>(Args),
            env = new Dictionary<string, string>(Env)
        };
    }
}

