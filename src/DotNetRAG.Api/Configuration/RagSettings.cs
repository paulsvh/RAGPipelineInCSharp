using System.ComponentModel.DataAnnotations;

namespace DotNetRAG.Api.Configuration;

public sealed class RagSettings
{
    public const string SectionName = "Rag";

    [Required]
    public string CorpusDirectory { get; set; } = "corpus";

    [Range(1, 100_000)]
    public int ChunkSize { get; set; } = 512;

    [Range(0, 99_999)]
    public int ChunkOverlap { get; set; } = 128;

    [Range(1, 100)]
    public int DefaultTopK { get; set; } = 5;

    [Range(0.0, 1.0)]
    public double MinSimilarityScore { get; set; } = 0.3;

    public string[] FileExtensions { get; set; } = [".md", ".txt"];
}
