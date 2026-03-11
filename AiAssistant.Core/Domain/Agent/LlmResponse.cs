namespace AiAssistant.Domain.Domain.Agent;

public sealed record LlmResponse
{
    public string? Text { get; init; }
    public List<ToolUseRequest> ToolUseRequests { get; init; } = [];
    public bool HasToolUse => ToolUseRequests.Count > 0;
}

public sealed record ToolUseRequest
{
    public string ToolUseId { get; init; } = "";
    public string ToolName { get; init; } = "";
    public string InputJson { get; init; } = "";
}

public sealed record ToolDefinition
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
}