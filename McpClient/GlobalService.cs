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
    }

    public static readonly string LlamaInstallFolder;
    public static readonly string LlamaServerBin;

    public static LlamaService LlamaService { get; set; }

    public static MainViewModel MainViewModel { get; set; }

    public static List<string> KnownCommands { get; set; }
}

