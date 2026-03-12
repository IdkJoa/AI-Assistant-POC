using System.Text.Json;
using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Domain.Agent;
using AiAssistant.Domain.Domain.Documents;
using AiAssistant.Domain.Interfaces;
using AiAssistant.Domain.MbSuite;
using AiAssistant.Domain.Tools;
using AiAssistant.Infrastructure.Configuration;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Error = AiAssistant.Domain.Common.OperationResult.Error;

namespace AiAssistant.Infrastructure.Services;

public sealed class AgentService : IAgentService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILlmService _llmService;
    private readonly MbSuiteClient _mbSuiteClient;
    private readonly ClaudeOptions _claudeOptions;
    private readonly ILogger<AgentService> _logger;

    private const string SystemPrompt = """
        Eres el asistente de gestión documental de SILDRA Group integrado en MbSuite.

        REGLAS:
        - Solo puedes leer información, nunca modificar datos.
        - Cuando se te proporcione contexto de documentos, responde ÚNICAMENTE basándote en ese contexto.
        - Cuando NO se te proporcione contexto de documentos o NO puedas obtener datos desde la API de MbSuite, responde con tu conocimiento general.
        - Si la información no está en el contexto, indícalo claramente.
        - Responde siempre en español formal.
        - Cuando detectes documentos vencidos o próximos a vencer, sé específico con fechas.
        - Un documento es URGENTE si vence en menos de 7 días.
        - Un documento es CRÍTICO si ya venció.
        - Un documento requiere ATENCIÓN si vence en menos de 30 días.

        FORMATO DE RESPUESTA:
        - Sé conciso y directo.
        - Usa listas cuando presentes múltiples documentos.
        - Siempre indica la fuente (nombre del archivo) de la información.
        """;

    public AgentService(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILlmService llmService,
        MbSuiteClient mbSuiteClient,
        IOptions<ClaudeOptions> claudeOptions,
        ILogger<AgentService> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore      = vectorStore;
        _llmService       = llmService;
        _mbSuiteClient    = mbSuiteClient;
        _claudeOptions    = claudeOptions.Value;
        _logger           = logger;
    }

    public async Task<Result<AgentResponse>> QueryAsync(
    AgentQuery query,
    CancellationToken ct = default)
{
    // RAG
    var embeddingResult = await _embeddingService.GenerateAsync(query.Question, ct);
    if (embeddingResult.IsFailure)
        return Result<AgentResponse>.Failure(embeddingResult.Error);

    var chunksResult = await _vectorStore.SearchAsync(
        embeddingResult.Value!, query.MaxContextChunks, ct);
    if (chunksResult.IsFailure)
        return Result<AgentResponse>.Failure(chunksResult.Error);

    var chunks     = chunksResult.Value!;
    var toolCalls  = new List<ToolCall>();
    var toolResults = new Dictionary<string, string>();

    // tools disponibles
    var availableTools = new List<ToolDefinition>
    {
        new() { Name = "get_borrowers_with_approved_loans", Description = "Obtiene prestatarios con préstamos aprobados" },
        new() { Name = "check_document_expiry",             Description = "Verifica vencimiento de documentos" },
        new() { Name = "get_account_statement",             Description = "Obtiene el estado de cuenta de un party" }
    };

    // primera llamada a Claude con tools
    var userMessage  = BuildUserMessage(query.Question, chunks);
    var llmResult    = await _llmService.ChatWithToolsAsync(
        SystemPrompt, userMessage, availableTools, ct);

    if (llmResult.IsFailure)
        return Result<AgentResponse>.Failure(llmResult.Error);

    var llmResponse = llmResult.Value!;

    // Ejecutar tools si Claude los pidió
    if (llmResponse.HasToolUse)
    {
        foreach (var toolUse in llmResponse.ToolUseRequests)
        {
            _logger.LogInformation("Executing tool: {Tool}", toolUse.ToolName);

            var toolResult = await ExecuteToolAsync(toolUse.ToolName, toolUse.InputJson, chunks, ct);

            toolResults[toolUse.ToolUseId] = toolResult;
            toolCalls.Add(new ToolCall
            {
                ToolName   = toolUse.ToolName,
                Parameters = new Dictionary<string, object> { ["input"] = toolUse.InputJson },
                Result     = toolResult
            });
        }

        // Segunda llamada a Claude con resultados de tools
        var finalResult = await SendToolResultsAsync(
            SystemPrompt, userMessage, llmResponse, toolResults, ct);

        if (finalResult.IsFailure)
            return Result<AgentResponse>.Failure(finalResult.Error);

        return Result<AgentResponse>.Success(new AgentResponse
        {
            Answer       = finalResult.Value!,
            SourceChunks = chunks.Select(c => c.FileName).Distinct().ToList(),
            ToolCalls    = toolCalls,
            GeneratedAt  = DateTime.UtcNow
        });
    }

    // Claude answered without tools
    return Result<AgentResponse>.Success(new AgentResponse
    {
        Answer       = llmResponse.Text ?? "",
        SourceChunks = chunks.Select(c => c.FileName).Distinct().ToList(),
        ToolCalls    = toolCalls,
        GeneratedAt  = DateTime.UtcNow
    });
}

private async Task<string> ExecuteToolAsync(
    string toolName,
    string inputJson,
    IReadOnlyList<DocumentChunk> chunks,
    CancellationToken ct)
{
    return toolName switch
    {
        "get_borrowers_with_approved_loans" => await ExecuteGetBorrowersAsync(ct),
        "check_document_expiry"             => ExecuteCheckDocumentExpiry(chunks),
        "get_account_statement" => await ExecuteGetAccountStatementAsync(inputJson, ct),
        _                                   => $"Tool {toolName} not found."
    };
}

private async Task<string> ExecuteGetBorrowersAsync(CancellationToken ct)
{
    var result = await _mbSuiteClient.GetBorrowersWithApprovedLoansAsync(ct);

    if (result.IsFailure)
        return $"Error: {result.Error.Message}";

    return System.Text.Json.JsonSerializer.Serialize(result.Value);
}

private static string ExecuteCheckDocumentExpiry(IReadOnlyList<DocumentChunk> chunks)
{
    var expiryResults = CheckDocumentExpiryTool.Execute(
        chunks.Select(c => c.Metadata).ToList());

    if (expiryResults.Count == 0)
        return "No se encontraron documentos con vencimientos próximos.";

    return System.Text.Json.JsonSerializer.Serialize(expiryResults);
}

private async Task<string> ExecuteGetAccountStatementAsync(string inputJson, CancellationToken ct)
{
    var input = JsonSerializer.Deserialize<AccountStatementInput>(
        inputJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    if (input is null || string.IsNullOrWhiteSpace(input.PartyId))
        return "Error: partyId es requerido para consultar el estado de cuenta.";

    var result = await _mbSuiteClient.GetAccountStatementAsync(
        input.PartyId,
        input.CurrencyCode,
        input.PageNumber,
        input.PageSize,
        ct);

    if (result.IsFailure)
        return $"Error al obtener estado de cuenta: {result.Error.Message}";

    var statement = result.Value!;
    var sb        = new System.Text.StringBuilder();

    sb.AppendLine($"Estado de cuenta | Party: {input.PartyId}");
    sb.AppendLine($"Página {statement.PageNumber} de {statement.TotalPages} | Total registros: {statement.PageCount}");
    sb.AppendLine();

    foreach (var tx in statement.Data)
    {
        sb.AppendLine($"[{tx.TransactionDate[..10]}] {tx.TransactionType}");
        sb.AppendLine($"  Ref: {tx.ReferenceNo} | {tx.CurrencyCode}");
        sb.AppendLine($"  Débito: {tx.DebitAmount:N2} | Crédito: {tx.CreditAmount:N2} | Balance: {tx.RunningBalance:N2}");
        sb.AppendLine($"  Descripción: {tx.Description}");
        sb.AppendLine();
    }

    return sb.ToString();
}

private sealed record AccountStatementInput(
    string PartyId,
    string? CurrencyCode = null,
    int PageNumber = 1,
    int PageSize   = 10
);

private async Task<Result<string>> SendToolResultsAsync(
    string systemPrompt,
    string userMessage,
    LlmResponse llmResponse,
    Dictionary<string, string> toolResults,
    CancellationToken ct)
{
    try
    {
        var client = new AnthropicClient(_claudeOptions.ApiKey);

        // Rebuild full history for claude
        var messages = new List<Message>
        {
            new()
            {
                Role    = RoleType.User,
                Content = [new TextContent { Text = userMessage }]
            },
            new()
            {
                Role    = RoleType.Assistant,
                Content = llmResponse.ToolUseRequests
                    .Select(t => (ContentBase)new ToolUseContent
                    {
                        Id    = t.ToolUseId,
                        Name  = t.ToolName,
                        Input = System.Text.Json.Nodes.JsonNode.Parse(t.InputJson)
                    })
                    .ToList()
            },
            new()
            {
                Role    = RoleType.User,
                Content = toolResults
                    .Select(kvp => (ContentBase)new ToolResultContent
                    {
                        ToolUseId = kvp.Key,
                        Content   = new List<ContentBase>
                        {
                            new TextContent { Text = kvp.Value }
                        }
                    })
                    .ToList()
            }
        };

        var request = new MessageParameters
        {
            Model     = _claudeOptions.LlmModel,
            MaxTokens = 8096,
            System    = [new SystemMessage(systemPrompt)],
            Messages  = messages
        };

        var response = await client.Messages.GetClaudeMessageAsync(request, ct);
        var text     = response.Content.OfType<TextContent>().FirstOrDefault()?.Text;

        return string.IsNullOrWhiteSpace(text)
            ? Result<string>.Failure(Error.LlmFailure("Claude returned empty final response."))
            : Result<string>.Success(text);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "SendToolResults failed");
        return Result<string>.Failure(Error.LlmFailure(ex.Message));
    }
}

private static string BuildUserMessage(string question, IReadOnlyList<DocumentChunk> chunks)
{
    var sb = new System.Text.StringBuilder();

    if (chunks.Count > 0)
    {
        sb.AppendLine("=== CONTEXTO DE DOCUMENTOS ===");
        foreach (var chunk in chunks)
        {
            sb.AppendLine($"[Archivo: {chunk.FileName} | Fragmento: {chunk.ChunkIndex}]");
            sb.AppendLine(chunk.Content);
            sb.AppendLine();
        }
    }

    sb.AppendLine("=== PREGUNTA ===");
    sb.AppendLine(question);

    return sb.ToString();
}
}