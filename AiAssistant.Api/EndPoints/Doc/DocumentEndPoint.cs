
using AiAssistant.Domain.Domain.Documents;
using AiAssistant.Domain.Domain.ValueObjects;
using AiAssistant.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AiAssistants.API.EndPoints.DocumentEndPoint
{
    public static class DocumentEndPoint
    {
        private const long maxFileSize = 50 * 1024 * 1024;
        private const int MaxBatchFiles = 20;
        private static readonly string[] SupportedTypes =
            ["application/pdf", "text/plain", "text/markdown", "application/octet-stream"];

        public static void MapDocumentEndpoints(this WebApplication application)
        {
            var group = application.MapGroup("/api/documents")
                .WithName("Documents")
                .WithOpenApi();

            group.MapPost("/upload", UploadDocumentAsync)
                .WithName("UploadDocument")
                .WithDescription("Carga un documento en la base de datos vectorial. El documento será procesado en chunks y vectorizado.")
                .WithSummary("Subir documento").DisableAntiforgery();
 
        }

        private static async Task<IResult> UploadDocumentAsync(
                IFormFile file,
                [FromQuery] DateTime? expiryDate,
                [FromServices] IEnumerable<IDocumentProcessor> processors,
                [FromServices] IEmbeddingService embeddingService,
                [FromServices] IVectorStore vectorStore,
                CancellationToken cancellationToken)
        {
            // Validar que el archivo no sea nulo o vacío
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "El archivo no puede estar vacío",
                    error = "EmptyFile",
                    timestamp = DateTime.UtcNow
                });
            }


            if (file.Length > maxFileSize)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "El archivo excede el tamaño máximo de 50 MB",
                    error = "FileTooLarge",
                    maxSizeInMB = maxFileSize / (1024 * 1024),
                    fileSizeInMB = (double)file.Length / (1024 * 1024),
                    timestamp = DateTime.UtcNow
                });
            }

            // Validar formato del archivo
            var documentProcessor = processors.FirstOrDefault(p => p.CanProcess(file.ContentType));
            if (documentProcessor == null)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = $"El tipo de contenido '{file.ContentType}' no es soportado",
                    error = "UnsupportedContentType",
                    supportedTypes = new[] { "application/pdf", "text/plain", "text/markdown", "application/octet-stream" },
                    providedContentType = file.ContentType,
                    timestamp = DateTime.UtcNow
                });
            }

            try
            {

                // Crear información del documento
                var documentId = DocumentId.New();
                var documentInfo = BuildDocumentInfo(documentId, file, expiryDate);

                // Leer el contenido del archivo
                using var stream = file.OpenReadStream();

                // Procesar el documento en chunks
                var processResult = await documentProcessor.ProcessAsync(stream, documentInfo, cancellationToken);

                if (processResult.IsFailure)
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        message = "Error al procesar el documento",
                        error = processResult.Error.Code,
                        details = processResult.Error.Message,
                        timestamp = DateTime.UtcNow
                    });
                }

                var rawchunks = processResult.Value;

                // Generar embeddings para cada chunk

                var embeddingTasks = rawchunks.Select(async chunk =>
                {
                    var embedResult = await embeddingService.GenerateAsync(chunk.Content, cancellationToken);
                    return embedResult.IsSuccess ? chunk with { Embedding = embedResult.Value } : null;
                });

                var embeddedChunks = (await Task.WhenAll(embeddingTasks)).ToList();
                // Almacenar chunks en la base de datos vectorial
                var upsertResult = await vectorStore.UpsertAsync(embeddedChunks, cancellationToken);

                if (upsertResult.IsFailure)
                {
                    return Results.InternalServerError(new
                    {
                        success = false,
                        message = "Error al almacenar el documento en la base de datos vectorial",
                        error = upsertResult.Error.Code,
                        details = upsertResult.Error.Message,
                        timestamp = DateTime.UtcNow
                    });
                }

                return Results.Accepted(
                    $"/api/documents/{documentId}",
                    new
                    {
                        success = true,
                        message = "Documento cargado exitosamente",
                        documentId = documentId.ToString(),
                        fileName = file.FileName,
                        contentType = file.ContentType,
                        chunkCount = embeddedChunks.Count,
                        indexedAt = documentInfo.IndexedAt,
                        expiryDate = expiryDate,
                        fileSize = file.Length,
                        timestamp = DateTime.UtcNow
                    });
            }
            catch (OperationCanceledException)
            {
                return Results.Conflict(new
                {
                    success = false,
                    message = "La operación fue cancelada",
                    error = "OperationCanceled",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception exception)
            {
                return Results.InternalServerError(new
                {
                    success = false,
                    message = "Error interno del servidor al procesar el documento",
                    error = "InternalError",
                    details = exception.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
 
        // ── Helpers ────────────────────────────────────────────────────────────────

        private static DocumentInfo BuildDocumentInfo(DocumentId documentId, IFormFile file, DateTime? expiryDate) =>
            new()
            {
                DocumentId = documentId,
                FileName = file.FileName,
                ContentType = file.ContentType,
                IndexedAt = DateTime.UtcNow,
                ExpiryDate = expiryDate,
                // Metadata simplificada: sin duplicar datos ya en DocumentInfo
                Metadata = new Dictionary<string, string>
                {
                    { "fileSize", file.Length.ToString() },
                    { "uploadedAt", DateTime.UtcNow.ToString("O") },
                    { "expiryDate", expiryDate?.ToString("O") ?? "never" }
                }
            };
 
         
    }

}
