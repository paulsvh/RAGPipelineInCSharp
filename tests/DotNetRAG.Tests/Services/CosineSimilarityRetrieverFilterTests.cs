using DotNetRAG.Api.Configuration;
using DotNetRAG.Api.Domain;
using DotNetRAG.Api.Interfaces;
using DotNetRAG.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DotNetRAG.Tests.Services;

public class CosineSimilarityRetrieverFilterTests
{
    private readonly IEmbedder _embedder;
    private readonly IVectorStore _vectorStore;

    public CosineSimilarityRetrieverFilterTests()
    {
        _embedder = Substitute.For<IEmbedder>();
        _vectorStore = Substitute.For<IVectorStore>();
    }

    private CosineSimilarityRetriever CreateRetriever(double minScore)
    {
        var settings = Options.Create(new RagSettings { MinSimilarityScore = minScore });
        return new CosineSimilarityRetriever(
            _embedder, _vectorStore, settings, NullLogger<CosineSimilarityRetriever>.Instance);
    }

    [Fact]
    public async Task RetrieveAsync_ResultsBelowMinSimilarityScore_AreFilteredOut()
    {
        // Arrange
        var retriever = CreateRetriever(0.5);
        float[] queryVector = [1f, 0f];

        var storeResults = new List<SimilarityResult>
        {
            CreateResult("high00000000", 0.80),
            CreateResult("low000000000", 0.30),
            CreateResult("low200000000", 0.10)
        };

        _embedder.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(queryVector);
        _vectorStore.SearchAsync(queryVector, 5, Arg.Any<CancellationToken>())
            .Returns(storeResults);

        // Act
        var results = await retriever.RetrieveAsync("query", 5);

        // Assert
        results.Should().HaveCount(1);
        results[0].EmbeddedChunk.Chunk.Id.Should().Be("high00000000");
        results[0].Score.Should().Be(0.80);
    }

    [Fact]
    public async Task RetrieveAsync_ResultsAboveMinSimilarityScore_AreKept()
    {
        // Arrange
        var retriever = CreateRetriever(0.3);
        float[] queryVector = [1f, 0f];

        var storeResults = new List<SimilarityResult>
        {
            CreateResult("aaaaaaaaaaaa", 0.95),
            CreateResult("bbbbbbbbbbbb", 0.70),
            CreateResult("cccccccccccc", 0.30) // exactly at threshold — should be kept (>=)
        };

        _embedder.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(queryVector);
        _vectorStore.SearchAsync(queryVector, 5, Arg.Any<CancellationToken>())
            .Returns(storeResults);

        // Act
        var results = await retriever.RetrieveAsync("query", 5);

        // Assert
        results.Should().HaveCount(3);
        results.Select(r => r.EmbeddedChunk.Chunk.Id)
            .Should().Equal("aaaaaaaaaaaa", "bbbbbbbbbbbb", "cccccccccccc");
    }

    [Fact]
    public async Task RetrieveAsync_AllResultsFilteredOut_ReturnsEmptyList()
    {
        // Arrange
        var retriever = CreateRetriever(0.9);
        float[] queryVector = [1f, 0f];

        var storeResults = new List<SimilarityResult>
        {
            CreateResult("low100000000", 0.50),
            CreateResult("low200000000", 0.40),
            CreateResult("low300000000", 0.20)
        };

        _embedder.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(queryVector);
        _vectorStore.SearchAsync(queryVector, 5, Arg.Any<CancellationToken>())
            .Returns(storeResults);

        // Act
        var results = await retriever.RetrieveAsync("query", 5);

        // Assert
        results.Should().BeEmpty();
    }

    private static SimilarityResult CreateResult(string chunkId, double score)
    {
        return new SimilarityResult
        {
            EmbeddedChunk = new EmbeddedChunk
            {
                Chunk = new DocumentChunk
                {
                    Id = chunkId,
                    Text = $"Text for {chunkId}",
                    SourcePath = "source.md",
                    ChunkIndex = 0,
                    StartCharOffset = 0,
                    EndCharOffset = 10
                },
                Embedding = [1f, 0f]
            },
            Score = score
        };
    }
}
