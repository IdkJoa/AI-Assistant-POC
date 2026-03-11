using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Domain.Agent;
using AiAssistant.Domain.Interfaces;
using AiAssistant.Infrastructure.Configuration;
using GenerativeAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiAssistant.Infrastructure.Services;

public sealed class GeminiLlmService : ILlmService
{
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiLlmService> _logger;

    public GeminiLlmService(
        IOptions<GeminiOptions> options,
        ILogger<GeminiLlmService> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }
    
    public async Task<Result<string>> ChatAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken ct = default)
    {
        try
        {
            var client   = new GoogleAi(_options.ApiKey);
            var model    = client.CreateGenerativeModel(_options.LlmModel);
            var prompt   = $"{systemPrompt}\n\n{userMessage}";
            var response = await model.GenerateContentAsync(prompt);
            var content  = response.Text;

            if (string.IsNullOrWhiteSpace(content))
                return Result<string>.Failure(
                    Error.LlmFailure("Gemini returned empty response."));

            _logger.LogInformation(
                "Gemini response generated. Length: {Length} chars", content.Length);

            return Result<string>.Success(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini chat failed");
            return Result<string>.Failure(Error.LlmFailure(ex.Message));
        }
    }

    public async Task<Result<LlmResponse>> ChatWithToolsAsync(string systemPrompt, string userMessage, IReadOnlyList<ToolDefinition> tools, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}