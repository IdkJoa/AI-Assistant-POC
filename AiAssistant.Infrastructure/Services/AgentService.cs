using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Domain.Agent;
using AiAssistant.Domain.Interfaces;
using AiAssistant.Domain.Tools;
using AiAssistant.Infrastructure.Ollama;
using Microsoft.Extensions.Logging;

namespace AiAssistant.Domain.Services;

public sealed class AgentService : IAgentService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly OllamaLlmService _llmService;
    private readonly ILogger<AgentService> _logger;

    private const string SystemPrompt = """
        Eres el asistente de gestión documental de SILDRA Group integrado en MbSuite.

        REGLAS:
        - Solo puedes leer información, nunca modificar datos.
        - Responde únicamente basándote en el contexto provisto.
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
        OllamaLlmService llmService,
        ILogger<AgentService> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore      = vectorStore;
        _llmService       = llmService;
        _logger           = logger;
    }

    public async Task<Result<AgentResponse>> QueryAsync(
        AgentQuery query,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Processing agent query: {Question}", query.Question);

        // ── Paso 1: Convertir la pregunta en embedding ─────────────────────
        var embedResult = await _embeddingService.GenerateAsync(query.Question, ct);
        if (embedResult.IsFailure)
            return Result<AgentResponse>.Failure(embedResult.Error);

        // ── Paso 2: Buscar chunks relevantes en Qdrant ─────────────────────
        var searchResult = await _vectorStore.SearchAsync(
            embedResult.Value!,
            query.MaxContextChunks,
            ct);

        if (searchResult.IsFailure)
            return Result<AgentResponse>.Failure(searchResult.Error);

        var chunks = searchResult.Value!;

        if (chunks.Count == 0)
        {
            _logger.LogWarning("No relevant chunks found for query: {Question}", query.Question);
            return Result<AgentResponse>.Success(new AgentResponse
            {
                Answer       = "No encontré información relevante en los documentos indexados para responder tu consulta.",
                SourceChunks = [],
                ToolCalls    = [],
                GeneratedAt  = DateTime.UtcNow
            });
        }

        // ── Paso 3: Ejecutar Tools ─────────────────────────────────────────
        var toolCalls = new List<ToolCall>();

        var expiryResults = CheckDocumentExpiryTool.Execute(
            chunks.Select(c => c.Metadata).ToList());

        if (expiryResults.Count > 0)
        {
            toolCalls.Add(new ToolCall
            {
                ToolName   = nameof(CheckDocumentExpiryTool),
                Parameters = new Dictionary<string, object>
                {
                    ["chunks_analyzed"] = chunks.Count
                },
                Result = FormatExpiryResults(expiryResults)
            });
        }

        EmailDraft? emailDraft = null;
        if (expiryResults.Any(r => r.Status != ExpiryStatus.Ok))
        {
            emailDraft = DraftEmailTool.Execute(expiryResults);

            if (emailDraft is not null)
                toolCalls.Add(new ToolCall
                {
                    ToolName   = nameof(DraftEmailTool),
                    Parameters = new Dictionary<string, object>
                    {
                        ["affected_documents"] = expiryResults.Count
                    },
                    Result = $"Borrador generado para {emailDraft.AffectedDocuments.Count} documento(s)"
                });
        }

        // ── Paso 4: Construir el prompt con contexto ───────────────────────
        var userMessage = BuildUserMessage(query.Question, chunks, expiryResults);

        // ── Paso 5: Llamar al LLM ──────────────────────────────────────────
        var llmResult = await _llmService.ChatAsync(SystemPrompt, userMessage, ct);
        if (llmResult.IsFailure)
            return Result<AgentResponse>.Failure(llmResult.Error);

        // ── Paso 6: Construir y devolver la respuesta ──────────────────────
        return Result<AgentResponse>.Success(new AgentResponse
        {
            Answer       = llmResult.Value!,
            SourceChunks = chunks.Select(c => c.FileName).Distinct().ToList(),
            ToolCalls    = toolCalls,
            EmailDraft   = emailDraft,
            GeneratedAt  = DateTime.UtcNow
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildUserMessage(
        string question,
        IEnumerable<DocumentChunk> chunks,
        IReadOnlyList<DocumentExpiryResult> expiryResults)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("=== CONTEXTO DE DOCUMENTOS ===");
        foreach (var chunk in chunks)
        {
            sb.AppendLine($"[Archivo: {chunk.FileName} | Fragmento: {chunk.ChunkIndex}]");
            sb.AppendLine(chunk.Content);
            sb.AppendLine();
        }

        if (expiryResults.Count > 0)
        {
            sb.AppendLine("=== RESULTADO DE ANÁLISIS DE VENCIMIENTOS ===");
            sb.AppendLine(FormatExpiryResults(expiryResults));
        }

        sb.AppendLine("=== PREGUNTA DEL USUARIO ===");
        sb.AppendLine(question);

        return sb.ToString();
    }

    private static string FormatExpiryResults(IReadOnlyList<DocumentExpiryResult> results)
    {
        var sb = new System.Text.StringBuilder();

        foreach (var r in results)
        {
            var statusLabel = r.Status switch
            {
                ExpiryStatus.Expired => $"❌ VENCIDO hace {Math.Abs(r.DaysUntilExpiry)} días",
                ExpiryStatus.Urgent  => $"⚠️ URGENTE - vence en {r.DaysUntilExpiry} días",
                ExpiryStatus.Warning => $"📋 ATENCIÓN - vence en {r.DaysUntilExpiry} días",
                _                    => "✅ OK"
            };

            sb.AppendLine($"- {r.FileName}: {statusLabel} ({r.ExpiryDate:dd/MM/yyyy})");
        }

        return sb.ToString();
    }
}