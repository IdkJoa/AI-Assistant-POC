namespace AiAssistant.Infrastructure.Configuration;

public class ClaudeOptions
{
    public const string SectionName = "Claude";
    public required string ApiKey { get; init; }
    public required string LlmModel { get; init; }
}