namespace AiAssistant.Domain.Tools;

public sealed record DocumentExpiryResult
{
    public required string FileName { get; init; }
    public required DateTime ExpiryDate { get; init; }
    public required ExpiryStatus Status { get; init; }
    public required int DaysUntilExpiry { get; init; }
}

public enum ExpiryStatus
{
    Ok,       // > 30 días
    Warning,  // <= 30 días
    Urgent,   // <= 7 días
    Expired   // ya venció
}