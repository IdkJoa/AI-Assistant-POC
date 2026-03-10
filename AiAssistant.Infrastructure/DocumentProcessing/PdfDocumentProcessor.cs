using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Domain.Documents;
using AiAssistant.Domain.Domain.ValueObjects;
using AiAssistant.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace AiAssistant.Infrastructure.DocumentProcessing;

public sealed class PdfDocumentProcessor : IDocumentProcessor
{
    private readonly ILogger<PdfDocumentProcessor> _logger;

    public PdfDocumentProcessor(ILogger<PdfDocumentProcessor> logger) => _logger = logger;

    public bool CanProcess(string contentType) =>
        contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

    public Task<Result<IReadOnlyList<DocumentChunk>>> ProcessAsync(
        Stream content,
        DocumentInfo info,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting to process PDF: {Filename}", info.FileName);
            
            using var pdf = PdfDocument.Open(content);

            var fullText = string.Join(' ', pdf.GetPages().Select(p => p.Text));
            var chunks = BuildChunks(fullText, info);

            return Task.FromResult(Result<IReadOnlyList<DocumentChunk>>.Success(chunks));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process PDF: {FileName}", info.FileName);
            return Task.FromResult(
                Result<IReadOnlyList<DocumentChunk>>.Failure(Error.DocumentProcessingFailure(ex.Message)));
        }
    }

    private static IReadOnlyList<DocumentChunk> BuildChunks(string text, DocumentInfo info) =>
        ChunkingService.Chunk(text)
            .Select((content, index) => new DocumentChunk
            {
                Id         = ChunkId.New(),
                DocumentId = info.DocumentId,
                FileName   = info.FileName,
                Content    = content,
                ChunkIndex = index,
                Metadata   = new Dictionary<string, string>
                {
                    ["source"]      = "pdf",
                    ["indexed_at"]  = info.IndexedAt.ToString("O"),
                    ["expiry_date"] = info.ExpiryDate?.ToString("O") ?? string.Empty
                }
            })
            .ToList();
}