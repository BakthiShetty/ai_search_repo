using Dapper;
using Npgsql;
using SemanticSearch.Api.Controllers;

namespace SemanticSearch.Api.Services;

public class SearchService(
    OllamaEmbeddingClient ollama,
    IConfiguration config,
    ILogger<SearchService> logger)
{
    private string ConnectionString => config.GetConnectionString("Postgres")!;
    private const double ConfidenceThreshold = 0.65;

    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(
        string query, int top, CancellationToken ct)
    {
        var queryVector = await ollama.EmbedAsync(query, ct);
        if (queryVector == null)
        {
            logger.LogWarning("Could not embed query — falling back to keyword search");
            return await KeywordSearchAsync(query, top, ct);
        }

        var vectorResults = await VectorSearchAsync(queryVector, top * 2, ct);
        var deduplicated = DeduplicateByFile(vectorResults, top);

        var topScore = deduplicated.FirstOrDefault()?.Score ?? 0;
        if (topScore < ConfidenceThreshold)
        {
            var keywordResults = await KeywordSearchAsync(query, top, ct);
            return Merge(deduplicated, keywordResults, top);
        }

        return deduplicated;
    }

    private async Task<List<SearchResultDto>> VectorSearchAsync(
        float[] vector, int limit, CancellationToken ct)
    {
        var vectorLiteral = $"[{string.Join(',', vector)}]";
        await using var conn = new NpgsqlConnection(ConnectionString);

        var rows = await conn.QueryAsync<SearchResultRow>(
            new CommandDefinition(
                @"SELECT m.id, m.filename, m.folder_path, m.author, m.created_at, m.nas_path,
                         1 - (c.embedding <=> @vec::vector) AS score
                  FROM file_chunks c
                  JOIN file_metadata m ON m.id = c.file_id
                  ORDER BY c.embedding <=> @vec::vector
                  LIMIT @limit",
                new { vec = vectorLiteral, limit },
                cancellationToken: ct));

        return rows.Select(r => new SearchResultDto(
            r.Id, r.Filename, r.FolderPath, r.Author, r.CreatedAt, r.NasPath, r.Score))
            .ToList();
    }

    private async Task<List<SearchResultDto>> KeywordSearchAsync(
        string query, int limit, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);

        var rows = await conn.QueryAsync<SearchResultRow>(
            new CommandDefinition(
                @"SELECT id, filename, folder_path, author, created_at, nas_path, 0.5::float AS score
                  FROM file_metadata
                  WHERE to_tsvector('english',
                        filename_normalized || ' ' || COALESCE(folder_path,'') || ' ' || COALESCE(author,''))
                        @@ websearch_to_tsquery('english', @query)
                  LIMIT @limit",
                new { query, limit },
                cancellationToken: ct));

        return rows.Select(r => new SearchResultDto(
            r.Id, r.Filename, r.FolderPath, r.Author, r.CreatedAt, r.NasPath, r.Score))
            .ToList();
    }

    private static List<SearchResultDto> DeduplicateByFile(
        List<SearchResultDto> results, int top)
    {
        return results
            .GroupBy(r => r.Id)
            .Select(g => g.OrderByDescending(r => r.Score).First())
            .OrderByDescending(r => r.Score)
            .Take(top)
            .ToList();
    }

    private static IReadOnlyList<SearchResultDto> Merge(
        IReadOnlyList<SearchResultDto> vector,
        IReadOnlyList<SearchResultDto> keyword,
        int top)
    {
        var seen = new HashSet<Guid>(vector.Select(r => r.Id));
        var merged = vector.ToList();
        merged.AddRange(keyword.Where(r => !seen.Contains(r.Id)));
        return merged.Take(top).ToList();
    }

    private class SearchResultRow
    {
        public Guid Id { get; set; }
        public string Filename { get; set; } = "";
        public string? FolderPath { get; set; }
        public string? Author { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string NasPath { get; set; } = "";
        public double Score { get; set; }
    }
}
