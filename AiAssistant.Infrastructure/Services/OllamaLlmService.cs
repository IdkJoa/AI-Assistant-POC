using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Domain.Agent;
using AiAssistant.Domain.Interfaces;
using AiAssistant.Infrastructure.Configuration;
using DocumentFormat.OpenXml.Office2019.Drawing.Model3D;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace AiAssistant.Infrastructure.Services;

public sealed class OllamaLlmService : ILlmService
{
    private readonly OllamaApiClient _client;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaLlmService> _logger;

    public OllamaLlmService(
        OllamaApiClient client,
        IOptions<OllamaOptions> options,
        ILogger<OllamaLlmService> logger)
    {
        _client  = client;
        _options = options.Value;
        _logger  = logger;
    }

    public async Task<Result<string>> ChatAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new List<Message>
            {
                new() { Role = ChatRole.System, Content = systemPrompt },
                new() { Role = ChatRole.User,   Content = userMessage  }
            };

            var request = new ChatRequest
            {
                Model    = _options.LlmModel,
                Messages = messages,
                Stream   = false
            };

            ChatResponseStream? lastChunk = null;
            await foreach (var chunk in _client.ChatAsync(request, cancellationToken))
                lastChunk = chunk;

            if (lastChunk?.Message?.Content is null)
                return Result<string>.Failure(
                    Error.LlmFailure("Ollama returned an empty response."));

            _logger.LogInformation(
                "LLM response generated. Length: {Length} chars",
                lastChunk.Message.Content.Length);

            return Result<string>.Success(lastChunk.Message.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM chat failed with model {Model}", _options.LlmModel);
            return Result<string>.Failure(Error.LlmFailure(ex.Message));
        }
    }

    public async Task<Result<LlmResponse>> ChatWithToolsAsync(string systemPrompt, string userMessage, IReadOnlyList<ToolDefinition> tools, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}