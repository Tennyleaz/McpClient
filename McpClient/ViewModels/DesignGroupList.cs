using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McpClient.Models;

namespace McpClient.ViewModels;
internal class DesignGroupList : GroupListViewModel
{
    public DesignGroupList()
    {
        Groups.Add(new Group
        {
            Id = 0,
            Name = "CRUD",
            Description = "專門處理檔案系統和資料庫的團隊"
        });
        Groups.Add(new Group
        {
            Id = 0,
            Name = "Automation",
            Description = "處理自動化操作並儲存結果的團隊"
        });
    }
}
