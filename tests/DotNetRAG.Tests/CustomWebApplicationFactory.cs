using DotNetRAG.Api.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace DotNetRAG.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public IEmbedder MockEmbedder { get; } = Substitute.For<IEmbedder>();
    public ILanguageModelClient MockLanguageModelClient { get; } = Substitute.For<ILanguageModelClient>();
    public IDocumentLoader MockDocumentLoader { get; } = Substitute.For<IDocumentLoader>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAi:ApiKey"] = "test-key",
                ["Anthropic:ApiKey"] = "test-key"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove real IEmbedder registration (added via AddHttpClient<IEmbedder, OpenAiEmbedder>)
            var embedderDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEmbedder));
            if (embedderDescriptor is not null)
                services.Remove(embedderDescriptor);

            // Remove real ILanguageModelClient registration
            var llmDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ILanguageModelClient));
            if (llmDescriptor is not null)
                services.Remove(llmDescriptor);

            // Remove real IDocumentLoader registration
            var docLoaderDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDocumentLoader));
            if (docLoaderDescriptor is not null)
                services.Remove(docLoaderDescriptor);

            // Register mock instances
            services.AddSingleton(MockEmbedder);
            services.AddSingleton(MockLanguageModelClient);
            services.AddSingleton(MockDocumentLoader);
        });
    }
}
