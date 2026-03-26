using DotNetRAG.Api.Configuration;
using DotNetRAG.Api.Domain;
using DotNetRAG.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace DotNetRAG.Api.Services;

public sealed class PlainTextDocumentLoader(
    IOptions<RagSettings> settings,
    ILogger<PlainTextDocumentLoader> logger) : IDocumentLoader
{
    private readonly RagSettings _settings = settings.Value;

    public async Task<IReadOnlyList<LoadedDocument>> LoadFromDirectoryAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Corpus directory not found: {directoryPath}");

        var documents = new List<LoadedDocument>();

        foreach (var extension in _settings.FileExtensions)
        {
            var files = Directory.GetFiles(directoryPath, $"*{extension}", SearchOption.AllDirectories);

            foreach (var filePath in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var content = await File.ReadAllTextAsync(filePath, cancellationToken);

                if (string.IsNullOrWhiteSpace(content))
                {
                    logger.LogWarning("Skipping empty file: {FilePath}", filePath);
                    continue;
                }

                var relativePath = Path.GetRelativePath(directoryPath, filePath)
                    .Replace('\\', '/');

                documents.Add(new LoadedDocument(relativePath, content));
                logger.LogDebug("Loaded document: {FilePath} ({Length} chars)", relativePath, content.Length);
            }
        }

        logger.LogInformation("Loaded {Count} documents from {Directory}", documents.Count, directoryPath);
        return documents;
    }
}
