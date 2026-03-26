namespace DotNetRAG.Api.Contracts.Responses;

public sealed record HealthResponse(
    string Status,
    int ChunksStored,
    DateTimeOffset Timestamp);
