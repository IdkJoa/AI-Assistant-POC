using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Interfaces;
using AiAssistant.Infrastructure.Configuration;
using GenerativeAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiAssistant.Infrastructure.OpenAi;

public sealed class GeminiEmbeddingService : IEmbeddingService
{
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiEmbeddingService> _logger;

    public GeminiEmbeddingService(
        IOptions<GeminiOptions> options,
        ILogger<GeminiEmbeddingService> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }

    public async Task<Result<float[]>> GenerateAsync(
        string text,
        CancellationToken ct = default)
    {
        try
        {
            var client   = new GoogleAi(_options.ApiKey);
            var model    = client.CreateEmbeddingModel(_options.EmbeddingModel);
            var response = await model.EmbedContentAsync(text);
            var vector   = response.Embedding.Values.Select(v => (float)v).ToArray();

            if (vector.Length == 0)
                return Result<float[]>.Failure(
                    Error.LlmFailure("Gemini returned empty embedding."));

            return Result<float[]>.Success(vector);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini embedding failed");
            return Result<float[]>.Failure(Error.LlmFailure(ex.Message));
        }
    }
}