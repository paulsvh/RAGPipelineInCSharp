using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DotNetRAG.Api.Contracts.Requests;
using DotNetRAG.Api.Domain;
using DotNetRAG.Api.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace DotNetRAG.Tests;

public class EndpointIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public EndpointIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GET_Health_Returns200WithChunkCount()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/diagnostics/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.TryGetProperty("status", out _).Should().BeTrue();
        json.TryGetProperty("chunksStored", out _).Should().BeTrue();
        json.TryGetProperty("timestamp", out _).Should().BeTrue();
        json.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task GET_Config_Returns200WithNonSecretConfig()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/diagnostics/config");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        // Verify the three config sections exist
        json.TryGetProperty("rag", out var rag).Should().BeTrue();
        json.TryGetProperty("openAi", out var openAi).Should().BeTrue();
        json.TryGetProperty("anthropic", out var anthropic).Should().BeTrue();

        // Verify RAG section has expected properties
        rag.TryGetProperty("corpusDirectory", out _).Should().BeTrue();
        rag.TryGetProperty("chunkSize", out _).Should().BeTrue();

        // Verify API keys are NOT present in any section
        var rawJson = await response.Content.ReadAsStringAsync();
        rawJson.Should().NotContainEquivalentOf("apiKey");
        rawJson.Should().NotContain("test-key");
    }

    [Fact]
    public async Task POST_Query_EmptyQuestion_Returns400()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var request = new QueryRequest(Question: "", TopK: null);

        // Act
        var response = await client.PostAsJsonAsync("/api/query", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetInt32().Should().Be(400);
        json.TryGetProperty("detail", out _).Should().BeTrue();
    }

    [Fact]
    public async Task POST_Query_ValidQuestion_Returns200WithAnswer()
    {
        // Arrange
        var embeddingVector = new float[1536];
        embeddingVector[0] = 1.0f; // simple unit-ish vector for cosine similarity

        // Configure mock embedder to return a fake vector for any query
        _factory.MockEmbedder
            .EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(embeddingVector);

        _factory.MockEmbedder
            .Dimensions.Returns(1536);

        // Pre-populate the vector store with a chunk that matches the query vector
        var store = _factory.Services.GetRequiredService<IVectorStore>();
        var embeddedChunk = new EmbeddedChunk
        {
            Chunk = new DocumentChunk
            {
                Id = "chunk-1",
                Text = "This is a test document about integration testing.",
                SourcePath = "test-doc.txt",
                ChunkIndex = 0,
                StartCharOffset = 0,
                EndCharOffset = 50
            },
            Embedding = embeddingVector // same vector => cosine similarity = 1.0
        };
        await store.UpsertAsync([embeddedChunk]);

        // Configure mock LLM to return a GenerationResult
        var generationResult = new GenerationResult(
            Answer: "Integration testing is a type of software testing.",
            Citations:
            [
                new ChunkCitation("chunk-1", "test-doc.txt", 0, 1.0)
            ],
            ModelUsed: "claude-sonnet-4-20250514",
            PromptTokens: 100,
            CompletionTokens: 50);

        _factory.MockLanguageModelClient
            .GenerateAnswerAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<SimilarityResult>>(), Arg.Any<CancellationToken>())
            .Returns(generationResult);

        using var client = _factory.CreateClient();
        var request = new QueryRequest(Question: "What is integration testing?", TopK: 5);

        // Act
        var response = await client.PostAsJsonAsync("/api/query", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.TryGetProperty("answer", out var answer).Should().BeTrue();
        answer.GetString().Should().Be("Integration testing is a type of software testing.");

        json.TryGetProperty("sourceChunks", out var sourceChunks).Should().BeTrue();
        sourceChunks.GetArrayLength().Should().BeGreaterThan(0);

        json.TryGetProperty("modelUsed", out var modelUsed).Should().BeTrue();
        modelUsed.GetString().Should().Be("claude-sonnet-4-20250514");

        json.TryGetProperty("usage", out var usage).Should().BeTrue();
        usage.TryGetProperty("promptTokens", out _).Should().BeTrue();
        usage.TryGetProperty("completionTokens", out _).Should().BeTrue();
        usage.TryGetProperty("chunksRetrieved", out _).Should().BeTrue();

        // Cleanup: clear the store so other tests are not affected
        await store.ClearAsync();
    }

    [Fact]
    public async Task DELETE_Ingest_Returns204()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/ingest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task POST_Ingest_PathOutsideCorpus_Returns403()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var request = new IngestRequest(DirectoryPath: @"C:\Windows\System32");

        // Act
        var response = await client.PostAsJsonAsync("/api/ingest", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetInt32().Should().Be(403);
    }
}
