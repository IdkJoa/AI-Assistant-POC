using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Interfaces;
using AiAssistant.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;

namespace AiAssistant.Infrastructure.OpenAI;

public sealed class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiEmbeddingService> _logger;

    public OpenAiEmbeddingService(
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiEmbeddingService> logger)
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
            var client   = new OpenAIClient(_options.ApiKey);
            var response = await client
                .GetEmbeddingClient(_options.EmbeddingModel)
                .GenerateEmbeddingAsync(text, cancellationToken: ct);

            var vector = response.Value.ToFloats().ToArray();
            return Result<float[]>.Success(vector);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI embedding failed");
            return Result<float[]>.Failure(Error.LlmFailure(ex.Message));
        }
    }
}