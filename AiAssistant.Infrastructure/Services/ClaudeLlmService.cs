using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Domain.Agent;
using AiAssistant.Domain.Interfaces;
using AiAssistant.Infrastructure.Claude;
using AiAssistant.Infrastructure.Configuration;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Error = AiAssistant.Domain.Common.OperationResult.Error;

namespace AiAssistant.Infrastructure.Services;

public sealed class ClaudeLlmService : ILlmService
{
    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeLlmService> _logger;

    public ClaudeLlmService(
        IOptions<ClaudeOptions> options,
        ILogger<ClaudeLlmService> logger)
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
            var client  = new AnthropicClient(_options.ApiKey);
            var request = new MessageParameters
            {
                Model     = _options.LlmModel,
                MaxTokens = 1024,
                System    = [new SystemMessage(systemPrompt)],
                Messages  =
                [
                    new Message
                    {
                        Role    = RoleType.User,
                        Content = [new TextContent { Text = userMessage }]
                    }
                ]
            };

            var response = await client.Messages.GetClaudeMessageAsync(request, ct);
            var content  = response.Content.OfType<TextContent>().FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(content))
                return Result<string>.Failure(Error.LlmFailure("Claude returned empty response."));

            _logger.LogInformation("Claude response generated. Length: {Length} chars", content.Length);
            return Result<string>.Success(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude chat failed");
            return Result<string>.Failure(Error.LlmFailure(ex.Message));
        }
    }

    public async Task<Result<LlmResponse>> ChatWithToolsAsync(
        string systemPrompt,
        string userMessage,
        IReadOnlyList<ToolDefinition> tools,
        CancellationToken ct = default)
    {
        try
        {
            var client      = new AnthropicClient(_options.ApiKey);
            var claudeTools = ClaudeToolDefinitions.GetAll()
                .Where(t => tools.Any(td => td.Name == t.Function.Name))
                .ToList();
            
            var request = new MessageParameters
            {
                Model     = _options.LlmModel,
                MaxTokens = 2048,
                System    = [new SystemMessage(systemPrompt)],
                Tools     = claudeTools,
                Messages  =
                [
                    new Message
                    {
                        Role    = RoleType.User,
                        Content = [new TextContent { Text = userMessage }]
                    }
                ]
            };

            var response = await client.Messages.GetClaudeMessageAsync(request, ct);

            if (response.StopReason == "tool_use")
            {
                var toolUseRequests = response.Content
                    .OfType<ToolUseContent>()
                    .Select(t => new ToolUseRequest
                    {
                        ToolUseId = t.Id,
                        ToolName  = t.Name,
                        InputJson = t.Input?.ToString() ?? "{}"
                    })
                    .ToList();

                return Result<LlmResponse>.Success(new LlmResponse
                {
                    ToolUseRequests = toolUseRequests
                });
            }

            var text = response.Content.OfType<TextContent>().FirstOrDefault()?.Text;
            return Result<LlmResponse>.Success(new LlmResponse { Text = text });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude ChatWithTools failed");
            return Result<LlmResponse>.Failure(Error.LlmFailure(ex.Message));
        }
    }
}