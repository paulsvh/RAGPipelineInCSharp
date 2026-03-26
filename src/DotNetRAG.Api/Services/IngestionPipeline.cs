using System.Diagnostics;
using DotNetRAG.Api.Contracts.Responses;
using DotNetRAG.Api.Domain;
using DotNetRAG.Api.Interfaces;

namespace DotNetRAG.Api.Services;

public sealed class IngestionPipeline(
    IDocumentLoader documentLoader,
    IChunker chunker,
    IEmbedder embedder,
    IVectorStore vectorStore,
    ILogger<IngestionPipeline> logger)
{
    public async Task<IngestResponse> IngestAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Step 1: Load documents
        logger.LogInformation("Loading documents from {Directory}", directoryPath);
        var documents = await documentLoader.LoadFromDirectoryAsync(directoryPath, cancellationToken);

        // Step 2: Chunk documents
        logger.LogInformation("Chunking {Count} documents", documents.Count);
        var allChunks = new List<DocumentChunk>();
        foreach (var doc in documents)
        {
            var chunks = chunker.Chunk(doc.Content, doc.SourcePath);
            allChunks.AddRange(chunks);
        }
        logger.LogInformation("Created {ChunkCount} chunks from {DocCount} documents",
            allChunks.Count, documents.Count);

        // Step 3: Embed chunks
        logger.LogInformation("Generating embeddings for {Count} chunks", allChunks.Count);
        var texts = allChunks.Select(c => c.Text).ToList();
        var embeddings = await embedder.EmbedBatchAsync(texts, cancellationToken);

        // Step 4: Store embedded chunks
        var embeddedChunks = allChunks.Zip(embeddings, (chunk, embedding) => new EmbeddedChunk
        {
            Chunk = chunk,
            Embedding = embedding
        }).ToList();

        await vectorStore.UpsertAsync(embeddedChunks, cancellationToken);

        stopwatch.Stop();
        logger.LogInformation(
            "Ingestion complete: {Docs} docs, {Chunks} chunks in {Duration}ms",
            documents.Count, allChunks.Count, stopwatch.ElapsedMilliseconds);

        return new IngestResponse(
            DocumentsLoaded: documents.Count,
            ChunksCreated: allChunks.Count,
            ChunksEmbedded: embeddedChunks.Count,
            Duration: stopwatch.Elapsed);
    }
}
