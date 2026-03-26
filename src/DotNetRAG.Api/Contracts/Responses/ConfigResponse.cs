namespace DotNetRAG.Api.Contracts.Responses;

public sealed record ConfigResponse(
    ConfigResponse.RagConfig Rag,
    ConfigResponse.OpenAiConfig OpenAi,
    ConfigResponse.AnthropicConfig Anthropic)
{
    public sealed record RagConfig(
        string CorpusDirectory,
        int ChunkSize,
        int ChunkOverlap,
        int DefaultTopK,
        double MinSimilarityScore,
        string[] FileExtensions);

    public sealed record OpenAiConfig(
        string EmbeddingModel,
        int EmbeddingDimensions,
        int MaxBatchSize);

    public sealed record AnthropicConfig(
        string Model,
        int MaxTokens);
}
