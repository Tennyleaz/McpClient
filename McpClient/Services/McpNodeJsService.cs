using ModelContextProtocol.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace McpClient.Services;


internal class McpNodeJsService : CliService, IDisposable
{
    private readonly HttpClient _httpClient;
    private const int PORT = 17925;
    private static readonly string BASE_URL = "http://localhost:" + PORT;
    private readonly int _healthCheckIntervalMs;
    private Task _healthTask;

    public static McpNodeJsService CreateMcpNodeJsService()
    {
        string path = Path.Combine(GlobalService.McpHostFolder, "mcp-host-use.exe");
        if (File.Exists(path))
            return new McpNodeJsService(path);
        return null;
    }

    private McpNodeJsService(string binaryPath) : base(binaryPath, null, 50)
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        _httpClient.BaseAddress = new Uri(BASE_URL);

        _healthCheckIntervalMs = 2000;
    }


    public async Task<bool> CheckHealth()
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }

    private async Task HealthLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_process == null || _process.HasExited)
            {
                SetState(CliServiceState.Crashed);
                break;
            }
            // Optional: ping HTTP /health endpoint here for deeper checks
            bool isUp = await CheckHealth();
            if (!isUp)
            {
                if (State == CliServiceState.Running)
                {
                    SetState(CliServiceState.Crashed);
                    break;
                }
                else if (State == CliServiceState.Starting)
                {
                    // Do nothing, wait for start finish
                }
                else if (State == CliServiceState.Stopping || State == CliServiceState.Stopped)
                {
                    SetState(CliServiceState.Stopped);
                    break;
                }
                else
                {
                    SetState(CliServiceState.Crashed);
                    break;
                }
            }
            else
            {
                SetState(CliServiceState.Running);
            }

            await Task.Delay(_healthCheckIntervalMs, cancellationToken).ContinueWith(_ => { }, cancellationToken);
        }
    }

    public new bool Start()
    {
        if (base.Start())
        {
            _healthTask = Task.Run(() => HealthLoop(_cts!.Token));
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        Stop();
        _httpClient.Dispose();
    }
}
