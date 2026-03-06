using AiAssistant.Domain.Domain.ValueObjects;

namespace AiAssistant.Domain.Domain.Documents;

public sealed record DocumentInfo
{
    public required DocumentId Id { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required DateTime IndexedAt { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public required Dictionary<string, string> Metadata { get; init; }
}