namespace DotNetRAG.Api.Domain;

public sealed record DocumentChunk
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public required string SourcePath { get; init; }
    public required int ChunkIndex { get; init; }
    public required int StartCharOffset { get; init; }
    public required int EndCharOffset { get; init; }
}
