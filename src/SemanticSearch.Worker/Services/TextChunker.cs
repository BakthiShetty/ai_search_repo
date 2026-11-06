namespace SemanticSearch.Worker.Services;

public class TextChunker
{
    private const int MaxWords   = 400;  // ~512 tokens
    private const int OverlapWords = 40; // ~10% overlap

    public IEnumerable<string> Split(string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) yield break;

        var start = 0;
        while (start < words.Length)
        {
            var end = Math.Min(start + MaxWords, words.Length);
            yield return string.Join(' ', words[start..end]);
            if (end == words.Length) break;
            start = end - OverlapWords;
        }
    }
}
