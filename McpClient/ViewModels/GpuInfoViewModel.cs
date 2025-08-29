using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.ViewModels;

internal class GpuInfoViewModel
{
    private readonly string _name;
    private readonly int _index;
    private readonly int _vramSizeMb;

    public GpuInfoViewModel(int index, string name, int vramSizeMb)
    {
        _index = index;
        _name = name;
        _vramSizeMb = vramSizeMb;
    }

    public string Name => _name;

    public string VRAM
    {
        get { return _vramSizeMb + " MB"; }
    }

    public int Index => _index;

    public int VramMb => _vramSizeMb;
}

