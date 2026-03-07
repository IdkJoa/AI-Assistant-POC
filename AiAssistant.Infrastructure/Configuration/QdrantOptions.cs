namespace SG.AIAssistant.Infrastructure.Configuration;

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string CollectionName { get; init; }
    public int VectorSize { get; init; } = 768; // nomic-embed-text output size
}