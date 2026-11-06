using System.Text.RegularExpressions;
using Dapper;
using Npgsql;

namespace SemanticSearch.Worker.Services;

public enum IndexOutcome
{
    Indexed,
    AlreadyIndexed,
    NoContentExtracted,
    Failed
}

public class IndexingPipeline(
    ContentExtractor extractor,
    TextChunker chunker,
    OllamaEmbeddingClient ollama,
    IConfiguration config,
    ILogger<IndexingPipeline> logger)
{
    private string ConnectionString => config.GetConnectionString("Postgres")!;

    /// <summary>
    /// Reports what happened via <see cref="IndexOutcome"/> instead of throwing for
    /// expected outcomes, so a single problematic file (bad content, DB hiccup,
    /// embedding failure) never aborts the rest of the run. Cancellation still propagates.
    /// </summary>
    public async Task<IndexOutcome> IndexAsync(string filePath, CancellationToken ct)
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync(ct);

            if (await IsAlreadyIndexed(conn, filePath, ct))
            {
                logger.LogDebug("Already indexed: {File}", Path.GetFileName(filePath));
                return IndexOutcome.AlreadyIndexed;
            }

            logger.LogInformation("Indexing: {File}", Path.GetFileName(filePath));

            var text = extractor.Extract(filePath);
            if (string.IsNullOrWhiteSpace(text))
            {
                logger.LogWarning("No text extracted from {File}", Path.GetFileName(filePath));
                return IndexOutcome.NoContentExtracted;
            }

            var (author, createdAt) = filePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)
                ? extractor.ExtractDocxMetadata(filePath)
                : (null, (DateTime?)null);

            var normalizedName = NormalizeFilename(Path.GetFileNameWithoutExtension(filePath));
            var fileId = await InsertFileMetadata(conn, filePath, normalizedName, author, createdAt, ct);

            // Prepend the human-readable filename so it is captured in vector embeddings.
            // This lets queries like "Incident Report June 2024" match
            // Safety_Incident_Report_June2024.docx even when the body text is sparse.
            var textWithTitle = $"Document title: {normalizedName}\n\n{text}";
            var chunks = chunker.Split(textWithTitle).ToList();
            var indexed = 0;

            for (var i = 0; i < chunks.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var vector = await ollama.EmbedAsync(chunks[i], ct);
                if (vector == null)
                {
                    logger.LogWarning(
                        "Skipping chunk {ChunkIndex}/{ChunkCount} of {File}: embedding failed",
                        i + 1, chunks.Count, Path.GetFileName(filePath));
                    continue;
                }

                await InsertChunk(conn, fileId, i, vector, ct);
                indexed++;
            }

            logger.LogInformation(
                "Indexed {File}: {Indexed}/{ChunkCount} chunks embedded",
                Path.GetFileName(filePath), indexed, chunks.Count);
            return IndexOutcome.Indexed;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to index {File}", Path.GetFileName(filePath));
            return IndexOutcome.Failed;
        }
    }

    private static async Task<bool> IsAlreadyIndexed(NpgsqlConnection conn, string filePath, CancellationToken ct)
    {
        var count = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(1) FROM file_metadata WHERE nas_path = @path",
                new { path = filePath },
                cancellationToken: ct));
        return count > 0;
    }

    private static async Task<Guid> InsertFileMetadata(
        NpgsqlConnection conn, string filePath,
        string filenameNormalized,
        string? author, DateTime? createdAt, CancellationToken ct)
    {
        return await conn.ExecuteScalarAsync<Guid>(
            new CommandDefinition(
                @"INSERT INTO file_metadata (filename, filename_normalized, folder_path, author, created_at, nas_path)
                  VALUES (@filename, @filenameNormalized, @folderPath, @author, @createdAt, @nasPath)
                  RETURNING id",
                new
                {
                    filename           = Path.GetFileName(filePath),
                    filenameNormalized,
                    folderPath = Path.GetDirectoryName(filePath),
                    author,
                    createdAt,
                    nasPath    = filePath
                },
                cancellationToken: ct));
    }

    private static string NormalizeFilename(string name)
    {
        name = name.Replace('_', ' ').Replace('-', ' ');
        name = Regex.Replace(name, @"(?<=[a-z])(?=[A-Z])", " ");
        name = Regex.Replace(name, @"(?<=[A-Za-z])(?=\d)|(?<=\d)(?=[A-Za-z])", " ");
        return Regex.Replace(name, @"\s+", " ").Trim();
    }

    private static async Task InsertChunk(
        NpgsqlConnection conn, Guid fileId,
        int chunkIndex, float[] embedding, CancellationToken ct)
    {
        var vectorLiteral = $"[{string.Join(',', embedding)}]";
        await conn.ExecuteAsync(
            new CommandDefinition(
                @"INSERT INTO file_chunks (file_id, chunk_index, embedding)
                  VALUES (@fileId, @chunkIndex, @embedding::vector)",
                new { fileId, chunkIndex, embedding = vectorLiteral },
                cancellationToken: ct));
    }
}
