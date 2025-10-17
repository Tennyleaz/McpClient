using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteVectorStore;

public class DocumentListing
{
    public string DocumentId { get; set; }
    public string DocumentName { get; set; }
    public string Language { get; set; }
    public string SourceType { get; set; }
    public DateTime FirstTimestamp { get; set; }
    public DateTime LastTimestamp { get; set; }
    public int ChunkCount { get; set; }
}
