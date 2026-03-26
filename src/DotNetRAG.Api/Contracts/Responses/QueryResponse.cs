namespace DotNetRAG.Api.Contracts.Responses;

public sealed record QueryResponse(
    string Answer,
    IReadOnlyList<ChunkReference> SourceChunks,
    string ModelUsed,
    QueryUsageInfo Usage);

public sealed record ChunkReference(
    string ChunkId,
    string SourceFile,
    int ChunkIndex,
    double SimilarityScore,
    string TextPreview);

public sealed record QueryUsageInfo(
    int PromptTokens,
    int CompletionTokens,
    int ChunksRetrieved);
