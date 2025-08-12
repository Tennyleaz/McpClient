using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class WebviewToken
{
    public WebviewTokenState State { get; set; }
    public int Version { get; set; }
}

internal class WebviewTokenState
{
    public string Username { get; set; }
    public string Token { get; set; }
    public string Error { get; set; }
}