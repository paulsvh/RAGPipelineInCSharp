using DotNetRAG.Api.Configuration;
using DotNetRAG.Api.Domain;
using DotNetRAG.Api.Interfaces;
using DotNetRAG.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DotNetRAG.Tests.Services;

public class CosineSimilarityRetrieverTests
{
    private readonly IEmbedder _embedder;
    private readonly IVectorStore _vectorStore;
    private readonly CosineSimilarityRetriever _retriever;

    public CosineSimilarityRetrieverTests()
    {
        _embedder = Substitute.For<IEmbedder>();
        _vectorStore = Substitute.For<IVectorStore>();
        var logger = NullLogger<CosineSimilarityRetriever>.Instance;
        var settings = Options.Create(new RagSettings { MinSimilarityScore = 0.0 });
        _retriever = new CosineSimilarityRetriever(_embedder, _vectorStore, settings, logger);
    }

    [Fact]
    public async Task RetrieveAsync_CallsEmbedderThenVectorStore()
    {
        var query = "test query";
        float[] queryVector = [1f, 0f, 0f];
        var expectedResults = new List<SimilarityResult>();

        _embedder.EmbedAsync(query, Arg.Any<CancellationToken>())
            .Returns(queryVector);
        _vectorStore.SearchAsync(queryVector, 5, Arg.Any<CancellationToken>())
            .Returns(expectedResults);

        await _retriever.RetrieveAsync(query, 5);

        await _embedder.Received(1).EmbedAsync(query, Arg.Any<CancellationToken>());
        await _vectorStore.Received(1).SearchAsync(queryVector, 5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsResultsFromVectorStore()
    {
        var query = "find me something";
        float[] queryVector = [0.5f, 0.5f];
        var expectedResults = new List<SimilarityResult>
        {
            new()
            {
                EmbeddedChunk = new EmbeddedChunk
                {
                    Chunk = new DocumentChunk
                    {
                        Id = "chunk-1",
                        Text = "result text",
                        SourcePath = "doc.md",
                        ChunkIndex = 0,
                        StartCharOffset = 0,
                        EndCharOffset = 11
                    },
                    Embedding = [0.5f, 0.5f]
                },
                Score = 0.95
            }
        };

        _embedder.EmbedAsync(query, Arg.Any<CancellationToken>())
            .Returns(queryVector);
        _vectorStore.SearchAsync(queryVector, 3, Arg.Any<CancellationToken>())
            .Returns(expectedResults);

        var results = await _retriever.RetrieveAsync(query, 3);

        results.Should().BeEquivalentTo(expectedResults);
    }

    [Fact]
    public async Task RetrieveAsync_ForwardsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var query = "cancellation test";
        float[] queryVector = [1f];

        _embedder.EmbedAsync(query, token)
            .Returns(queryVector);
        _vectorStore.SearchAsync(queryVector, 5, token)
            .Returns(new List<SimilarityResult>());

        await _retriever.RetrieveAsync(query, 5, token);

        await _embedder.Received(1).EmbedAsync(query, token);
        await _vectorStore.Received(1).SearchAsync(queryVector, 5, token);
    }
}
