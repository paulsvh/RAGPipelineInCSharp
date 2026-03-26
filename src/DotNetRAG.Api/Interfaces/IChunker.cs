using DotNetRAG.Api.Domain;

namespace DotNetRAG.Api.Interfaces;

/// <summary>
/// Splits document content into overlapping chunks for embedding.
/// </summary>
public interface IChunker
{
    /// <summary>
    /// Splits <paramref name="content"/> into chunks, each tagged with <paramref name="sourcePath"/>.
    /// </summary>
    IReadOnlyList<DocumentChunk> Chunk(string content, string sourcePath);
}
