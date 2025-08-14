using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class Group
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public bool IsPublic { get; set; }
    public List<Agent> Workflows { get; set; }
}

internal class Agent
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public bool Is_public { get; set; }
    public DateTime Created_at { get; set; }
    public DateTime Updated_at { get; set; }
    public int Created_by { get; set; }
    public List<AgentHeader> CustomHeaders { get; set; }
}

internal class AgentHeader
{
    public int Id { get; set; }
    public int Agent_id { get; set; }
    public string Header { get; set; }
    public string Description { get; set; }
    public HeaderValue HeaderValues { get; set; }
}

internal class HeaderValue
{
    public int Id { get; set; }
    public int Header_id { get; set; }
    public int User_id { get; set; }
    public string Value { get; set; }
}

internal class Workflow
{
    public int Id { get; set; }
    public int Group_id { get; set; }
    public int Agent_id { get; set; }
    public int Sequence_order { get; set; }
}