namespace AiAssistant.Infrastructure.Configuration;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";
    public required string ApiKey { get; init; }
    public required string LlmModel { get; init; }        // "gemini-1.5-flash"
    public required string EmbeddingModel { get; init; }  // "text-embedding-004"
}