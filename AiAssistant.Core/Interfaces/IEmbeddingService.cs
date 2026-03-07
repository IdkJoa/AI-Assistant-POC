using AiAssistant.Domain.Common.OperationResult;

namespace AiAssistant.Domain.Interfaces;

public interface IEmbeddingService
{
    Task<Result<float[]>> GenerateAsync(string text, CancellationToken cancellationToken = default);
}