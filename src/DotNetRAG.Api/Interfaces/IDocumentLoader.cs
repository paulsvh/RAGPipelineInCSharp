using DotNetRAG.Api.Domain;

namespace DotNetRAG.Api.Interfaces;

/// <summary>
/// Loads raw document content from a file system directory.
/// </summary>
public interface IDocumentLoader
{
    /// <summary>
    /// Recursively loads all documents with matching extensions from <paramref name="directoryPath"/>.
    /// </summary>
    Task<IReadOnlyList<LoadedDocument>> LoadFromDirectoryAsync(
        string directoryPath,
        CancellationToken cancellationToken = default);
}
