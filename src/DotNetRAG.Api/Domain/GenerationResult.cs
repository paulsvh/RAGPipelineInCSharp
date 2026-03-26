namespace DotNetRAG.Api.Domain;

public sealed record GenerationResult(
    string Answer,
    IReadOnlyList<ChunkCitation> Citations,
    string ModelUsed,
    int PromptTokens,
    int CompletionTokens);

public sealed record ChunkCitation(
    string ChunkId,
    string SourceFile,
    int ChunkIndex,
    double SimilarityScore);
