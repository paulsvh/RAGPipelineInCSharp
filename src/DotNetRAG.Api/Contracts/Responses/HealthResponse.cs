using System.Text.Json.Serialization;

namespace DotNetRAG.Api.Contracts.Responses;

public sealed record HealthResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("chunksStored")] int ChunksStored,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp);
