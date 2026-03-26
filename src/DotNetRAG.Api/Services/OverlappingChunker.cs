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

        // Extract document title and track section headings for context injection
        var docTitle = ExtractTitle(content);
        var paragraphs = SplitIntoParagraphs(content);
        var chunks = new List<DocumentChunk>();
        var currentText = new StringBuilder();
        var currentSection = "";
        int chunkIndex = 0;
        int charOffset = 0;

        foreach (var paragraph in paragraphs)
        {
            // Track the nearest section heading
            if (paragraph.StartsWith("## "))
                currentSection = paragraph;

            if (currentText.Length > 0 && currentText.Length + paragraph.Length + 2 > _chunkSize)
            {
                var text = currentText.ToString().Trim();
                if (text.Length > 0)
                {
                    var contextualText = PrependContext(text, docTitle, currentSection);
                    chunks.Add(CreateChunk(contextualText, sourcePath, chunkIndex, charOffset));
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
                var contextualText = PrependContext(text, docTitle, currentSection);
                chunks.Add(CreateChunk(contextualText, sourcePath, chunkIndex, charOffset));
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
            var contextualText = PrependContext(finalText, docTitle, currentSection);
            chunks.Add(CreateChunk(contextualText, sourcePath, chunkIndex, charOffset));
        }

        return chunks;
    }

    private static string ExtractTitle(string content)
    {
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("# ") && !trimmed.StartsWith("## "))
                return trimmed[2..].Trim();
        }
        return "";
    }

    private static string PrependContext(string text, string docTitle, string section)
    {
        // Don't prepend if the chunk already starts with a heading
        if (text.StartsWith('#'))
            return text;

        var prefix = new StringBuilder();
        if (!string.IsNullOrEmpty(docTitle))
            prefix.Append($"[{docTitle}]");
        if (!string.IsNullOrEmpty(section) && !text.Contains(section))
        {
            if (prefix.Length > 0) prefix.Append(" > ");
            prefix.Append($"[{section.TrimStart('#', ' ')}]");
        }

        return prefix.Length > 0 ? $"{prefix}\n{text}" : text;
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
