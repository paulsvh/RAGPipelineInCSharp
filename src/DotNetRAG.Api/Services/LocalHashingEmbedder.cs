using System.Numerics;
using DotNetRAG.Api.Interfaces;

namespace DotNetRAG.Api.Services;

/// <summary>
/// A zero-dependency local embedder using TF-IDF-weighted feature hashing.
/// Tokenizes text into terms, applies log-scaled term frequency weighting,
/// filters stopwords, boosts important terms, hashes into a fixed-dimension
/// vector, and L2-normalizes the result. No API key required.
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

        if (tokens.Count == 0)
            return vector;

        // Build term frequency map
        var termFreqs = new Dictionary<string, int>();
        foreach (var token in tokens)
        {
            termFreqs.TryGetValue(token, out var count);
            termFreqs[token] = count + 1;
        }

        // TF-weighted unigrams with log scaling and term importance boost
        foreach (var (term, count) in termFreqs)
        {
            var tf = 1f + MathF.Log(count); // log-scaled TF
            // Boost longer/rarer terms — a simple IDF proxy:
            // short common words (2-3 chars) get weight 1.0,
            // longer distinctive terms (6+ chars) get up to 3.0
            var importance = 1f + MathF.Min(2f, MathF.Max(0f, (term.Length - 3) * 0.5f));
            var hash = StableHash(term);
            var index = (int)((uint)hash % DefaultDimensions);
            var sign = (hash & 1) == 0 ? 1f : -1f;
            vector[index] += sign * tf * importance;
        }

        // Bigrams for phrase-level signal (weighted higher for specificity)
        for (int i = 0; i < tokens.Count - 1; i++)
        {
            var bigram = tokens[i] + " " + tokens[i + 1];
            var hash = StableHash(bigram);
            var index = (int)((uint)hash % DefaultDimensions);
            var sign = (hash & 1) == 0 ? 1f : -1f;
            vector[index] += sign * 1.5f;
        }

        // Trigrams for even more specific phrase matching
        for (int i = 0; i < tokens.Count - 2; i++)
        {
            var trigram = tokens[i] + " " + tokens[i + 1] + " " + tokens[i + 2];
            var hash = StableHash(trigram);
            var index = (int)((uint)hash % DefaultDimensions);
            var sign = (hash & 1) == 0 ? 1f : -1f;
            vector[index] += sign * 2.0f;
        }

        Normalize(vector);
        return vector;
    }

    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "is", "at", "which", "on", "in", "to", "of", "and", "or", "an",
        "for", "with", "by", "from", "as", "it", "its", "be", "are", "was",
        "were", "been", "being", "has", "have", "had", "do", "does", "did",
        "will", "would", "could", "should", "may", "might", "can", "shall",
        "that", "this", "these", "those", "not", "but", "if", "then", "than",
        "so", "no", "nor", "too", "very", "just", "about", "above", "after",
        "before", "between", "into", "through", "during", "each", "few",
        "more", "most", "other", "some", "such", "only", "own", "same",
        "also", "how", "what", "when", "where", "who", "why", "all", "any",
        "both", "here", "there", "up", "out", "over", "under", "again",
        "further", "once", "per", "via", "make", "made", "get", "use",
        "used", "using", "like", "need", "want", "good", "best", "well",
        "new", "old", "way", "much", "many", "even", "still", "back",
        "take", "come", "give", "tell", "say", "know", "see", "look",
        "find", "keep", "let", "put", "set", "try", "ask", "work",
        "seem", "feel", "leave", "call", "long", "great", "right",
        "going", "really", "proper", "does", "thing", "things"
    };

    private static List<string> Tokenize(string text)
    {
        return text
            .ToLowerInvariant()
            .Split([' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?',
                    '(', ')', '[', ']', '{', '}', '"', '\'', '/', '-',
                    '#', '*', '|', '`', '~', '@', '$', '%', '^', '&',
                    '+', '=', '<', '>'],
                StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 1 && !Stopwords.Contains(t))
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
