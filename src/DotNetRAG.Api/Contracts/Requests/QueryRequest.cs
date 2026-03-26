using System.Text.Json.Serialization;

namespace DotNetRAG.Api.Contracts.Requests;

public sealed record QueryRequest(
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("topK")] int? TopK = null);
