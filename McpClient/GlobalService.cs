using McpClient.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using McpClient.ViewModels;

namespace McpClient;

internal static class GlobalService
{
    static GlobalService()
    {
        LlamaInstallFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "McpClient", "Llama");
        string name = "llama-server";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            name += ".exe";
        LlamaServerBin = Path.Combine(LlamaInstallFolder, name);

        McpHostFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "McpNodeJs");

        DispatcherFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "McpBackend");

        ChatFrontendFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "dist");
    }

    public static readonly string LlamaInstallFolder;
    public static readonly string LlamaServerBin;

    public static readonly string McpHostFolder;

    public static readonly string DispatcherFolder;

    public static readonly string ChatFrontendFolder;

    public static LlamaService LlamaService { get; set; }

    public static McpNodeJsService NodeJsService { get; set; }

    public static DispatcherBackendService BackendService { get; set; }

    public static MainViewModel MainViewModel { get; set; }

    public static List<string> KnownCommands { get; set; }
}

