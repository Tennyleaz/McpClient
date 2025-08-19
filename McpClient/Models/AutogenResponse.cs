using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class AutogenResponse
{
    public string Type { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string Task { get; set; }
    public string Response { get; set; }
    public bool IsTerminated { get; set; }

    public override string ToString()
    {
        return $"[{From}] {Task}: {Response}";
    }
}

internal class AutoGenRequest
{
    public string connectionId;
    public List<int> agents;
    public int group;
    public string query;
}
