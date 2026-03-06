using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Domain.Documents;

namespace AiAssistant.Domain.Interfaces;

public interface IDocumentProcessor
{
    bool CanProcess(string contentType);
    Task<Result<IReadOnlyList<DocumentChunk>>> ProcessAsync(
        Stream content,
        DocumentInfo info,
        CancellationToken ct = default);
}