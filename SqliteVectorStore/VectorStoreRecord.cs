using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteVectorStore;

public class VectorStoreRecord
{
    public string Id { get; set; }
    public string DocumentId { get; set; }
    public string DocumentName { get; set; }
    public int ChunkIndex { get; set; }
    public float[] Embedding { get; set; }
    public string Document { get; set; }
    public string Language { get; set; }
    public string SourceType { get; set; }
    public DateTime Timestamp { get; set; }
    public string MetadataJson { get; set; }  // extras
}
