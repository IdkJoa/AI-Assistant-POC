using AiAssistant.Domain.Domain.ValueObjects;

namespace AiAssistant.Domain.Domain.Documents;

public sealed record DocumentChunk
{
    public required ChunkId Id { get; init; }
    public required DocumentId DocumentId { get; init; }
    public required string FileName { get; init; }
    public required string Content { get; init; }
    public required int ChunkIndex { get; init; }
    public required Dictionary<string, string> Metadata { get; init; }
    public float[]? Embedding { get; init; }
}