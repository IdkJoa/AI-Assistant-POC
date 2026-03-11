using System.Text.Json;
using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.MbSuite.Models;
using Microsoft.Extensions.Logging;

namespace AiAssistant.Infrastructure.Services;

public sealed class MbSuiteClient
{
    private readonly HttpClient _http;
    private readonly ILogger<MbSuiteClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MbSuiteClient(HttpClient http, ILogger<MbSuiteClient> logger)
    {
        _logger = logger;
        _http   = http;
    }

    public async Task<Result<List<BorrowerWithLoansResponse>>> GetBorrowersWithApprovedLoansAsync(
        CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("api/borrowers/with-approved-loans", ct);

            if (!response.IsSuccessStatusCode)
            {
                var r = await _http.GetAsync("api/borrowers/with-approved-loans", ct);
                var body     = await response.Content.ReadAsStringAsync(ct);
                
                _logger.LogWarning(
                    "MbSuite GetBorrowersWithApprovedLoans failed. Status: {Status}", response.StatusCode);

                _logger.LogInformation("Status: {Status} | Body: {Body}", r.StatusCode, body);

                return Result<List<BorrowerWithLoansResponse>>.Failure(
                    Error.NotFound($"Failed to get borrowers. Status: {response.StatusCode}"));
            }

            var content   = await response.Content.ReadAsStringAsync(ct);
            var borrowers = JsonSerializer.Deserialize<List<BorrowerWithLoansResponse>>(
                content, JsonOptions);

            return borrowers is null
                ? Result<List<BorrowerWithLoansResponse>>.Failure(
                    Error.LlmFailure("Failed to deserialize borrowers response."))
                : Result<List<BorrowerWithLoansResponse>>.Success(borrowers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MbSuite GetBorrowersWithApprovedLoans failed");
            return Result<List<BorrowerWithLoansResponse>>.Failure(
                Error.LlmFailure(ex.Message));
        }
    }
}