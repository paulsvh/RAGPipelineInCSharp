using System.Text.Json.Serialization;

namespace DotNetRAG.Api.Contracts.Requests;

public sealed record IngestRequest(
    [property: JsonPropertyName("corpusId")] string? CorpusId = null);
