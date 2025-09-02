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
    private readonly string _backend;

    public GpuInfoViewModel(int index, string name, int vramSizeMb, string backend = null)
    {
        _index = index;
        _name = name;
        _vramSizeMb = vramSizeMb;
        _backend = backend;
    }

    public string Name => _name;

    public string VRAM
    {
        get { return _vramSizeMb + " MB"; }
    }

    public int Index => _index;

    public int VramMb => _vramSizeMb;

    public string Backend => _backend;
}

