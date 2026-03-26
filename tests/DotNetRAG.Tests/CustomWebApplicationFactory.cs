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
                ["Anthropic:ApiKey"] = "test-key"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove real registrations
            RemoveService<IEmbedder>(services);
            RemoveService<ILanguageModelClient>(services);
            RemoveService<IDocumentLoader>(services);

            // Register mock instances
            services.AddSingleton(MockEmbedder);
            services.AddSingleton(MockLanguageModelClient);
            services.AddSingleton(MockDocumentLoader);
        });
    }

    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is not null)
            services.Remove(descriptor);
    }
}
