using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class McpServerListResponse
{
    public List<McpServerItem> data { get; set; }
}

internal class McpServerItem
{
    public string server_name { get; set; }
}
