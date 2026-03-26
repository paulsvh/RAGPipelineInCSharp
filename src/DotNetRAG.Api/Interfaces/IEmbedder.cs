namespace DotNetRAG.Api.Interfaces;

/// <summary>
/// Generates vector embeddings from text using an embedding model.
/// </summary>
public interface IEmbedder
{
    /// <summary>
    /// Generates embeddings for multiple texts in a single batched operation.
    /// </summary>
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an embedding for a single text input.
    /// </summary>
    Task<float[]> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The dimensionality of the embedding vectors produced.
    /// </summary>
    int Dimensions { get; }
}
