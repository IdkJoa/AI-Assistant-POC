using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Interfaces;
using AiAssistant.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;

namespace AiAssistant.Infrastructure.Ollama;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly OllamaApiClient _apiClient;
    private readonly ILogger<OllamaEmbeddingService> _logger;
    private readonly OllamaOptions _options;

    public OllamaEmbeddingService(
        OllamaApiClient apiClient,
        IOptions<OllamaOptions> options,
        ILogger<OllamaEmbeddingService> logger)
    {
        _apiClient = apiClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<float[]>> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _apiClient.EmbedAsync(new EmbedRequest
            {
                Model = _options.EmbeddingModel,
                Input = [text]
            }, cancellationToken);

            var embedding = response.Embeddings.FirstOrDefault();

            return embedding is null or { Length: 0 }
                ? Result<float[]>.Failure(Error.LlmFailure("Ollama returned empty EmbeddingResponse"))
                : Result<float[]>.Success(embedding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding for text of length {Length}", text.Length);
            return Result<float[]>.Failure(Error.LlmFailure(ex.Message));
        }
    }
}