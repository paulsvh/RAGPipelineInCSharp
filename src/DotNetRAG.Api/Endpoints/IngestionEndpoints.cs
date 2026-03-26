using System.Text.Json;
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

        group.MapGet("/corpora", HandleListCorporaAsync)
            .WithName("ListCorpora")
            .WithSummary("List available demo corpora")
            .Produces<IReadOnlyList<CorpusInfo>>(StatusCodes.Status200OK);

        group.MapPost("/", HandleIngestAsync)
            .WithName("IngestDocuments")
            .WithSummary("Load a demo corpus into the vector store by ID")
            .Produces<IngestResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/", HandleClearAsync)
            .WithName("ClearStore")
            .WithSummary("Clear all documents from the vector store")
            .Produces(StatusCodes.Status204NoContent);

        return group;
    }

    private static async Task<IResult> HandleListCorporaAsync(
        IOptions<RagSettings> settings,
        IWebHostEnvironment env,
        CancellationToken cancellationToken)
    {
        var corpusRoot = ResolveCorpusRoot(settings.Value, env);
        if (!Directory.Exists(corpusRoot))
            return Results.Ok(Array.Empty<CorpusInfo>());

        var corpora = new List<CorpusInfo>();
        foreach (var subDir in Directory.GetDirectories(corpusRoot))
        {
            var metaPath = Path.Combine(subDir, "_meta.json");
            if (!File.Exists(metaPath)) continue;

            var json = await File.ReadAllTextAsync(metaPath, cancellationToken);
            var meta = JsonSerializer.Deserialize<JsonElement>(json);

            corpora.Add(new CorpusInfo(
                Id: Path.GetFileName(subDir),
                Name: meta.TryGetProperty("name", out var n) ? n.GetString() ?? "" : Path.GetFileName(subDir),
                Description: meta.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                Documents: meta.TryGetProperty("documents", out var c) ? c.GetInt32() : 0));
        }

        return Results.Ok(corpora);
    }

    private static async Task<IResult> HandleIngestAsync(
        IngestRequest? request,
        IngestionPipeline pipeline,
        IVectorStore vectorStore,
        IOptions<RagSettings> settings,
        IWebHostEnvironment env,
        CancellationToken cancellationToken)
    {
        var corpusId = request?.CorpusId;
        if (string.IsNullOrWhiteSpace(corpusId))
            return Results.Problem(
                detail: "A corpus ID is required. Use GET /api/ingest/corpora to list available corpora.",
                statusCode: StatusCodes.Status400BadRequest);

        // Only allow simple directory names — no path separators
        if (corpusId.Contains('/') || corpusId.Contains('\\') || corpusId.Contains(".."))
            return Results.Problem(
                detail: "Invalid corpus ID.",
                statusCode: StatusCodes.Status400BadRequest);

        var corpusRoot = ResolveCorpusRoot(settings.Value, env);
        var directory = Path.Combine(corpusRoot, corpusId);

        if (!Directory.Exists(directory))
            return Results.Problem(
                detail: $"Corpus '{corpusId}' not found.",
                statusCode: StatusCodes.Status404NotFound);

        // Clear existing data before loading new corpus
        await vectorStore.ClearAsync(cancellationToken);

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

    private static string ResolveCorpusRoot(RagSettings settings, IWebHostEnvironment env)
    {
        return Path.GetFullPath(Path.Combine(env.ContentRootPath, settings.CorpusDirectory))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
