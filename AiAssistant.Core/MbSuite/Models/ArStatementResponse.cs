using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiAssistant.Domain.MbSuite.Models;

public sealed record ArStatementResponse
{
    [JsonPropertyName("data")]         public List<StatementTransaction> Data { get; init; } = [];
    [JsonPropertyName("pageNumber")]   public int PageNumber { get; init; }
    [JsonPropertyName("pageSize")]     public int PageSize { get; init; }
    [JsonPropertyName("pageCount")]    public int PageCount { get; init; }
    [JsonPropertyName("totalPages")] public int TotalPages { get; init; }
}

public sealed record StatementTransaction
{
    [JsonPropertyName("transactionId")]     public int TransactionId { get; init; }
    [JsonPropertyName("transactionType")]   public string TransactionType { get; init; } = "";
    [JsonPropertyName("transactionDate")]   public string TransactionDate { get; init; } = "";
    [JsonPropertyName("currencyCode")]      public string CurrencyCode { get; init; } = "";
    [JsonPropertyName("debitAmount")]       public decimal DebitAmount { get; init; }
    [JsonPropertyName("creditAmount")]      public decimal CreditAmount { get; init; }
    [JsonPropertyName("runningBalance")]    public decimal RunningBalance { get; init; }
    [JsonPropertyName("referenceNo")]       public string ReferenceNo { get; init; } = "";
    [JsonPropertyName("description")]       public string Description { get; init; } = "";
}