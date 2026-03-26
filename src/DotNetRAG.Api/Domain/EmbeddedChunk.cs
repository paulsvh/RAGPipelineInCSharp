namespace DotNetRAG.Api.Domain;

public sealed record EmbeddedChunk
{
    public required DocumentChunk Chunk { get; init; }
    public required float[] Embedding { get; init; }
}
