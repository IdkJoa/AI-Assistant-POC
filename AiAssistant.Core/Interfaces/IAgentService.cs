using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Domain.Agent;

namespace AiAssistant.Domain.Interfaces;

public interface IAgentService
{
    Task<Result<AgentResponse>> QueryAsync(AgentQuery query, CancellationToken ct = default);
}