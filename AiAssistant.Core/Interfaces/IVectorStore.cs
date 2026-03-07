using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Domain.Documents;

namespace AiAssistant.Domain.Interfaces;

public interface IVectorStore
{
    Task<Result> UpsertAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<DocumentChunk>>> SearchAsync(float[] queryEmbedding, int topK = 5, CancellationToken cancellationToken = default);
    Task<Result> EnsureCollectionExistsAsync(CancellationToken cancellationToken = default);
}