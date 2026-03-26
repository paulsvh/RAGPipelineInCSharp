using System.ComponentModel.DataAnnotations;

namespace DotNetRAG.Api.Configuration;

public sealed class AnthropicSettings
{
    public const string SectionName = "Anthropic";

    [Required(ErrorMessage = "Anthropic API key is required. Set it via: dotnet user-secrets set \"Anthropic:ApiKey\" \"your-key\"")]
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-4-20250514";
    public string BaseUrl { get; set; } = "https://api.anthropic.com";
    public int MaxTokens { get; set; } = 2048;
}
