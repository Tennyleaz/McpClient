using McpClient.Services;
using McpClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace McpClient;

internal static class GlobalService
{
    static GlobalService()
    {
        string settingFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "McpClient");
        if (!Directory.Exists(settingFolder))
        {
            try
            {
                Directory.CreateDirectory(settingFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to create setting folder: "+ ex);
            }
        }

        LlamaInstallFolder = Path.Combine(settingFolder, "Llama");
        string name = "llama-server";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            name += ".exe";
        LlamaServerBin = Path.Combine(LlamaInstallFolder, name);

        if (Debugger.IsAttached)
        {
            McpHostFolder = "D:\\tenny_lu\\Documents\\McpNodeJs";
            DispatcherFolder = "D:\\workspace\\output\\McpBackend-win-x64";
            ChatFrontendFolder = "D:\\tenny_lu\\Documents\\dist";
            RagBackendFolder = "D:\\workspace\\output\\RagBackend-win-x64";
        }
        else
        {
            // Get current app folder
            string baseAppFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            McpHostFolder = Path.Combine(baseAppFolder, "McpNodeJs");
            DispatcherFolder = Path.Combine(baseAppFolder, "McpBackend");
            ChatFrontendFolder = Path.Combine(baseAppFolder, "dist");
            RagBackendFolder = Path.Combine(baseAppFolder, "RagBackend");
        }

        ChromaDbFolder = Path.Combine(settingFolder, "ChromaDb");
        if (!Directory.Exists(ChromaDbFolder))
        {
            try
            {
                Directory.CreateDirectory(ChromaDbFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to create Chroma DB folder: " + ex);
            }
        }

        // Copy default config file to local setting path if not exist
        const string configFileName = "mcp_servers.config.json";
        McpHostConfigFile = Path.Combine(settingFolder, configFileName);
        if (!File.Exists(McpHostConfigFile))
        {
            try
            {
                string defaultConfigFile = Path.Combine(McpHostFolder, configFileName);
                File.Copy(defaultConfigFile, McpHostConfigFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to copy default MCP config json to appdata: " + ex);
            }
        }

        DispatcherDbFile = Path.Combine(settingFolder, "dispatcher.db");
        QuartzDbFile = Path.Combine(settingFolder, "quartz.db");

        FileSystemFolders = new List<string>();
    }

    public static readonly string LlamaInstallFolder;
    public static readonly string LlamaServerBin;

    public static readonly string McpHostFolder;
    public static readonly string McpHostConfigFile;

    public static readonly string DispatcherFolder;
    public static readonly string DispatcherDbFile;

    public static readonly string ChatFrontendFolder;

    public static readonly string ChromaDbFolder;

    public static readonly string RagBackendFolder;

    public static readonly string QuartzDbFile;

    public static LlamaService LlamaService { get; set; }

    public static McpNodeJsService NodeJsService { get; set; }

    public static DispatcherBackendService BackendService { get; set; }

    public static ChromaDbService ChromaDbService { get; set; }

    public static RagBackendService RagBackendService { get; set; }

    public static MainViewModel MainViewModel { get; set; }

    public static List<string> KnownCommands { get; set; }

    public static List<string> FileSystemFolders { get; set; }
}

