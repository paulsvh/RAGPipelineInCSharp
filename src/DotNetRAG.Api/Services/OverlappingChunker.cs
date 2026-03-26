using System.Security.Cryptography;
using System.Text;
using DotNetRAG.Api.Configuration;
using DotNetRAG.Api.Domain;
using DotNetRAG.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace DotNetRAG.Api.Services;

public sealed class OverlappingChunker(IOptions<RagSettings> settings) : IChunker
{
    private readonly int _chunkSize = settings.Value.ChunkSize;
    private readonly int _chunkOverlap = settings.Value.ChunkOverlap;

    public IReadOnlyList<DocumentChunk> Chunk(string content, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(content))
            return [];

        var paragraphs = SplitIntoParagraphs(content);
        var chunks = new List<DocumentChunk>();
        var currentText = new StringBuilder();
        int chunkIndex = 0;
        int charOffset = 0;

        foreach (var paragraph in paragraphs)
        {
            if (currentText.Length > 0 && currentText.Length + paragraph.Length + 2 > _chunkSize)
            {
                var text = currentText.ToString().Trim();
                if (text.Length > 0)
                {
                    chunks.Add(CreateChunk(text, sourcePath, chunkIndex, charOffset));
                    chunkIndex++;

                    // Calculate overlap: take the last _chunkOverlap characters
                    var overlapStart = Math.Max(0, text.Length - _chunkOverlap);
                    var overlapText = text[overlapStart..];
                    charOffset += text.Length - overlapText.Length;

                    currentText.Clear();
                    currentText.Append(overlapText);
                }
            }

            if (currentText.Length > 0)
                currentText.Append("\n\n");

            currentText.Append(paragraph);

            // Handle paragraphs larger than chunk size
            while (currentText.Length > _chunkSize)
            {
                var text = currentText.ToString()[.._chunkSize].Trim();
                chunks.Add(CreateChunk(text, sourcePath, chunkIndex, charOffset));
                chunkIndex++;

                var overlapStart = Math.Max(0, _chunkSize - _chunkOverlap);
                var remaining = currentText.ToString()[overlapStart..];
                charOffset += overlapStart;

                currentText.Clear();
                currentText.Append(remaining);
            }
        }

        // Flush remaining text
        var finalText = currentText.ToString().Trim();
        if (finalText.Length > 0)
        {
            chunks.Add(CreateChunk(finalText, sourcePath, chunkIndex, charOffset));
        }

        return chunks;
    }

    private static List<string> SplitIntoParagraphs(string content)
    {
        return content
            .Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();
    }

    private static DocumentChunk CreateChunk(
        string text, string sourcePath, int chunkIndex, int startOffset)
    {
        var id = GenerateChunkId(sourcePath, chunkIndex);
        return new DocumentChunk
        {
            Id = id,
            Text = text,
            SourcePath = sourcePath,
            ChunkIndex = chunkIndex,
            StartCharOffset = startOffset,
            EndCharOffset = startOffset + text.Length
        };
    }

    private static string GenerateChunkId(string sourcePath, int chunkIndex)
    {
        var input = $"{sourcePath}:{chunkIndex}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes)[..12].ToLowerInvariant();
    }
}
