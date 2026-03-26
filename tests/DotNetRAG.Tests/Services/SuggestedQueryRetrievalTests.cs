using DotNetRAG.Api.Configuration;
using DotNetRAG.Api.Domain;
using DotNetRAG.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DotNetRAG.Tests.Services;

/// <summary>
/// End-to-end retrieval tests that verify every suggested query in the UI
/// actually retrieves relevant chunks from its corpus. These tests use the
/// real LocalHashingEmbedder, real OverlappingChunker, and real InMemoryVectorStore
/// — no mocks — to catch threshold/ranking issues before users see them.
/// </summary>
public class SuggestedQueryRetrievalTests : IAsyncLifetime
{
    private readonly LocalHashingEmbedder _embedder = new(NullLogger<LocalHashingEmbedder>.Instance);
    private readonly InMemoryVectorStore _store = new();
    private readonly OverlappingChunker _chunker;
    private readonly CosineSimilarityRetriever _retriever;
    private readonly PlainTextDocumentLoader _loader;

    private static readonly string CorpusRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "corpus"));

    public SuggestedQueryRetrievalTests()
    {
        var ragSettings = Options.Create(new RagSettings
        {
            ChunkSize = 1500,
            ChunkOverlap = 300,
            MinSimilarityScore = 0.05,
            FileExtensions = [".md", ".txt"]
        });
        _chunker = new OverlappingChunker(ragSettings);
        _retriever = new CosineSimilarityRetriever(
            _embedder, _store, ragSettings,
            NullLogger<CosineSimilarityRetriever>.Instance);
        _loader = new PlainTextDocumentLoader(ragSettings, NullLogger<PlainTextDocumentLoader>.Instance);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync()
    {
        _store.Dispose();
        return Task.CompletedTask;
    }

    private async Task IngestCorpus(string corpusName)
    {
        await _store.ClearAsync();
        var corpusPath = Path.Combine(CorpusRoot, corpusName);
        var docs = await _loader.LoadFromDirectoryAsync(corpusPath);
        var allChunks = new List<DocumentChunk>();
        foreach (var doc in docs)
            allChunks.AddRange(_chunker.Chunk(doc.Content, doc.SourcePath));

        var embeddings = await _embedder.EmbedBatchAsync(allChunks.Select(c => c.Text).ToList());
        var embedded = allChunks.Zip(embeddings, (c, e) => new EmbeddedChunk { Chunk = c, Embedding = e }).ToList();
        await _store.UpsertAsync(embedded);
    }

    private async Task AssertQueryRetrievesRelevantChunks(string query, string expectedFileSubstring)
    {
        var results = await _retriever.RetrieveAsync(query, topK: 5);

        results.Should().NotBeEmpty(
            $"query \"{query}\" should retrieve at least one chunk above the similarity threshold");

        results.Should().Contain(r =>
            r.EmbeddedChunk.Chunk.SourcePath.Contains(expectedFileSubstring, StringComparison.OrdinalIgnoreCase)
            || r.EmbeddedChunk.Chunk.Text.Contains(expectedFileSubstring, StringComparison.OrdinalIgnoreCase),
            $"query \"{query}\" should retrieve chunks related to \"{expectedFileSubstring}\"");
    }

    // ── Tech Company Corpus ──

    [Fact]
    public async Task TechCompany_AuroraMonitoring_RetrievesRelevantChunks()
    {
        await IngestCorpus("tech-company");
        await AssertQueryRetrievesRelevantChunks(
            "What monitoring is available for Aurora Analytics?", "aurora");
    }

    [Fact]
    public async Task TechCompany_Sev1Incident_RetrievesRelevantChunks()
    {
        await IngestCorpus("tech-company");
        await AssertQueryRetrievesRelevantChunks(
            "What happens during a SEV1 incident?", "incident");
    }

    [Fact]
    public async Task TechCompany_DataRetention_RetrievesRelevantChunks()
    {
        await IngestCorpus("tech-company");
        await AssertQueryRetrievesRelevantChunks(
            "What is the data retention policy for confidential data?", "retention");
    }

    [Fact]
    public async Task TechCompany_NebulaEncryption_RetrievesRelevantChunks()
    {
        await IngestCorpus("tech-company");
        await AssertQueryRetrievesRelevantChunks(
            "How does Nebula handle encryption and compliance?", "nebula");
    }

    // ── Cooking Recipes Corpus ──

    [Fact]
    public async Task CookingRecipes_Carbonara_RetrievesRelevantChunks()
    {
        await IngestCorpus("cooking-recipes");
        await AssertQueryRetrievesRelevantChunks(
            "How do I make a proper carbonara?", "carbonara");
    }

    [Fact]
    public async Task CookingRecipes_BreadTemperature_RetrievesRelevantChunks()
    {
        await IngestCorpus("cooking-recipes");
        await AssertQueryRetrievesRelevantChunks(
            "What temperature should I bake bread at?", "bread");
    }

    [Fact]
    public async Task CookingRecipes_ChocolateMousse_RetrievesRelevantChunks()
    {
        await IngestCorpus("cooking-recipes");
        await AssertQueryRetrievesRelevantChunks(
            "How do I make chocolate mousse?", "mousse");
    }

    [Fact]
    public async Task CookingRecipes_GrillingSpices_RetrievesRelevantChunks()
    {
        await IngestCorpus("cooking-recipes");
        await AssertQueryRetrievesRelevantChunks(
            "What spices pair well with grilling?", "spice");
    }

    // ── Space Exploration Corpus ──

    [Fact]
    public async Task SpaceExploration_JamesWebb_RetrievesRelevantChunks()
    {
        await IngestCorpus("space-exploration");
        await AssertQueryRetrievesRelevantChunks(
            "What has the James Webb telescope discovered?", "webb");
    }

    [Fact]
    public async Task SpaceExploration_MarsWater_RetrievesRelevantChunks()
    {
        await IngestCorpus("space-exploration");
        await AssertQueryRetrievesRelevantChunks(
            "What evidence of water has been found on Mars?", "mars");
    }

    [Fact]
    public async Task SpaceExploration_IonPropulsion_RetrievesRelevantChunks()
    {
        await IngestCorpus("space-exploration");
        await AssertQueryRetrievesRelevantChunks(
            "How does ion propulsion compare to chemical rockets?", "propulsion");
    }

    [Fact]
    public async Task SpaceExploration_ExoplanetDetection_RetrievesRelevantChunks()
    {
        await IngestCorpus("space-exploration");
        await AssertQueryRetrievesRelevantChunks(
            "What methods are used to detect exoplanets?", "exoplanet");
    }
}
