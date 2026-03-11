using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Domain.Agent;

namespace AiAssistant.Domain.Interfaces;

public interface ILlmService
{
    Task<Result<string>> ChatAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default);
    
    Task<Result<LlmResponse>> ChatWithToolsAsync(
        string systemPrompt,
        string userMessage,
        IReadOnlyList<ToolDefinition> tools,
        CancellationToken ct = default);
}