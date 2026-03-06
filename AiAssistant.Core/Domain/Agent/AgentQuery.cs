using AiAssistant.Domain.Domain.ValueObjects;

namespace AiAssistant.Domain.Domain.Agent;

public sealed record AgentQuery
{
    public required string Question { get; init; }
    public DocumentId DocumentId { get; init; }
    public int MaxContextChunks { get; init; } = 5;
}