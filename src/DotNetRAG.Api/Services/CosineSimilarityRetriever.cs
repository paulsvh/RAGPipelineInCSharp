using DotNetRAG.Api.Configuration;
using DotNetRAG.Api.Domain;
using DotNetRAG.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace DotNetRAG.Api.Services;

public sealed class CosineSimilarityRetriever(
    IEmbedder embedder,
    IVectorStore vectorStore,
    IOptions<RagSettings> settings,
    ILogger<CosineSimilarityRetriever> logger) : IRetriever
{
    private readonly double _minScore = settings.Value.MinSimilarityScore;

    public async Task<IReadOnlyList<SimilarityResult>> RetrieveAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Embedding query: {Query}", query);

        var queryVector = await embedder.EmbedAsync(query, cancellationToken);

        logger.LogDebug("Searching vector store for top {TopK} results", topK);

        var results = await vectorStore.SearchAsync(queryVector, topK, cancellationToken);

        // Filter out low-relevance results to prevent confabulation
        var filtered = results.Where(r => r.Score >= _minScore).ToList();

        logger.LogInformation(
            "Retrieved {Count} chunks ({Filtered} above threshold {Threshold:F2}, top score: {TopScore:F4})",
            results.Count,
            filtered.Count,
            _minScore,
            results.Count > 0 ? results[0].Score : 0);

        return filtered;
    }
}
