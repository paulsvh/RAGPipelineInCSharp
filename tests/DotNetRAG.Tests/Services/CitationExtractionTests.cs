using DotNetRAG.Api.Domain;
using DotNetRAG.Api.Services;
using FluentAssertions;

namespace DotNetRAG.Tests.Services;

public class CitationExtractionTests
{
    [Fact]
    public void ExtractCitations_ValidCitation_IsExtractedCorrectly()
    {
        // Arrange
        var context = CreateContext(("abcdef123456", "doc.md", 0, 0.95));
        var answer = "The answer is here [cite:abcdef123456].";

        // Act
        var citations = AnthropicLanguageModelClient.ExtractCitations(answer, context);

        // Assert
        citations.Should().HaveCount(1);
        citations[0].ChunkId.Should().Be("abcdef123456");
        citations[0].SourceFile.Should().Be("doc.md");
        citations[0].ChunkIndex.Should().Be(0);
        citations[0].SimilarityScore.Should().Be(0.95);
    }

    [Fact]
    public void ExtractCitations_MultipleCitations_AreExtracted()
    {
        // Arrange
        var context = CreateContext(
            ("aaaaaaaaaaaa", "a.md", 0, 0.90),
            ("bbbbbbbbbbbb", "b.md", 1, 0.85));
        var answer = "First point [cite:aaaaaaaaaaaa] and second point [cite:bbbbbbbbbbbb].";

        // Act
        var citations = AnthropicLanguageModelClient.ExtractCitations(answer, context);

        // Assert
        citations.Should().HaveCount(2);
        citations[0].ChunkId.Should().Be("aaaaaaaaaaaa");
        citations[1].ChunkId.Should().Be("bbbbbbbbbbbb");
    }

    [Fact]
    public void ExtractCitations_DuplicateCitations_AreDeduplicated()
    {
        // Arrange
        var context = CreateContext(("abcdef123456", "doc.md", 0, 0.90));
        var answer = "Point A [cite:abcdef123456] and point B [cite:abcdef123456].";

        // Act
        var citations = AnthropicLanguageModelClient.ExtractCitations(answer, context);

        // Assert
        citations.Should().HaveCount(1);
        citations[0].ChunkId.Should().Be("abcdef123456");
    }

    [Fact]
    public void ExtractCitations_CitationIdNotInContext_IsIgnored()
    {
        // Arrange
        var context = CreateContext(("aaaaaaaaaaaa", "a.md", 0, 0.90));
        var answer = "Reference [cite:bbbbbbbbbbbb] to unknown chunk.";

        // Act
        var citations = AnthropicLanguageModelClient.ExtractCitations(answer, context);

        // Assert
        citations.Should().BeEmpty();
    }

    [Fact]
    public void ExtractCitations_NoCitationsInAnswer_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateContext(("aaaaaaaaaaaa", "a.md", 0, 0.90));
        var answer = "This answer has no citations at all.";

        // Act
        var citations = AnthropicLanguageModelClient.ExtractCitations(answer, context);

        // Assert
        citations.Should().BeEmpty();
    }

    [Fact]
    public void ExtractCitations_NoCitationsEvenWithContextChunks_ReturnsEmptyList()
    {
        // Arrange — fallback was removed, so even with context, no citations means empty list
        var context = CreateContext(
            ("aaaaaaaaaaaa", "a.md", 0, 0.90),
            ("bbbbbbbbbbbb", "b.md", 1, 0.85),
            ("cccccccccccc", "c.md", 2, 0.80));
        var answer = "I cannot find the answer in the provided context.";

        // Act
        var citations = AnthropicLanguageModelClient.ExtractCitations(answer, context);

        // Assert
        citations.Should().BeEmpty();
    }

    private static IReadOnlyList<SimilarityResult> CreateContext(
        params (string Id, string Source, int Index, double Score)[] items)
    {
        return items.Select(item => new SimilarityResult
        {
            EmbeddedChunk = new EmbeddedChunk
            {
                Chunk = new DocumentChunk
                {
                    Id = item.Id,
                    Text = $"Content for {item.Id}",
                    SourcePath = item.Source,
                    ChunkIndex = item.Index,
                    StartCharOffset = 0,
                    EndCharOffset = 10
                },
                Embedding = [1f, 0f]
            },
            Score = item.Score
        }).ToList();
    }
}
