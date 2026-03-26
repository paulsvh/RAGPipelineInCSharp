using System.Text.Json.Serialization;

namespace DotNetRAG.Api.Contracts.Responses;

public sealed record ConfigResponse(
    [property: JsonPropertyName("rag")] ConfigResponse.RagConfig Rag,
    [property: JsonPropertyName("anthropic")] ConfigResponse.AnthropicConfig Anthropic)
{
    public sealed record RagConfig(
        [property: JsonPropertyName("corpusDirectory")] string CorpusDirectory,
        [property: JsonPropertyName("chunkSize")] int ChunkSize,
        [property: JsonPropertyName("chunkOverlap")] int ChunkOverlap,
        [property: JsonPropertyName("defaultTopK")] int DefaultTopK,
        [property: JsonPropertyName("minSimilarityScore")] double MinSimilarityScore,
        [property: JsonPropertyName("fileExtensions")] string[] FileExtensions);

    public sealed record AnthropicConfig(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("maxTokens")] int MaxTokens);
}
