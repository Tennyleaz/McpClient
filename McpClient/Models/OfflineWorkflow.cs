using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class OfflineWorkflow
{
    public int Id { get; set; }
    public int User_id { get; set; }
    public string Name { get; set; }
    public string Endpoint { get; set; }
    public string Payload { get; set; }
}
