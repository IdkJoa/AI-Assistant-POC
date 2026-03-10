using AiAssistant.Domain.Common.OperationResult;

namespace AiAssistant.Domain.Interfaces;

public interface ILlmService
{
    Task<Result<string>> ChatAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default);
}