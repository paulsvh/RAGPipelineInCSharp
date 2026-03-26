using DotNetRAG.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetRAG.Tests.Services;

public class LocalHashingEmbedderTests
{
    private readonly LocalHashingEmbedder _embedder = new(
        NullLogger<LocalHashingEmbedder>.Instance);

    [Fact]
    public void Dimensions_Returns384()
    {
        _embedder.Dimensions.Should().Be(384);
    }

    [Fact]
    public async Task EmbedAsync_ReturnVectorOfCorrectDimension()
    {
        var vector = await _embedder.EmbedAsync("test input text");
        vector.Length.Should().Be(384);
    }

    [Fact]
    public async Task EmbedAsync_IsDeterministic()
    {
        var v1 = await _embedder.EmbedAsync("the same text");
        var v2 = await _embedder.EmbedAsync("the same text");
        v1.Should().BeEquivalentTo(v2);
    }

    [Fact]
    public async Task EmbedAsync_DifferentInputsProduceDifferentVectors()
    {
        var v1 = await _embedder.EmbedAsync("carbonara pasta recipe with eggs and cheese");
        var v2 = await _embedder.EmbedAsync("rocket propulsion engines and spacecraft");

        var similarity = InMemoryVectorStore.CosineSimilarity(v1.AsSpan(), v2.AsSpan());
        similarity.Should().BeLessThan(0.5, "unrelated texts should produce dissimilar vectors");
    }

    [Fact]
    public async Task EmbedAsync_VectorIsL2Normalized()
    {
        var vector = await _embedder.EmbedAsync("some text to embed");
        var magnitude = Math.Sqrt(vector.Sum(x => (double)x * x));
        magnitude.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public async Task EmbedBatchAsync_ReturnsOneVectorPerInput()
    {
        var texts = new List<string> { "first", "second", "third" };
        var results = await _embedder.EmbedBatchAsync(texts);
        results.Count.Should().Be(3);
    }

    [Fact]
    public async Task EmbedBatchAsync_EmptyInput_ReturnsEmptyList()
    {
        var results = await _embedder.EmbedBatchAsync(new List<string>());
        results.Count.Should().Be(0);
    }

    [Fact]
    public async Task RelatedTexts_ProduceSimilarityAboveMinThreshold()
    {
        // This test validates that the embedder produces scores compatible
        // with the configured MinSimilarityScore (0.05). If this fails,
        // the retriever will filter out all results and queries return nothing.
        var query = await _embedder.EmbedAsync("How do I make carbonara pasta?");
        var document = await _embedder.EmbedAsync(
            "Carbonara is a classic Roman pasta dish made with eggs, pecorino cheese, " +
            "guanciale, and black pepper. Cook the pasta in salted boiling water.");

        var similarity = InMemoryVectorStore.CosineSimilarity(
            query.AsSpan(), document.AsSpan());

        similarity.Should().BeGreaterThan(0.05,
            "related text should score above MinSimilarityScore threshold");
    }

    [Fact]
    public async Task UnrelatedTexts_ProduceLowSimilarity()
    {
        var query = await _embedder.EmbedAsync("How do I make carbonara pasta?");
        var unrelated = await _embedder.EmbedAsync(
            "The James Webb Space Telescope orbits the L2 Lagrange point " +
            "approximately 1.5 million kilometers from Earth.");

        var similarity = InMemoryVectorStore.CosineSimilarity(
            query.AsSpan(), unrelated.AsSpan());

        similarity.Should().BeLessThan(0.1,
            "unrelated text should score near zero");
    }
}
