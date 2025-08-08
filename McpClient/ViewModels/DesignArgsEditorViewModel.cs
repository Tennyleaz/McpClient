using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.ViewModels;

internal class DesignArgsEditorViewModel : ArgsEditorViewModel
{
    public DesignArgsEditorViewModel()
    {
        Args.Add(new ArgsEditorItem("run"));
        Args.Add(new ArgsEditorItem("-1"));
        Args.Add(new ArgsEditorItem("--rm"));
        Args.Add(new ArgsEditorItem("mcp/postgres"));
        Args.Add(new ArgsEditorItem("postgresql://phisonai:!phisonai@10.102.196.56:5432/phisonai"));
    }
}

