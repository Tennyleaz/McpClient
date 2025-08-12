using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Models
{
    internal class LoginResponse
    {
        public string UserEmail { get; set; }
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
