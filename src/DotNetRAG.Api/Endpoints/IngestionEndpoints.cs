using DotNetRAG.Api.Configuration;
using DotNetRAG.Api.Contracts.Requests;
using DotNetRAG.Api.Contracts.Responses;
using DotNetRAG.Api.Interfaces;
using DotNetRAG.Api.Services;
using Microsoft.Extensions.Options;

namespace DotNetRAG.Api.Endpoints;

public static class IngestionEndpoints
{
    public static RouteGroupBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ingest")
            .WithTags("Ingestion");

        group.MapPost("/", HandleIngestAsync)
            .WithName("IngestDocuments")
            .WithSummary("Ingest documents from a directory into the vector store")
            .Produces<IngestResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/", HandleClearAsync)
            .WithName("ClearStore")
            .WithSummary("Clear all documents from the vector store")
            .Produces(StatusCodes.Status204NoContent);

        return group;
    }

    private static async Task<IResult> HandleIngestAsync(
        IngestRequest? request,
        IngestionPipeline pipeline,
        IOptions<RagSettings> settings,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetFullPath(request?.DirectoryPath ?? settings.Value.CorpusDirectory);

        // Prevent path traversal — restrict to the configured corpus root
        var allowedRoot = Path.GetFullPath(settings.Value.CorpusDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!directory.StartsWith(allowedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !directory.Equals(allowedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(
                detail: "Directory path must be within the configured corpus directory.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var result = await pipeline.IngestAsync(directory, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleClearAsync(
        IVectorStore vectorStore,
        CancellationToken cancellationToken)
    {
        await vectorStore.ClearAsync(cancellationToken);
        return Results.NoContent();
    }
}
