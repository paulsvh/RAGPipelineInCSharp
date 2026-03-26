using DotNetRAG.Api.Configuration;
using DotNetRAG.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace DotNetRAG.Tests.Services;

public class OverlappingChunkerTests
{
    private readonly OverlappingChunker _chunker;

    public OverlappingChunkerTests()
    {
        var settings = Options.Create(new RagSettings
        {
            ChunkSize = 512,
            ChunkOverlap = 128
        });
        _chunker = new OverlappingChunker(settings);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\n")]
    [InlineData(null)]
    public void Chunk_EmptyOrWhitespaceInput_ReturnsEmptyList(string? input)
    {
        var result = _chunker.Chunk(input!, "test.txt");

        result.Should().BeEmpty();
    }

    [Fact]
    public void Chunk_ShortText_ReturnsSingleChunk()
    {
        var text = "This is a short paragraph that fits within a single chunk.";

        var result = _chunker.Chunk(text, "doc.md");

        result.Should().HaveCount(1);
        result[0].Text.Should().Be(text);
        result[0].SourcePath.Should().Be("doc.md");
        result[0].ChunkIndex.Should().Be(0);
    }

    [Fact]
    public void Chunk_MultipleParagraphs_CreatesExpectedNumberOfChunks()
    {
        // Build text with multiple paragraphs that exceed the chunk size (512)
        var paragraph = new string('A', 200);
        // 3 paragraphs of 200 chars each, separated by \n\n (total ~604 chars with separators)
        var text = string.Join("\n\n", paragraph, paragraph, paragraph);

        var result = _chunker.Chunk(text, "source.txt");

        result.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public void Chunk_WithOverlap_ChunksHaveOverlappingContent()
    {
        // Use a small chunk size to force multiple chunks with visible overlap
        var settings = Options.Create(new RagSettings
        {
            ChunkSize = 50,
            ChunkOverlap = 20
        });
        var chunker = new OverlappingChunker(settings);

        // Create paragraphs that will force splitting
        var text = "First paragraph here.\n\nSecond paragraph here.\n\nThird paragraph here.";

        var result = chunker.Chunk(text, "overlap.txt");

        result.Should().HaveCountGreaterThan(1);

        // The end of the first chunk should overlap with the beginning of the second chunk
        for (int i = 1; i < result.Count; i++)
        {
            var previousChunkText = result[i - 1].Text;
            var currentChunkText = result[i].Text;

            // The current chunk should contain some text from the end of the previous chunk
            var previousEnd = previousChunkText[^Math.Min(20, previousChunkText.Length)..];
            currentChunkText.Should().StartWith(previousEnd,
                because: "chunks should overlap by the configured overlap amount");
        }
    }

    [Fact]
    public void Chunk_DeterministicIds_SameInputProducesSameIds()
    {
        var text = "Some content for deterministic ID testing.\n\nAnother paragraph.";
        var sourcePath = "deterministic.txt";

        var result1 = _chunker.Chunk(text, sourcePath);
        var result2 = _chunker.Chunk(text, sourcePath);

        result1.Should().HaveSameCount(result2);
        for (int i = 0; i < result1.Count; i++)
        {
            result1[i].Id.Should().Be(result2[i].Id);
        }
    }

    [Fact]
    public void Chunk_ChunkIndexValues_AreSequentialStartingAtZero()
    {
        var paragraph = new string('B', 200);
        var text = string.Join("\n\n", paragraph, paragraph, paragraph);

        var result = _chunker.Chunk(text, "sequential.txt");

        result.Should().HaveCountGreaterThanOrEqualTo(1);
        for (int i = 0; i < result.Count; i++)
        {
            result[i].ChunkIndex.Should().Be(i);
        }
    }

    [Fact]
    public void Chunk_SourcePath_IsPreservedOnAllChunks()
    {
        var paragraph = new string('C', 200);
        var text = string.Join("\n\n", paragraph, paragraph, paragraph);
        var sourcePath = "my/document/path.md";

        var result = _chunker.Chunk(text, sourcePath);

        result.Should().AllSatisfy(chunk =>
            chunk.SourcePath.Should().Be(sourcePath));
    }
}
