using System.Numerics;
using DotNetRAG.Api.Interfaces;

namespace DotNetRAG.Api.Services;

/// <summary>
/// A zero-dependency local embedder using feature hashing (the "hashing trick").
/// Tokenizes text into word unigrams and bigrams, hashes them into a fixed-dimension
/// vector, and L2-normalizes the result. No API key required.
///
/// This produces keyword-level similarity (not semantic), but works well enough
/// for demos and showcases the IEmbedder interface swappability.
/// </summary>
public sealed class LocalHashingEmbedder(ILogger<LocalHashingEmbedder> logger) : IEmbedder
{
    private const int DefaultDimensions = 384;

    public int Dimensions => DefaultDimensions;

    public Task<float[]> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HashEmbed(text));
    }

    public Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        var results = texts.Select(HashEmbed).ToList();
        logger.LogDebug("Locally embedded {Count} texts into {Dims}-dim vectors", texts.Count, DefaultDimensions);
        return Task.FromResult<IReadOnlyList<float[]>>(results);
    }

    private static float[] HashEmbed(string text)
    {
        var vector = new float[DefaultDimensions];
        var tokens = Tokenize(text);

        // Unigrams
        foreach (var token in tokens)
        {
            var hash = StableHash(token);
            var index = (int)((uint)hash % DefaultDimensions);
            var sign = (hash & 1) == 0 ? 1f : -1f;
            vector[index] += sign;
        }

        // Bigrams for phrase-level signal
        for (int i = 0; i < tokens.Count - 1; i++)
        {
            var bigram = tokens[i] + " " + tokens[i + 1];
            var hash = StableHash(bigram);
            var index = (int)((uint)hash % DefaultDimensions);
            var sign = (hash & 1) == 0 ? 1f : -1f;
            vector[index] += sign * 0.5f;
        }

        // L2 normalize
        Normalize(vector);
        return vector;
    }

    private static List<string> Tokenize(string text)
    {
        return text
            .ToLowerInvariant()
            .Split([' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '"', '\'', '/', '-', '#', '*', '|'],
                StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 1)
            .ToList();
    }

    private static int StableHash(string input)
    {
        // FNV-1a hash for deterministic cross-platform results
        unchecked
        {
            int hash = (int)2166136261;
            foreach (var c in input)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return hash;
        }
    }

    private static void Normalize(float[] vector)
    {
        float sumSq = 0f;
        int i = 0;
        int simdLen = Vector<float>.Count;

        for (; i <= vector.Length - simdLen; i += simdLen)
        {
            var v = new Vector<float>(vector, i);
            sumSq += Vector.Dot(v, v);
        }
        for (; i < vector.Length; i++)
            sumSq += vector[i] * vector[i];

        if (sumSq <= 0f) return;

        var magnitude = MathF.Sqrt(sumSq);
        for (i = 0; i < vector.Length; i++)
            vector[i] /= magnitude;
    }
}
