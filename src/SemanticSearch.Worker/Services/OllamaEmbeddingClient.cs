using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SemanticSearch.Worker.Services;

public class OllamaEmbeddingClient(HttpClient http, ILogger<OllamaEmbeddingClient> logger)
{
    private const string Model = "nomic-embed-text";

    public async Task<float[]?> EmbedAsync(string text, CancellationToken ct = default)
    {
        try
        {
            var response = await http.PostAsJsonAsync(
                "/api/embeddings",
                new { model = Model, prompt = text },
                ct);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<OllamaEmbedResult>(ct);
            return result?.Embedding;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Ollama embed failed: {Message}", ex.Message);
            return null;
        }
    }

    private record OllamaEmbedResult(
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
