using DotNetRAG.Api.Domain;
using DotNetRAG.Api.Interfaces;
using DotNetRAG.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DotNetRAG.Tests.Services;

public class QueryPipelineTests
{
    private readonly IRetriever _retriever;
    private readonly ILanguageModelClient _languageModelClient;
    private readonly QueryPipeline _pipeline;

    public QueryPipelineTests()
    {
        _retriever = Substitute.For<IRetriever>();
        _languageModelClient = Substitute.For<ILanguageModelClient>();
        var logger = NullLogger<QueryPipeline>.Instance;
        _pipeline = new QueryPipeline(_retriever, _languageModelClient, logger);
    }

    [Fact]
    public async Task QueryAsync_EmptyRetrievalResults_ReturnsNoRelevantDocumentsMessage()
    {
        // Arrange
        _retriever.RetrieveAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SimilarityResult>());

        // Act
        var result = await _pipeline.QueryAsync("what is RAG?", 5);

        // Assert
        result.Answer.Should().Be("No relevant documents found. Please ingest documents first.");
        result.ModelUsed.Should().Be("none");
        result.SourceChunks.Should().BeEmpty();
        result.Usage.PromptTokens.Should().Be(0);
        result.Usage.CompletionTokens.Should().Be(0);
        result.Usage.ChunksRetrieved.Should().Be(0);

        // LLM should never be called when no chunks are found
        await _languageModelClient.DidNotReceive()
            .GenerateAnswerAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<SimilarityResult>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryAsync_NormalFlow_AssemblesQueryResponseCorrectly()
    {
        // Arrange
        var similarityResults = new List<SimilarityResult>
        {
            CreateSimilarityResult("abcdef123456", "doc.md", 0, "Some relevant text", 0.92),
            CreateSimilarityResult("fedcba654321", "other.md", 1, "Another chunk", 0.85)
        };

        _retriever.RetrieveAsync("my question", 5, Arg.Any<CancellationToken>())
            .Returns(similarityResults);

        var generationResult = new GenerationResult(
            Answer: "The answer is 42",
            Citations: new List<ChunkCitation>(),
            ModelUsed: "claude-sonnet-4-20250514",
            PromptTokens: 100,
            CompletionTokens: 50);

        _languageModelClient.GenerateAnswerAsync("my question", similarityResults, Arg.Any<CancellationToken>())
            .Returns(generationResult);

        // Act
        var result = await _pipeline.QueryAsync("my question", 5);

        // Assert
        result.Answer.Should().Be("The answer is 42");
        result.ModelUsed.Should().Be("claude-sonnet-4-20250514");
        result.Usage.PromptTokens.Should().Be(100);
        result.Usage.CompletionTokens.Should().Be(50);
        result.Usage.ChunksRetrieved.Should().Be(2);

        result.SourceChunks.Should().HaveCount(2);
        result.SourceChunks[0].ChunkId.Should().Be("abcdef123456");
        result.SourceChunks[0].SourceFile.Should().Be("doc.md");
        result.SourceChunks[0].SimilarityScore.Should().Be(0.92);
        result.SourceChunks[1].ChunkId.Should().Be("fedcba654321");
        result.SourceChunks[1].SourceFile.Should().Be("other.md");
    }

    [Fact]
    public async Task QueryAsync_LongChunkText_TextPreviewIsTruncatedTo200CharsWithEllipsis()
    {
        // Arrange
        var longText = new string('x', 300);
        var similarityResults = new List<SimilarityResult>
        {
            CreateSimilarityResult("aabbccddeeff", "long.md", 0, longText, 0.90)
        };

        _retriever.RetrieveAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(similarityResults);

        _languageModelClient.GenerateAnswerAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<SimilarityResult>>(), Arg.Any<CancellationToken>())
            .Returns(new GenerationResult("answer", [], "model", 10, 5));

        // Act
        var result = await _pipeline.QueryAsync("question", 5);

        // Assert
        result.SourceChunks.Should().HaveCount(1);
        var preview = result.SourceChunks[0].TextPreview;
        preview.Should().HaveLength(203); // 200 chars + "..."
        preview.Should().EndWith("...");
        preview[..200].Should().Be(new string('x', 200));
    }

    private static SimilarityResult CreateSimilarityResult(
        string chunkId, string sourcePath, int chunkIndex, string text, double score)
    {
        return new SimilarityResult
        {
            EmbeddedChunk = new EmbeddedChunk
            {
                Chunk = new DocumentChunk
                {
                    Id = chunkId,
                    Text = text,
                    SourcePath = sourcePath,
                    ChunkIndex = chunkIndex,
                    StartCharOffset = 0,
                    EndCharOffset = text.Length
                },
                Embedding = [1f, 0f]
            },
            Score = score
        };
    }
}
