using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Domain.Documents;

namespace AiAssistant.Domain.Interfaces;

public interface IVectorStore
{
    Task<Result> UpsertAsync(IEnumerable<DocumentChunk> chunks, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DocumentChunk>>> SearchAsync(float[] queryEmbedding, int topK = 5, CancellationToken ct = default);
    Task<Result> EnsureCollectionExistsAsync(CancellationToken ct = default);
}