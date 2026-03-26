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

        services.AddOptionsWithValidateOnStart<OpenAiSettings>()
            .BindConfiguration(OpenAiSettings.SectionName)
            .ValidateDataAnnotations();

        services.AddOptionsWithValidateOnStart<AnthropicSettings>()
            .BindConfiguration(AnthropicSettings.SectionName)
            .ValidateDataAnnotations();

        // Stateless components
        services.AddSingleton<IDocumentLoader, PlainTextDocumentLoader>();
        services.AddSingleton<IChunker, OverlappingChunker>();

        // In-memory vector store (singleton — holds state)
        services.AddSingleton<IVectorStore, InMemoryVectorStore>();

        // HTTP-backed services with resilience
        services.AddHttpClient<IEmbedder, OpenAiEmbedder>()
            .AddStandardResilienceHandler();

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
