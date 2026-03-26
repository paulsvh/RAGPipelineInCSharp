using DotNetRAG.Api.Configuration;
using DotNetRAG.Api.Interfaces;
using DotNetRAG.Api.Middleware;
using DotNetRAG.Api.Services;

namespace DotNetRAG.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRagPipeline(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration with startup validation
        services.AddOptionsWithValidateOnStart<RagSettings>()
            .BindConfiguration(RagSettings.SectionName)
            .ValidateDataAnnotations()
            .Validate(s => s.ChunkOverlap < s.ChunkSize,
                "ChunkOverlap must be less than ChunkSize to avoid infinite chunking loops.");

        services.AddOptionsWithValidateOnStart<AnthropicSettings>()
            .BindConfiguration(AnthropicSettings.SectionName)
            .ValidateDataAnnotations();

        // Stateless components
        services.AddSingleton<IDocumentLoader, PlainTextDocumentLoader>();
        services.AddSingleton<IChunker, OverlappingChunker>();
        services.AddSingleton<IEmbedder, LocalHashingEmbedder>();

        // In-memory vector store (singleton — holds state)
        services.AddSingleton<IVectorStore, InMemoryVectorStore>();

        // LLM client
        services.AddHttpClient<ILanguageModelClient, AnthropicLanguageModelClient>()
            .AddStandardResilienceHandler();

        // Composed services
        services.AddScoped<IRetriever, CosineSimilarityRetriever>();
        services.AddScoped<IngestionPipeline>();
        services.AddScoped<QueryPipeline>();

        // Error handling
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
