namespace AiAssistant.Infrastructure.Configuration;

public sealed class MbSuiteOptions
{
    public const string SectionName = "MbSuite";
    public required string BaseUrl { get; init; }
    public required string AccessToken { get; init; }
    public required string OrganizationId { get; init; }
    public required string BranchId { get; init; }
}