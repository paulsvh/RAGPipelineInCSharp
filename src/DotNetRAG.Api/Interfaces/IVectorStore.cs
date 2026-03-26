using DotNetRAG.Api.Domain;

namespace DotNetRAG.Api.Interfaces;

/// <summary>
/// Stores and searches embedded document chunks. Implementations can range from
/// in-memory stores to external vector databases (Pinecone, Weaviate, Qdrant).
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Inserts or updates embedded chunks in the store.
    /// </summary>
    Task UpsertAsync(
        IReadOnlyList<EmbeddedChunk> chunks,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the <paramref name="topK"/> nearest neighbors to <paramref name="queryVector"/>.
    /// </summary>
    Task<IReadOnlyList<SimilarityResult>> SearchAsync(
        float[] queryVector,
        int topK,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the total number of chunks currently stored.
    /// </summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all chunks from the store.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
