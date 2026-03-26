using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DotNetRAG.Api.Configuration;
using DotNetRAG.Api.Domain;
using DotNetRAG.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace DotNetRAG.Api.Services;

public sealed partial class AnthropicLanguageModelClient(
    HttpClient httpClient,
    IOptions<AnthropicSettings> settings,
    ILogger<AnthropicLanguageModelClient> logger) : ILanguageModelClient
{
    private readonly AnthropicSettings _settings = settings.Value;

    private const string SystemPrompt = """
        You are a helpful assistant that answers questions based ONLY on the provided context chunks.
        Each chunk has an ID and source file. You MUST cite only the chunks you actually use.

        Format each citation as [cite:CHUNK_ID] inline in your answer, where CHUNK_ID is the exact 12-character hex ID from the chunk header.

        Example answer:
        The platform supports real-time streaming ingestion [cite:a1b2c3d4e5f6] and integrates with monitoring tools for alerting [cite:f6e5d4c3b2a1].

        Rules:
        - Only cite chunks whose content you directly reference in your answer.
        - If the context does not contain enough information, say so explicitly and do not cite any chunks.
        - Do not fabricate information beyond what is in the context.
        """;

    public async Task<GenerationResult> GenerateAnswerAsync(
        string question,
        IReadOnlyList<SimilarityResult> context,
        CancellationToken cancellationToken = default)
    {
        var userMessage = BuildUserMessage(question, context);

        var body = new AnthropicRequest
        {
            Model = _settings.Model,
            MaxTokens = _settings.MaxTokens,
            System = SystemPrompt,
            Messages =
            [
                new AnthropicMessage { Role = "user", Content = userMessage }
            ]
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v1/messages");
        request.Headers.Add("x-api-key", _settings.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = JsonContent.Create(body, AnthropicJsonContext.Default.AnthropicRequest);

        logger.LogDebug("Sending request to Anthropic API with {ChunkCount} context chunks", context.Count);

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Anthropic API returned {response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync(
            AnthropicJsonContext.Default.AnthropicResponse,
            cancellationToken);

        if (result is null)
            throw new InvalidOperationException("Anthropic returned null response.");

        var answer = result.Content
            .Where(c => c.Type == "text")
            .Select(c => c.Text)
            .FirstOrDefault() ?? string.Empty;

        var citations = ExtractCitations(answer, context);

        logger.LogInformation(
            "Generated answer with {CitationCount} citations using {Model}",
            citations.Count, result.Model);

        return new GenerationResult(
            answer,
            citations,
            result.Model,
            result.Usage.InputTokens,
            result.Usage.OutputTokens);
    }

    private static string BuildUserMessage(string question, IReadOnlyList<SimilarityResult> context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("--- CONTEXT CHUNKS ---");

        foreach (var result in context)
        {
            var chunk = result.EmbeddedChunk.Chunk;
            sb.AppendLine($"[CHUNK {chunk.Id} from {chunk.SourcePath}]");
            sb.AppendLine(chunk.Text);
            sb.AppendLine("[/CHUNK]");
            sb.AppendLine();
        }

        sb.AppendLine("--- END CONTEXT ---");
        sb.AppendLine();
        sb.AppendLine($"Question: {question}");

        return sb.ToString();
    }

    internal static List<ChunkCitation> ExtractCitations(
        string answer,
        IReadOnlyList<SimilarityResult> context)
    {
        var citations = new List<ChunkCitation>();
        var matches = CitationPattern().Matches(answer);
        var seen = new HashSet<string>();

        foreach (Match match in matches)
        {
            var chunkId = match.Groups[1].Value;
            if (!seen.Add(chunkId))
                continue;

            var matchingResult = context.FirstOrDefault(
                r => r.EmbeddedChunk.Chunk.Id == chunkId);

            if (matchingResult is not null)
            {
                var chunk = matchingResult.EmbeddedChunk.Chunk;
                citations.Add(new ChunkCitation(
                    chunk.Id,
                    chunk.SourcePath,
                    chunk.ChunkIndex,
                    matchingResult.Score));
            }
        }

        return citations;
    }

    [GeneratedRegex(@"\[cite:([a-f0-9]{12})\]")]
    private static partial Regex CitationPattern();

    // Anthropic API DTOs
    internal sealed class AnthropicRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("system")]
        public string? System { get; set; }

        [JsonPropertyName("messages")]
        public required List<AnthropicMessage> Messages { get; set; }
    }

    internal sealed class AnthropicMessage
    {
        [JsonPropertyName("role")]
        public required string Role { get; set; }

        [JsonPropertyName("content")]
        public required string Content { get; set; }
    }

    internal sealed class AnthropicResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public List<ContentBlock> Content { get; set; } = [];

        [JsonPropertyName("usage")]
        public UsageInfo Usage { get; set; } = new();
    }

    internal sealed class ContentBlock
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    internal sealed class UsageInfo
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }

    [JsonSerializable(typeof(AnthropicRequest))]
    [JsonSerializable(typeof(AnthropicResponse))]
    internal sealed partial class AnthropicJsonContext : JsonSerializerContext;
}
