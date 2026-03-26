using DotNetRAG.Api.Domain;

namespace DotNetRAG.Api.Interfaces;

/// <summary>
/// Retrieves the most semantically relevant chunks for a given query.
/// Composes embedding generation with vector store search.
/// </summary>
public interface IRetriever
{
    /// <summary>
    /// Embeds the <paramref name="query"/> and returns the top matching chunks,
    /// filtered by a minimum similarity threshold.
    /// </summary>
    Task<IReadOnlyList<SimilarityResult>> RetrieveAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default);
}
