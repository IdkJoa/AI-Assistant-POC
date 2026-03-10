using System.Globalization;
using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Domain.Documents;
using AiAssistant.Domain.Domain.ValueObjects;
using AiAssistant.Domain.Interfaces;
using AiAssistant.Infrastructure.Configuration;
using AiAssistant.Infrastructure.Ollama;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace AiAssistant.Infrastructure.Qdrant;

public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _qdrantClient;
    private readonly QdrantOptions _options;
    private readonly ILogger<OllamaEmbeddingService> _logger;

    public QdrantVectorStore(
        QdrantClient qdrantClient, 
        IOptions<QdrantOptions> options, 
        ILogger<OllamaEmbeddingService> logger)
    {
        _qdrantClient = qdrantClient;
        _options = options.Value;
        _logger = logger;
    }


    public async Task<Result> UpsertAsync(
        IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting to upsert chunks into Qdrant");
            var pointsList = new List<PointStruct>();

            foreach (var c in chunks.Where(c => c.Embedding is { Length: > 0 }))
            {
                var point = new PointStruct
                {
                    Id      = Guid.Parse(c.Id.ToString()),
                    Vectors = c.Embedding!,
                    Payload =
                    {
                        ["document_id"] = c.DocumentId.ToString(),
                        ["file_name"] = c.FileName,
                        ["content"] = c.Content,
                        ["chunk_index"] = c.ChunkIndex
                    }
                };

                foreach (var (key, value) in c.Metadata)
                    point.Payload[key] = value;

                pointsList.Add(point);
            }
            
            if (pointsList.Count == 0)
            {
                return Result.Failure(Error.VectorStoreFailure("No valid chunks with embeddings to upsert."));
            }

            await _qdrantClient.UpsertAsync(_options.CollectionName, pointsList, cancellationToken: cancellationToken);

            _logger.LogInformation("Upserted {Count} chunks into Qdrant", pointsList.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert chunks into Qdrant");
            return Result.Failure(Error.VectorStoreFailure(ex.Message));        }
    }

    public async Task<Result<IReadOnlyList<DocumentChunk>>> SearchAsync(float[] queryEmbedding, int topK = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting to search Qdrant chunks");
            var results = await _qdrantClient.SearchAsync(
                _options.CollectionName,
                queryEmbedding,
                limit: (ulong)topK,
                scoreThreshold: 0.6f,
                cancellationToken: cancellationToken);
            
            var chunks = results.Select(r => new DocumentChunk
            {
                Id = ChunkId.From(r.Id.Uuid),
                DocumentId =  DocumentId.From(GetString(r.Payload, "document_id")),
                FileName    = r.Payload["file_name"].StringValue,
                Content     = r.Payload["content"].StringValue,
                ChunkIndex  = (int)r.Payload["chunk_index"].IntegerValue,
                Metadata    = r.Payload
                    .Where(kv => !new[] { "document_id", "file_name", "content", "chunk_index" }
                        .Contains(kv.Key))
                    .ToDictionary(kv => kv.Key, kv => kv.Value.StringValue)
            }).ToList();
            
            return Result<IReadOnlyList<DocumentChunk>>.Success(chunks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Failed to search Qdrant chunks");
            return Result<IReadOnlyList<DocumentChunk>>.Failure(Error.VectorStoreFailure(ex.Message));
        }   
    }

    public async Task<Result> EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting to ensure collection exists");
            
            var collections = await _qdrantClient.ListCollectionsAsync(cancellationToken);
            if (collections.Any(c => c == _options.CollectionName))
                return Result.Success();

            await _qdrantClient.CreateCollectionAsync(
                _options.CollectionName,
                new VectorParams
                {
                    Size = (ulong)_options.VectorSize,
                    Distance = Distance.Cosine
                }, 
                cancellationToken: cancellationToken);
            
            _logger.LogInformation("Created Qdrant collection: {CollectionName}", _options.CollectionName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure Qdrant collection exists");
            return Result.Failure(Error.VectorStoreFailure(ex.Message));
        }
    }
    
    public async Task<Result> DeleteCollectionAsync(CancellationToken ct = default)
    {
        try
        {
            await _qdrantClient.DeleteCollectionAsync(_options.CollectionName, cancellationToken: ct);
            _logger.LogInformation("Deleted Qdrant collection: {Collection}", _options.CollectionName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete collection");
            return Result.Failure(Error.VectorStoreFailure(ex.Message));
        }
    }
    
    private static string GetString(MapField<string, Value> payload, string key) =>
        payload.TryGetValue(key, out var val) ? GetStringValue(val) : string.Empty;

    private static long GetInteger(MapField<string, Value> payload, string key) =>
        payload.TryGetValue(key, out var val) && val.KindCase == Value.KindOneofCase.IntegerValue
            ? val.IntegerValue
            : 0;

    private static string GetStringValue(Value val) => val.KindCase switch
    {
        Value.KindOneofCase.StringValue  => val.StringValue,
        Value.KindOneofCase.IntegerValue => val.IntegerValue.ToString(),
        Value.KindOneofCase.DoubleValue  => val.DoubleValue.ToString(CultureInfo.InvariantCulture),
        Value.KindOneofCase.BoolValue    => val.BoolValue.ToString(),
        _                                => string.Empty
    };
}