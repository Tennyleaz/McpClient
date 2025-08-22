using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class TranscriptResponse
{
    public List<Transcript> Transcript { get; set; }
    public string Note { get; set; }
}

internal class Transcript
{
    public DateTime Timestamp { get; set; }
    public string Text { get; set; }
}