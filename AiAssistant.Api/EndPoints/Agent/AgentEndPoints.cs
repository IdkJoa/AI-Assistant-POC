using AiAssistant.Domain.Common.OperationResult;
using AiAssistant.Domain.Domain.Agent;
using AiAssistant.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AiAssistant.Api.EndPoints.Agent;

public static class AgentEndPoints
{
    public static void MapAgentEndPoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/agent").WithTags("Agent").WithOpenApi();


        group.MapPost("/query", async ([FromBody] AgentQuery request,
        [FromServices] IAgentService agentService, CancellationToken ct) =>
        {

            //Create the AgentQuery object from the request
            var query = new AgentQuery{
                 Question = request.Question,
                 DocumentId = request.DocumentId,
                 MaxContextChunks = request.MaxContextChunks
            };

            var result = await agentService.QueryAsync(query, ct);

            if(!result.IsSuccess)
            {
                return Results.BadRequest(Error.LlmFailure($"Failed to get response from the agent {result.Error}"));
            }

            return Results.Ok(result);      
         }).WithName("QueryAgent").WithDescription("Query the agent with a question and get a response based on the provided document context.");
    }
}