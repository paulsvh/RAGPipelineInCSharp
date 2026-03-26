using DotNetRAG.Api.Contracts.Responses;
using DotNetRAG.Api.Interfaces;

namespace DotNetRAG.Api.Services;

public sealed class QueryPipeline(
    IRetriever retriever,
    ILanguageModelClient languageModelClient,
    ILogger<QueryPipeline> logger)
{
    public async Task<QueryResponse> QueryAsync(
        string question,
        int topK,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Retrieve relevant chunks
        logger.LogInformation("Retrieving top {TopK} chunks for query", topK);
        var results = await retriever.RetrieveAsync(question, topK, cancellationToken);

        if (results.Count == 0)
        {
            return new QueryResponse(
                Answer: "No relevant documents found. Please ingest documents first.",
                SourceChunks: [],
                ModelUsed: "none",
                Usage: new QueryUsageInfo(0, 0, 0));
        }

        // Step 2: Generate answer with LLM
        logger.LogInformation("Generating answer from {Count} context chunks", results.Count);
        var generationResult = await languageModelClient.GenerateAnswerAsync(
            question, results, cancellationToken);

        // Step 3: Build response with source references
        var chunkReferences = results.Select(r =>
        {
            var chunk = r.EmbeddedChunk.Chunk;
            var preview = chunk.Text.Length > 200
                ? chunk.Text[..200] + "..."
                : chunk.Text;

            return new ChunkReference(
                ChunkId: chunk.Id,
                SourceFile: chunk.SourcePath,
                ChunkIndex: chunk.ChunkIndex,
                SimilarityScore: r.Score,
                TextPreview: preview);
        }).ToList();

        return new QueryResponse(
            Answer: generationResult.Answer,
            SourceChunks: chunkReferences,
            ModelUsed: generationResult.ModelUsed,
            Usage: new QueryUsageInfo(
                PromptTokens: generationResult.PromptTokens,
                CompletionTokens: generationResult.CompletionTokens,
                ChunksRetrieved: results.Count));
    }
}
