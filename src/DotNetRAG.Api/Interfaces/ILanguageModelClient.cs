using DotNetRAG.Api.Domain;

namespace DotNetRAG.Api.Interfaces;

/// <summary>
/// Generates grounded, cited answers from a question and retrieved context chunks.
/// </summary>
public interface ILanguageModelClient
{
    /// <summary>
    /// Sends the <paramref name="question"/> with <paramref name="context"/> chunks to an LLM
    /// and returns an answer with inline citations.
    /// </summary>
    Task<GenerationResult> GenerateAnswerAsync(
        string question,
        IReadOnlyList<SimilarityResult> context,
        CancellationToken cancellationToken = default);
}
