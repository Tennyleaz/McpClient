using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteVectorStore;

public class VectorStoreClient
{
    private readonly string _connectionString;

    public VectorStoreClient(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        Initialize();
    }

    private void Initialize()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS records (
                id TEXT PRIMARY KEY,
                document_id TEXT,
                document_name TEXT,
                chunk_index INTEGER,
                embedding BLOB,
                document TEXT,
                language TEXT,
                source_type TEXT,
                timestamp TEXT,
                metadata TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_records_document_id ON records(document_id);
            CREATE INDEX IF NOT EXISTS idx_records_language ON records(language);
        ";
        cmd.ExecuteNonQuery();
    }

    public void Upsert(VectorStoreRecord record)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO records
                (id, document_id, document_name, chunk_index, embedding, document, language, source_type, timestamp, metadata)
            VALUES
                ($id, $documentId, $documentName, $chunkIndex, $embedding, $document, $language, $sourceType, $timestamp, $metadata)
        ";

        cmd.Parameters.AddWithValue("$id", record.Id);
        cmd.Parameters.AddWithValue("$documentId", record.DocumentId);
        cmd.Parameters.AddWithValue("$documentName", record.DocumentName);
        cmd.Parameters.AddWithValue("$chunkIndex", record.ChunkIndex);
        cmd.Parameters.AddWithValue("$embedding", VectorHelpers.FloatArrayToBytes(record.Embedding));
        cmd.Parameters.AddWithValue("$document", record.Document);
        cmd.Parameters.AddWithValue("$language", record.Language ?? "");
        cmd.Parameters.AddWithValue("$sourceType", record.SourceType ?? "");
        cmd.Parameters.AddWithValue("$timestamp", record.Timestamp.ToString("o")); // ISO 8601
        cmd.Parameters.AddWithValue("$metadata", record.MetadataJson ?? "");
        cmd.ExecuteNonQuery();
    }

    public void UpsertMany(IEnumerable<VectorStoreRecord> records)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();
        foreach (var rec in records)
        {
            Upsert(rec);
        }
        tx.Commit();
    }

    /// <summary>
    /// Search top-K most similar records to the query vector, optionally filtering on language/document_id.
    /// </summary>
    public List<(VectorStoreRecord Record, float Similarity)> Search(
        float[] queryEmbedding, int topK = 5, int offset = 0,
        string language = null, string documentId = null)
    {
        var records = new List<VectorStoreRecord>();

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        var where = new List<string>();
        if (!string.IsNullOrEmpty(language))
        {
            where.Add("language = $lang");
            cmd.Parameters.AddWithValue("$lang", language);
        }
        if (!string.IsNullOrEmpty(documentId))
        {
            where.Add("document_id = $docId");
            cmd.Parameters.AddWithValue("$docId", documentId);
        }
        cmd.CommandText = "SELECT * FROM records"
            + (where.Any() ? " WHERE " + string.Join(" AND ", where) : "");

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            records.Add(ReadRecord(reader));
        }

        // Compute similarity and take top-K
        var scored = records
            .Select(r => (Record: r, Similarity: VectorHelpers.CosineSimilarity(queryEmbedding, r.Embedding)))
            .OrderByDescending(x => x.Similarity)
            .Skip(offset)
            .Take(topK)
            .ToList();

        return scored;
    }

    private static VectorStoreRecord ReadRecord(IDataRecord reader)
    {
        return new VectorStoreRecord
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            DocumentId = reader["document_id"] as string,
            DocumentName = reader["document_name"] as string,
            ChunkIndex = Convert.ToInt32(reader["chunk_index"]),
            Embedding = VectorHelpers.BytesToFloatArray((byte[])reader["embedding"]),
            Document = reader["document"] as string,
            Language = reader["language"] as string,
            SourceType = reader["source_type"] as string,
            Timestamp = DateTime.TryParse(reader["timestamp"] as string, out var dt) ? dt : DateTime.MinValue,
            MetadataJson = reader["metadata"] as string,
        };
    }

    public async Task<List<DocumentListing>> ListDocumentsAsync()
    {
        var result = new List<DocumentListing>();

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT DISTINCT document_id, document_name, language, source_type,
                MIN(timestamp) AS min_timestamp, MAX(timestamp) AS max_timestamp,
                COUNT(*) AS chunk_count
            FROM records
            GROUP BY document_id, document_name, language, source_type
            ORDER BY min_timestamp
        ";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new DocumentListing
            {
                DocumentId = reader["document_id"] as string,
                DocumentName = reader["document_name"] as string,
                Language = reader["language"] as string,
                SourceType = reader["source_type"] as string,
                FirstTimestamp = DateTime.TryParse(reader["min_timestamp"] as string, out var dt1) ? dt1 : DateTime.MinValue,
                LastTimestamp = DateTime.TryParse(reader["max_timestamp"] as string, out var dt2) ? dt2 : DateTime.MinValue,
                ChunkCount = Convert.ToInt32(reader["chunk_count"])
            });
        }
        return result;
    }

    public async Task<int> DeleteByDocumentIdAsync(string documentId)
    {
        if (string.IsNullOrEmpty(documentId))
            throw new ArgumentNullException(nameof(documentId));

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM records WHERE document_id = $id";
        cmd.Parameters.AddWithValue("$id", documentId);
        int deleted = await cmd.ExecuteNonQueryAsync();
        return deleted;
    }

    // You can add more methods (filter by document_id, etc) as needed.
}
