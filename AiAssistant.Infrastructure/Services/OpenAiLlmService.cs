using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Domain.Agent;
using AiAssistant.Domain.Interfaces;
using AiAssistant.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace AiAssistant.Infrastructure.Services;

public sealed class OpenAiLlmService : ILlmService
{
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiLlmService> _logger;

    public OpenAiLlmService(
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiLlmService> logger)
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
            var client = new OpenAIClient(_options.ApiKey);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            };

            var response = await client
                .GetChatClient(_options.LlmModel)
                .CompleteChatAsync(messages, cancellationToken: ct);

            var content = response.Value.Content[0].Text;
            return Result<string>.Success(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI chat failed");
            return Result<string>.Failure(Error.LlmFailure(ex.Message));
        }
    }

    public async Task<Result<LlmResponse>> ChatWithToolsAsync(string systemPrompt, string userMessage, IReadOnlyList<ToolDefinition> tools, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}