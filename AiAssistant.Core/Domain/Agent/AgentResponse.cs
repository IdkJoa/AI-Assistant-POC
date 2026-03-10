using AiAssistant.Domain.Tools;

namespace AiAssistant.Domain.Domain.Agent;

public sealed record AgentResponse
{
    public required string Answer { get; init; }
    public required List<string> SourceChunks { get; init; }  
    public EmailDraft EmailDraft { get; init; }
    public List<ToolCall>? ToolCalls { get; init; }
    public required DateTime GeneratedAt { get; init; }
}

public sealed record ToolCall
{
    public required string ToolName { get; init; }
    public required Dictionary<string, object> Parameters { get; init; }
    public required string Result { get; init; }
}