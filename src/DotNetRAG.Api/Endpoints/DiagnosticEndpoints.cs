using DotNetRAG.Api.Configuration;
using DotNetRAG.Api.Contracts.Responses;
using DotNetRAG.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace DotNetRAG.Api.Endpoints;

public static class DiagnosticEndpoints
{
    public static RouteGroupBuilder MapDiagnosticEndpoints(
        this IEndpointRouteBuilder app,
        IWebHostEnvironment env)
    {
        var group = app.MapGroup("/api/diagnostics")
            .WithTags("Diagnostics");

        group.MapGet("/health", HandleHealthAsync)
            .WithName("HealthCheck")
            .WithSummary("Health check with vector store statistics")
            .Produces<HealthResponse>(StatusCodes.Status200OK);

        // Config endpoint exposes operational details — restrict to Development
        if (env.IsDevelopment())
        {
            group.MapGet("/config", HandleConfig)
                .WithName("GetConfig")
                .WithSummary("Get active non-secret configuration (Development only)")
                .Produces<ConfigResponse>(StatusCodes.Status200OK);
        }

        return group;
    }

    private static async Task<IResult> HandleHealthAsync(
        IVectorStore vectorStore,
        CancellationToken cancellationToken)
    {
        var chunkCount = await vectorStore.CountAsync(cancellationToken);
        return Results.Ok(new HealthResponse(
            Status: "Healthy",
            ChunksStored: chunkCount,
            Timestamp: DateTimeOffset.UtcNow));
    }

    private static IResult HandleConfig(
        IOptions<RagSettings> ragSettings,
        IOptions<AnthropicSettings> anthropicSettings)
    {
        var rag = ragSettings.Value;
        var anthropic = anthropicSettings.Value;

        return Results.Ok(new ConfigResponse(
            Rag: new ConfigResponse.RagConfig(
                rag.CorpusDirectory, rag.ChunkSize, rag.ChunkOverlap,
                rag.DefaultTopK, rag.MinSimilarityScore, rag.FileExtensions),
            Anthropic: new ConfigResponse.AnthropicConfig(
                anthropic.Model, anthropic.MaxTokens)));
    }
}
