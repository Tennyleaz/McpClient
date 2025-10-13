using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.ViewModels;
public class WizardViewModel
{
    public List<RuntimeItem> DetectedRuntimes { get; set; } = new();
    public List<RuntimeItem> MissingRuntimes { get; set; } = new();
}

public class RuntimeItem
{
    public RuntimeItem(string name, string status)
    {
        Name = name;
        Status = status;
    }

    public string Name { get; set; }
    public string Status { get; set; }
    public bool IsManualDownload { get; set; }
    public bool IsCommandExist { get; set; }
}
