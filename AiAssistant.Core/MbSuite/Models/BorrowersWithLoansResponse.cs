using System.Text.Json.Serialization;

namespace AiAssistant.Domain.MbSuite.Models;

public sealed record BorrowerWithLoansResponse
{
    [JsonPropertyName("borrowerId")]        public string BorrowerId { get; init; } = "";
    [JsonPropertyName("borrowerNumber")]    public string BorrowerNumber { get; init; } = "";
    [JsonPropertyName("firstName")]         public string FirstName { get; init; } = "";
    [JsonPropertyName("lastName")]          public string LastName { get; init; } = "";
    [JsonPropertyName("riskCategory")]      public string RiskCategory { get; init; } = "";
    [JsonPropertyName("creditScore")]       public int CreditScore { get; init; }
    [JsonPropertyName("creditLimit")]       public decimal CreditLimit { get; init; }
    [JsonPropertyName("totalExposure")]     public decimal TotalExposure { get; init; }
    [JsonPropertyName("primaryCity")]       public string PrimaryCity { get; init; } = "";
    [JsonPropertyName("primaryProvince")]   public string PrimaryProvince { get; init; } = "";
    [JsonPropertyName("approvedLoans")]     public List<ApprovedLoan> ApprovedLoans { get; init; } = [];
}

public sealed record ApprovedLoan
{
    [JsonPropertyName("loanId")]                    public string LoanId { get; init; } = "";
    [JsonPropertyName("loanNumber")]                public string LoanNumber { get; init; } = "";
    [JsonPropertyName("principal")]                 public decimal Principal { get; init; }
    [JsonPropertyName("principalBalance")]          public decimal PrincipalBalance { get; init; }
    [JsonPropertyName("interestBalance")]           public decimal InterestBalance { get; init; }
    [JsonPropertyName("penaltyBalance")]            public decimal PenaltyBalance { get; init; }
    [JsonPropertyName("currency")]                  public string Currency { get; init; } = "";
    [JsonPropertyName("pendingInstallments")]       public List<PendingInstallment> PendingInstallments { get; init; } = [];
    [JsonPropertyName("pendingTransactionsTotal")]  public decimal PendingTransactionsTotal { get; init; }
}

public sealed record PendingInstallment
{
    [JsonPropertyName("installmentId")]  public int InstallmentId { get; init; }
    [JsonPropertyName("dueDate")]        public string DueDate { get; init; } = "";
    [JsonPropertyName("totalAmount")]    public decimal TotalAmount { get; init; }
    [JsonPropertyName("penaltyAmount")]  public decimal PenaltyAmount { get; init; }
    [JsonPropertyName("originalAmount")] public decimal OriginalAmount { get; init; }
}