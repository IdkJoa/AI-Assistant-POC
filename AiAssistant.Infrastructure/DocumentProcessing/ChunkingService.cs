namespace AiAssistant.Infrastructure.DocumentProcessing;

public static class ChunkingService
{
    public static IReadOnlyList<string> Chunk(
        string text,
        int chunkSize = 512,
        int overlap = 64)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        var chunks = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var i = 0;

        while (i < words.Length)
        {
            var chunk = words.Skip(i).Take(chunkSize);
            chunks.Add(string.Join(' ', chunk));
            i += chunkSize - overlap;
        }

        return chunks;
    }
}