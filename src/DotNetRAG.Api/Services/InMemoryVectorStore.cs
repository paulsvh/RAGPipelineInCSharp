using System.Collections.Concurrent;
using System.Numerics;
using DotNetRAG.Api.Domain;
using DotNetRAG.Api.Interfaces;

namespace DotNetRAG.Api.Services;

public sealed class InMemoryVectorStore : IVectorStore, IDisposable
{
    private readonly ConcurrentDictionary<string, EmbeddedChunk> _store = new();
    private readonly SemaphoreSlim _upsertLock = new(1, 1);

    public void Dispose() => _upsertLock.Dispose();

    public async Task UpsertAsync(
        IReadOnlyList<EmbeddedChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        await _upsertLock.WaitAsync(cancellationToken);
        try
        {
            foreach (var chunk in chunks)
            {
                _store[chunk.Chunk.Id] = chunk;
            }
        }
        finally
        {
            _upsertLock.Release();
        }
    }

    public Task<IReadOnlyList<SimilarityResult>> SearchAsync(
        float[] queryVector,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var results = _store.Values
            .Select(chunk => new SimilarityResult
            {
                EmbeddedChunk = chunk,
                Score = CosineSimilarity(queryVector.AsSpan(), chunk.Embedding.AsSpan())
            })
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<SimilarityResult>>(results);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_store.Count);

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _store.Clear();
        return Task.CompletedTask;
    }

    internal static double CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have the same dimensionality.");

        float dot = 0f, normA = 0f, normB = 0f;
        int i = 0;
        int simdLength = Vector<float>.Count;

        // SIMD-accelerated loop
        for (; i <= a.Length - simdLength; i += simdLength)
        {
            var va = new Vector<float>(a.Slice(i, simdLength));
            var vb = new Vector<float>(b.Slice(i, simdLength));
            dot += Vector.Dot(va, vb);
            normA += Vector.Dot(va, va);
            normB += Vector.Dot(vb, vb);
        }

        // Scalar remainder
        for (; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        double denom = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denom == 0 ? 0 : dot / denom;
    }
}
