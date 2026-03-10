using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Domain.Documents;
using AiAssistant.Domain.Domain.ValueObjects;
using AiAssistant.Domain.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;

namespace AiAssistant.Infrastructure.DocumentProcessing;

public class WordDocumentProcessor : IDocumentProcessor
{
    private readonly ILogger<WordDocumentProcessor> _logger;
    
    public WordDocumentProcessor(ILogger<WordDocumentProcessor> logger)
    {
        _logger = logger;
    }

    public bool CanProcess(string contentType) => contentType is
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document" or
        "application/vnd.ms-word";
    
    public Task<Result<IReadOnlyList<DocumentChunk>>> ProcessAsync(
        Stream content, DocumentInfo info, CancellationToken ct = default)
    {
        try
        {
            using var doc = WordprocessingDocument.Open(content, isEditable: false);
            var body = doc.MainDocumentPart?.Document?.Body;

            if (body is null)
            {
                Result<IReadOnlyList<DocumentChunk>>.Failure(Error.DocumentProcessingFailure("Word document has no body."));
            }
            
            var fullText = string.Join(' ', body.Descendants<Text>().Select(t => t.Text));
            var chunks = BuildChunks(fullText, info);

            return Task.FromResult(Result<IReadOnlyList<DocumentChunk>>.Success(chunks));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Word document: {FileName}", info.FileName);
            return Task.FromResult(Result<IReadOnlyList<DocumentChunk>>.Failure(Error.DocumentProcessingFailure(ex.Message)));
        }
    }
    
    private IReadOnlyList<DocumentChunk> BuildChunks(string text, DocumentInfo info) =>
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
                    ["source"]      = "word",
                    ["indexed_at"]  = info.IndexedAt.ToString("O"),
                    ["expiry_date"] = info.ExpiryDate?.ToString("O") ?? string.Empty
                }
            })
            .ToList();
}