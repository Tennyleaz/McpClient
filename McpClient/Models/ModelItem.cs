using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class ModelData
{
    public List<ModelItem> Data { get; set; }
}

internal class ModelItem
{
    public ModelItem()
    {

    }

    public ModelItem(string name, string type)
    {
        LabelName = ModelName = name;
        Id = null;
        Type = type;
    }

    public string Id { get; set; }
    public string LabelName { get; set; }
    public string ModelName { get; set; }
    public string Type { get; set; }
}
