using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McpClient.Models;

namespace McpClient.ViewModels
{
    internal class DesignWorkflowListViewModel : WorkflowListViewModel
    {
        public DesignWorkflowListViewModel()
        {
            OfflineWorkflows.Add(new OfflineWorkflow
            {
                Endpoint = "http://127.0.0.1/api/test",
                Id = 1,
                User_id = 1,
                Name = "test workflow 1",
                Payload = "test payload 1"
            });
            OfflineWorkflows.Add(new OfflineWorkflow
            {
                Endpoint = "http://127.0.0.1/api/test",
                Id = 2,
                User_id = 2,
                Name = "test workflow 2",
                Payload = "test payload 2"
            });
        }
    }
}
