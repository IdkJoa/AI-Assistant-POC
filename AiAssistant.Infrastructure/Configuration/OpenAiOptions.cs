using Microsoft.Extensions.Options;

namespace AiAssistant.Infrastructure.Configuration;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";
    public required string ApiKey { get; init; }
    public required  string LlmModel { get; init; }
    public required  string EmbeddingModel { get; init; }
}