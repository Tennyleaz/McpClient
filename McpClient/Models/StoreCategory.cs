using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Models;

internal class StoreCategoryItem(string name, string value)
{
    public string Name { get; set; } = name;
    public string Value { get; set; } = value;
}
