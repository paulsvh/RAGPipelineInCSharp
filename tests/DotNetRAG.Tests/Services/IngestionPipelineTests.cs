using DotNetRAG.Api.Domain;
using DotNetRAG.Api.Interfaces;
using DotNetRAG.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DotNetRAG.Tests.Services;

public class IngestionPipelineTests
{
    private readonly IDocumentLoader _documentLoader;
    private readonly IChunker _chunker;
    private readonly IEmbedder _embedder;
    private readonly IVectorStore _vectorStore;
    private readonly IngestionPipeline _pipeline;

    public IngestionPipelineTests()
    {
        _documentLoader = Substitute.For<IDocumentLoader>();
        _chunker = Substitute.For<IChunker>();
        _embedder = Substitute.For<IEmbedder>();
        _vectorStore = Substitute.For<IVectorStore>();
        var logger = NullLogger<IngestionPipeline>.Instance;
        _pipeline = new IngestionPipeline(_documentLoader, _chunker, _embedder, _vectorStore, logger);
    }

    [Fact]
    public async Task IngestAsync_OrchestratesLoadChunkEmbedStoreInCorrectOrder()
    {
        // Arrange
        var documents = new List<LoadedDocument>
        {
            new("doc1.md", "Hello world")
        };
        var chunks = new List<DocumentChunk>
        {
            new()
            {
                Id = "aaaaaaaaaaaa",
                Text = "Hello world",
                SourcePath = "doc1.md",
                ChunkIndex = 0,
                StartCharOffset = 0,
                EndCharOffset = 11
            }
        };
        float[][] embeddings = [[1f, 0f, 0f]];

        var callOrder = new List<string>();

        _documentLoader.LoadFromDirectoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(documents)
            .AndDoes(_ => callOrder.Add("load"));

        _chunker.Chunk(Arg.Any<string>(), Arg.Any<string>())
            .Returns(chunks)
            .AndDoes(_ => callOrder.Add("chunk"));

        _embedder.EmbedBatchAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(embeddings)
            .AndDoes(_ => callOrder.Add("embed"));

        _vectorStore.UpsertAsync(Arg.Any<IReadOnlyList<EmbeddedChunk>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("store"));

        // Act
        await _pipeline.IngestAsync("/some/dir");

        // Assert
        callOrder.Should().Equal("load", "chunk", "embed", "store");
    }

    [Fact]
    public async Task IngestAsync_ResponseDtoHasCorrectCounts()
    {
        // Arrange
        var documents = new List<LoadedDocument>
        {
            new("doc1.md", "content one"),
            new("doc2.md", "content two")
        };

        var chunks1 = new List<DocumentChunk>
        {
            new() { Id = "aaaaaaaaaaaa", Text = "chunk1", SourcePath = "doc1.md", ChunkIndex = 0, StartCharOffset = 0, EndCharOffset = 6 },
            new() { Id = "bbbbbbbbbbbb", Text = "chunk2", SourcePath = "doc1.md", ChunkIndex = 1, StartCharOffset = 6, EndCharOffset = 12 }
        };
        var chunks2 = new List<DocumentChunk>
        {
            new() { Id = "cccccccccccc", Text = "chunk3", SourcePath = "doc2.md", ChunkIndex = 0, StartCharOffset = 0, EndCharOffset = 6 }
        };

        _documentLoader.LoadFromDirectoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(documents);

        _chunker.Chunk("content one", "doc1.md").Returns(chunks1);
        _chunker.Chunk("content two", "doc2.md").Returns(chunks2);

        float[][] embeddings = [[1f], [2f], [3f]];
        _embedder.EmbedBatchAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(embeddings);

        _vectorStore.UpsertAsync(Arg.Any<IReadOnlyList<EmbeddedChunk>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _pipeline.IngestAsync("/some/dir");

        // Assert
        result.DocumentsLoaded.Should().Be(2);
        result.ChunksCreated.Should().Be(3);
        result.ChunksEmbedded.Should().Be(3);
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task IngestAsync_EmptyDocumentListProducesZeroChunks()
    {
        // Arrange
        _documentLoader.LoadFromDirectoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<LoadedDocument>());

        _embedder.EmbedBatchAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<float[]>());

        _vectorStore.UpsertAsync(Arg.Any<IReadOnlyList<EmbeddedChunk>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _pipeline.IngestAsync("/empty/dir");

        // Assert
        result.DocumentsLoaded.Should().Be(0);
        result.ChunksCreated.Should().Be(0);
        result.ChunksEmbedded.Should().Be(0);
        _chunker.DidNotReceive().Chunk(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task IngestAsync_MultipleDocumentsHaveChunksAggregated()
    {
        // Arrange
        var documents = new List<LoadedDocument>
        {
            new("a.md", "aaa"),
            new("b.md", "bbb"),
            new("c.md", "ccc")
        };

        _documentLoader.LoadFromDirectoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(documents);

        _chunker.Chunk("aaa", "a.md").Returns(new List<DocumentChunk>
        {
            new() { Id = "a00000000000", Text = "aaa", SourcePath = "a.md", ChunkIndex = 0, StartCharOffset = 0, EndCharOffset = 3 }
        });
        _chunker.Chunk("bbb", "b.md").Returns(new List<DocumentChunk>
        {
            new() { Id = "b00000000000", Text = "bbb1", SourcePath = "b.md", ChunkIndex = 0, StartCharOffset = 0, EndCharOffset = 4 },
            new() { Id = "b00000000001", Text = "bbb2", SourcePath = "b.md", ChunkIndex = 1, StartCharOffset = 4, EndCharOffset = 8 }
        });
        _chunker.Chunk("ccc", "c.md").Returns(new List<DocumentChunk>
        {
            new() { Id = "c00000000000", Text = "ccc", SourcePath = "c.md", ChunkIndex = 0, StartCharOffset = 0, EndCharOffset = 3 }
        });

        float[][] embeddings = [[1f], [2f], [3f], [4f]];
        _embedder.EmbedBatchAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(embeddings);

        _vectorStore.UpsertAsync(Arg.Any<IReadOnlyList<EmbeddedChunk>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _pipeline.IngestAsync("/multi/dir");

        // Assert
        result.DocumentsLoaded.Should().Be(3);
        result.ChunksCreated.Should().Be(4);
        result.ChunksEmbedded.Should().Be(4);

        // Verify all texts were passed to embedder as a single batch
        await _embedder.Received(1).EmbedBatchAsync(
            Arg.Is<IReadOnlyList<string>>(texts => texts.Count == 4),
            Arg.Any<CancellationToken>());

        // Verify all embedded chunks were stored in a single upsert
        await _vectorStore.Received(1).UpsertAsync(
            Arg.Is<IReadOnlyList<EmbeddedChunk>>(chunks => chunks.Count == 4),
            Arg.Any<CancellationToken>());
    }
}
