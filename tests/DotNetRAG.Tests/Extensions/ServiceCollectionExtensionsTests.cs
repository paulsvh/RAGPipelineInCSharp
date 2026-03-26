using DotNetRAG.Api.Extensions;
using DotNetRAG.Api.Interfaces;
using DotNetRAG.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetRAG.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public void AddRagPipeline_RegistersAllRequiredServices()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Anthropic:ApiKey"] = "test-key"
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(config);
        services.AddRagPipeline(config);

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IDocumentLoader>().Should().BeOfType<PlainTextDocumentLoader>();
        provider.GetRequiredService<IChunker>().Should().BeOfType<OverlappingChunker>();
        provider.GetRequiredService<IEmbedder>().Should().BeOfType<LocalHashingEmbedder>();
        provider.GetRequiredService<IVectorStore>().Should().BeOfType<InMemoryVectorStore>();
    }

    [Fact]
    public void AddRagPipeline_ChunkOverlapGreaterThanChunkSize_FailsValidation()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Anthropic:ApiKey"] = "test-key",
            ["Rag:ChunkSize"] = "100",
            ["Rag:ChunkOverlap"] = "200"
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(config);
        services.AddRagPipeline(config);

        var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IOptions<DotNetRAG.Api.Configuration.RagSettings>>().Value;

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*ChunkOverlap*ChunkSize*");
    }

    [Fact]
    public void AddRagPipeline_MissingAnthropicApiKey_FailsValidation()
    {
        var config = BuildConfig(new Dictionary<string, string?>());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(config);
        services.AddRagPipeline(config);

        var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IOptions<DotNetRAG.Api.Configuration.AnthropicSettings>>().Value;

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*Anthropic*API key*");
    }
}
