namespace DotNetRAG.Api.Domain;

public sealed record SimilarityResult
{
    public required EmbeddedChunk EmbeddedChunk { get; init; }
    public required double Score { get; init; }
}
