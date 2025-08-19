using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class DocumentDto
{
    public string Name { get; set; }
    public Guid? ChatId { get; set; }
    public DateTime CreatedTime { get; set; }
    public byte[] Data { get; set; }
}
