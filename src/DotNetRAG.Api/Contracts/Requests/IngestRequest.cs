using System.Text.Json.Serialization;

namespace DotNetRAG.Api.Contracts.Requests;

public sealed record IngestRequest(
    [property: JsonPropertyName("directoryPath")] string? DirectoryPath = null);
