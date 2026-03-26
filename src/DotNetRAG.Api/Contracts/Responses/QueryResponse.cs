using System.Text.Json.Serialization;

namespace DotNetRAG.Api.Contracts.Responses;

public sealed record QueryResponse(
    [property: JsonPropertyName("answer")] string Answer,
    [property: JsonPropertyName("sourceChunks")] IReadOnlyList<ChunkReference> SourceChunks,
    [property: JsonPropertyName("modelUsed")] string ModelUsed,
    [property: JsonPropertyName("usage")] QueryUsageInfo Usage);

public sealed record ChunkReference(
    [property: JsonPropertyName("chunkId")] string ChunkId,
    [property: JsonPropertyName("sourceFile")] string SourceFile,
    [property: JsonPropertyName("chunkIndex")] int ChunkIndex,
    [property: JsonPropertyName("similarityScore")] double SimilarityScore,
    [property: JsonPropertyName("textPreview")] string TextPreview);

public sealed record QueryUsageInfo(
    [property: JsonPropertyName("promptTokens")] int PromptTokens,
    [property: JsonPropertyName("completionTokens")] int CompletionTokens,
    [property: JsonPropertyName("chunksRetrieved")] int ChunksRetrieved);
