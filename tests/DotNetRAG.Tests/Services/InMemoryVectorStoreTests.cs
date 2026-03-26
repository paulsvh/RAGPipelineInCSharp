using DotNetRAG.Api.Domain;
using DotNetRAG.Api.Services;
using FluentAssertions;

namespace DotNetRAG.Tests.Services;

public class InMemoryVectorStoreTests
{
    private readonly InMemoryVectorStore _store = new();

    private static EmbeddedChunk CreateEmbeddedChunk(string id, float[] embedding)
    {
        return new EmbeddedChunk
        {
            Chunk = new DocumentChunk
            {
                Id = id,
                Text = $"Text for {id}",
                SourcePath = "test.txt",
                ChunkIndex = 0,
                StartCharOffset = 0,
                EndCharOffset = 10
            },
            Embedding = embedding
        };
    }

    [Fact]
    public async Task UpsertAsync_AndCountAsync_ReturnsCorrectCount()
    {
        var chunks = new[]
        {
            CreateEmbeddedChunk("chunk-1", [1f, 0f, 0f]),
            CreateEmbeddedChunk("chunk-2", [0f, 1f, 0f]),
            CreateEmbeddedChunk("chunk-3", [0f, 0f, 1f])
        };

        await _store.UpsertAsync(chunks);

        var count = await _store.CountAsync();
        count.Should().Be(3);
    }

    [Fact]
    public async Task SearchAsync_ReturnsResultsOrderedByCosineSimilarity()
    {
        var chunks = new[]
        {
            CreateEmbeddedChunk("exact-match", [1f, 0f, 0f]),
            CreateEmbeddedChunk("partial-match", [0.7f, 0.7f, 0f]),
            CreateEmbeddedChunk("no-match", [0f, 0f, 1f])
        };
        await _store.UpsertAsync(chunks);

        float[] queryVector = [1f, 0f, 0f];
        var results = await _store.SearchAsync(queryVector, 3);

        results.Should().HaveCount(3);
        results[0].EmbeddedChunk.Chunk.Id.Should().Be("exact-match");
        results[0].Score.Should().BeGreaterThan(results[1].Score);
        results[1].Score.Should().BeGreaterThan(results[2].Score);
    }

    [Fact]
    public async Task SearchAsync_WithTopK_LimitsResults()
    {
        var chunks = new[]
        {
            CreateEmbeddedChunk("a", [1f, 0f, 0f]),
            CreateEmbeddedChunk("b", [0f, 1f, 0f]),
            CreateEmbeddedChunk("c", [0f, 0f, 1f]),
            CreateEmbeddedChunk("d", [1f, 1f, 0f])
        };
        await _store.UpsertAsync(chunks);

        float[] queryVector = [1f, 0f, 0f];
        var results = await _store.SearchAsync(queryVector, 2);

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task ClearAsync_EmptiesTheStore()
    {
        var chunks = new[]
        {
            CreateEmbeddedChunk("chunk-1", [1f, 0f]),
            CreateEmbeddedChunk("chunk-2", [0f, 1f])
        };
        await _store.UpsertAsync(chunks);

        await _store.ClearAsync();

        var count = await _store.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        float[] a = [1f, 2f, 3f];
        float[] b = [1f, 2f, 3f];

        var result = InMemoryVectorStore.CosineSimilarity(a.AsSpan(), b.AsSpan());

        result.Should().BeApproximately(1.0, 1e-6);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        float[] a = [1f, 0f, 0f];
        float[] b = [0f, 1f, 0f];

        var result = InMemoryVectorStore.CosineSimilarity(a.AsSpan(), b.AsSpan());

        result.Should().BeApproximately(0.0, 1e-6);
    }

    [Fact]
    public void CosineSimilarity_OppositeVectors_ReturnsNegativeOne()
    {
        float[] a = [1f, 2f, 3f];
        float[] b = [-1f, -2f, -3f];

        var result = InMemoryVectorStore.CosineSimilarity(a.AsSpan(), b.AsSpan());

        result.Should().BeApproximately(-1.0, 1e-6);
    }

    [Fact]
    public async Task UpsertAsync_WithSameId_OverwritesExistingChunk()
    {
        var original = CreateEmbeddedChunk("same-id", [1f, 0f, 0f]);
        await _store.UpsertAsync(new[] { original });

        var replacement = new EmbeddedChunk
        {
            Chunk = new DocumentChunk
            {
                Id = "same-id",
                Text = "Updated text",
                SourcePath = "updated.txt",
                ChunkIndex = 0,
                StartCharOffset = 0,
                EndCharOffset = 12
            },
            Embedding = [0f, 1f, 0f]
        };
        await _store.UpsertAsync(new[] { replacement });

        var count = await _store.CountAsync();
        count.Should().Be(1);

        float[] queryVector = [0f, 1f, 0f];
        var results = await _store.SearchAsync(queryVector, 1);
        results[0].EmbeddedChunk.Chunk.Text.Should().Be("Updated text");
    }

    [Fact]
    public void CosineSimilarity_MismatchedDimensions_ThrowsArgumentException()
    {
        var a = new float[] { 1f, 0f };
        var b = new float[] { 1f, 0f, 0f };

        var act = () => InMemoryVectorStore.CosineSimilarity(a.AsSpan(), b.AsSpan());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CosineSimilarity_ZeroVectors_ReturnsZero()
    {
        ReadOnlySpan<float> a = new float[] { 0f, 0f, 0f };
        ReadOnlySpan<float> b = new float[] { 1f, 0f, 0f };

        var result = InMemoryVectorStore.CosineSimilarity(a, b);

        result.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_EmptyStore_ReturnsEmptyList()
    {
        var emptyStore = new InMemoryVectorStore();
        float[] queryVector = [1f, 0f, 0f];

        var results = await emptyStore.SearchAsync(queryVector, 5);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_TopKGreaterThanCount_ReturnsAllItems()
    {
        await _store.UpsertAsync([CreateEmbeddedChunk("a", [1f, 0f]), CreateEmbeddedChunk("b", [0f, 1f])]);

        var results = await _store.SearchAsync([1f, 0f], topK: 10);

        results.Should().HaveCount(2);
    }
}
