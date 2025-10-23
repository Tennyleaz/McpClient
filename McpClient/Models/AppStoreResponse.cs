using System.Collections.Generic;

namespace McpClient.Models;

internal class AppStoreResponse
{
    public List<StoreApp> Apps { get; set; }
    public int Total { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
    public int TotalPages { get; set; }
}
