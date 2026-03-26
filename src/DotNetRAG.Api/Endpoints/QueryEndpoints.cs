using DotNetRAG.Api.Configuration;
using DotNetRAG.Api.Contracts.Requests;
using DotNetRAG.Api.Contracts.Responses;
using DotNetRAG.Api.Services;
using Microsoft.Extensions.Options;

namespace DotNetRAG.Api.Endpoints;

public static class QueryEndpoints
{
    public static RouteGroupBuilder MapQueryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/query")
            .WithTags("Query");

        group.MapPost("/", HandleQueryAsync)
            .WithName("QueryDocuments")
            .WithSummary("Ask a question against the ingested document corpus")
            .Produces<QueryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> HandleQueryAsync(
        QueryRequest request,
        QueryPipeline pipeline,
        IOptions<RagSettings> settings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return Results.Problem(
                detail: "Question cannot be empty.",
                statusCode: StatusCodes.Status400BadRequest);

        var topK = Math.Clamp(request.TopK ?? settings.Value.DefaultTopK, 1, 20);
        var result = await pipeline.QueryAsync(request.Question, topK, cancellationToken);
        return Results.Ok(result);
    }
}
