namespace AiAssistant.Infrastructure.Configuration;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";
    public required string BaseUrl { get; init; }
    public required string LlmModel { get; init; }
    public required string EmbeddingModel { get; init; }
    public int TimeoutSeconds { get; init; } = 120;
}