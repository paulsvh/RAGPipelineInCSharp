using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetRAG.Api.Configuration;
using DotNetRAG.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace DotNetRAG.Api.Services;

public sealed partial class OpenAiEmbedder(
    HttpClient httpClient,
    IOptions<OpenAiSettings> settings,
    ILogger<OpenAiEmbedder> logger) : IEmbedder
{
    private readonly OpenAiSettings _settings = settings.Value;

    public int Dimensions => _settings.EmbeddingDimensions;

    public async Task<float[]> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var results = await EmbedBatchAsync([text], cancellationToken);
        return results[0];
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
            return [];

        var allEmbeddings = new List<float[]>(texts.Count);

        // Process in batches
        for (int i = 0; i < texts.Count; i += _settings.MaxBatchSize)
        {
            var batch = texts.Skip(i).Take(_settings.MaxBatchSize).ToList();
            var batchEmbeddings = await EmbedBatchInternalAsync(batch, cancellationToken);
            allEmbeddings.AddRange(batchEmbeddings);

            logger.LogDebug(
                "Embedded batch {BatchStart}-{BatchEnd} of {Total}",
                i, Math.Min(i + _settings.MaxBatchSize, texts.Count), texts.Count);
        }

        return allEmbeddings;
    }

    private async Task<List<float[]>> EmbedBatchInternalAsync(
        List<string> texts,
        CancellationToken cancellationToken)
    {
        var body = new EmbeddingRequest
        {
            Input = texts,
            Model = _settings.EmbeddingModel,
            Dimensions = _settings.EmbeddingDimensions
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/embeddings");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        request.Content = JsonContent.Create(body, JsonContext.Default.EmbeddingRequest);

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"OpenAI embedding API returned {response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync(
            JsonContext.Default.EmbeddingResponse,
            cancellationToken);

        if (result?.Data is null || result.Data.Count == 0)
            throw new InvalidOperationException("OpenAI returned empty embedding response.");

        return result.Data
            .OrderBy(d => d.Index)
            .Select(d => d.Embedding)
            .ToList();
    }

    // Request/response DTOs for OpenAI Embeddings API
    private sealed class EmbeddingRequest
    {
        [JsonPropertyName("input")]
        public required List<string> Input { get; set; }

        [JsonPropertyName("model")]
        public required string Model { get; set; }

        [JsonPropertyName("dimensions")]
        public int Dimensions { get; set; }
    }

    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingData> Data { get; set; } = [];

        [JsonPropertyName("usage")]
        public EmbeddingUsage? Usage { get; set; }
    }

    private sealed class EmbeddingData
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = [];
    }

    private sealed class EmbeddingUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    [JsonSerializable(typeof(EmbeddingRequest))]
    [JsonSerializable(typeof(EmbeddingResponse))]
    private sealed partial class JsonContext : JsonSerializerContext;
}
