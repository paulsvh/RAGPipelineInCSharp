namespace DotNetRAG.Api.Contracts.Responses;

public sealed record IngestResponse(
    int DocumentsLoaded,
    int ChunksCreated,
    int ChunksEmbedded,
    TimeSpan Duration);
