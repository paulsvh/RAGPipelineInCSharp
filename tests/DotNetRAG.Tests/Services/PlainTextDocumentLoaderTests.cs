using DotNetRAG.Api.Configuration;
using DotNetRAG.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DotNetRAG.Tests.Services;

public class PlainTextDocumentLoaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PlainTextDocumentLoader _loader;

    public PlainTextDocumentLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "dotnetrag_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);

        var settings = Options.Create(new RagSettings
        {
            FileExtensions = [".md", ".txt"]
        });
        _loader = new PlainTextDocumentLoader(settings, NullLogger<PlainTextDocumentLoader>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task LoadFromDirectoryAsync_NonexistentDirectory_ThrowsDirectoryNotFoundException()
    {
        var act = () => _loader.LoadFromDirectoryAsync("/nonexistent/path");
        await act.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task LoadFromDirectoryAsync_EmptyFiles_AreSkipped()
    {
        File.WriteAllText(Path.Combine(_tempDir, "empty.txt"), "");
        File.WriteAllText(Path.Combine(_tempDir, "whitespace.txt"), "   \n  ");
        File.WriteAllText(Path.Combine(_tempDir, "valid.txt"), "This has content.");

        var result = await _loader.LoadFromDirectoryAsync(_tempDir);

        result.Should().HaveCount(1);
        result[0].Content.Should().Be("This has content.");
    }

    [Fact]
    public async Task LoadFromDirectoryAsync_FiltersByConfiguredExtensions()
    {
        File.WriteAllText(Path.Combine(_tempDir, "doc.md"), "Markdown content");
        File.WriteAllText(Path.Combine(_tempDir, "notes.txt"), "Text content");
        File.WriteAllText(Path.Combine(_tempDir, "code.cs"), "Console.WriteLine();");
        File.WriteAllText(Path.Combine(_tempDir, "data.json"), "{}");

        var result = await _loader.LoadFromDirectoryAsync(_tempDir);

        result.Should().HaveCount(2);
        result.Select(d => d.SourcePath).Should().BeEquivalentTo(["doc.md", "notes.txt"]);
    }

    [Fact]
    public async Task LoadFromDirectoryAsync_RecursivelyLoadsSubdirectories()
    {
        var subDir = Path.Combine(_tempDir, "subdir");
        Directory.CreateDirectory(subDir);

        File.WriteAllText(Path.Combine(_tempDir, "root.md"), "Root content");
        File.WriteAllText(Path.Combine(subDir, "nested.md"), "Nested content");

        var result = await _loader.LoadFromDirectoryAsync(_tempDir);

        result.Should().HaveCount(2);
        result.Select(d => d.SourcePath).Should().Contain("subdir/nested.md");
    }

    [Fact]
    public async Task LoadFromDirectoryAsync_RelativePaths_UseForwardSlashes()
    {
        var subDir = Path.Combine(_tempDir, "deep", "nested");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "file.txt"), "Content");

        var result = await _loader.LoadFromDirectoryAsync(_tempDir);

        result.Should().HaveCount(1);
        result[0].SourcePath.Should().Be("deep/nested/file.txt");
        result[0].SourcePath.Should().NotContain("\\");
    }

    [Fact]
    public async Task LoadFromDirectoryAsync_EmptyDirectory_ReturnsEmptyList()
    {
        var result = await _loader.LoadFromDirectoryAsync(_tempDir);
        result.Should().BeEmpty();
    }
}
