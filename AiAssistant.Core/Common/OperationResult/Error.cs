namespace AiAssistant.Domain.Common.OperationResult;

public record struct Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string message) => new("NOT_FOUND", message);
    public static Error Validation(string message) => new("VALIDATION", message);
    public static Error LlmFailure(string message) => new("LLM_FAILURE", message);
    public static Error VectorStoreFailure(string message) => new("VECTOR_STORE_FAILURE", message);
    public static Error DocumentProcessingFailure(string message) => new("DOC_PROCESSING_FAILURE", message);
    public static Error Unexpected(string message) => new("UNEXPECTED", message);
}