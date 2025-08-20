using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class AutogenResponse
{
    public string Type { get; set; }
    public string FromAgent { get; set; }
    public string ToAgent { get; set; }
    public string Task { get; set; }
    public string Response { get; set; }
    public bool IsTerminated { get; set; }

    public override string ToString()
    {
        return $"[{FromAgent}] {Task}: {Response}";
    }
}

internal class AutoGenRequest
{
    public string connectionId;
    public List<int> agents;
    public int group;
    public string query;
}

internal class AutogenStreamResponse
{
    public string Mcptools { get; set; }
    public string Id { get; set; }
    public string Object { get; set; }
    public int Created { get; set; }
    public string Model { get; set; }
    public string Server_name { get; set; }
    public string Tool_name { get; set; }
    public Dictionary<string, object> Tool_args { get; set; }
    public List<AutogenChoice> Choices { get; set; }
}

internal class AutogenChoice
{
    public int Index { get; set; }
    public AutogenStreamContent Delta { get; set; }
    public string Logprobs { get; set; }
    public string Finish_reason { get; set; }
    public string Stop_reason { get; set; }
}

internal class AutogenStreamContent
{
    public string Role { get; set; }
    public string Content { get; set; }
}