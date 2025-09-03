using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McpClient.Models;

namespace McpClient.ViewModels;

internal class ModelViewModel
{
    public string Name { get; set; }
    public string Model { get; set; }
    public string Icon { get; set; }

    public ModelViewModel(ModelItem item)
    {
        Name = item.LabelName;
        Model = item.ModelName;
        Icon = item.Type == "cloud" ? "☁️" : "🖥️";
    }
}
