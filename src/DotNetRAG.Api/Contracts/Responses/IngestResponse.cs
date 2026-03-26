using System.Text.Json.Serialization;

namespace DotNetRAG.Api.Contracts.Responses;

public sealed record IngestResponse(
    [property: JsonPropertyName("documentsLoaded")] int DocumentsLoaded,
    [property: JsonPropertyName("chunksCreated")] int ChunksCreated,
    [property: JsonPropertyName("chunksEmbedded")] int ChunksEmbedded,
    [property: JsonPropertyName("duration")] TimeSpan Duration);
