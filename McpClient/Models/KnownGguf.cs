using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McpClient.Utils;

namespace McpClient.Models;

internal class KnownGguf
{
    public string Name { get; set; }
    public string GgufFilePath { get; set; }
    public string Architecture { get; set; }
    public string Size { get; set; }
    public LlamaFileType FileType { get; set; }
    public int MaxContextLength { get; set; }
}
