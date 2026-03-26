using System.ComponentModel.DataAnnotations;

namespace DotNetRAG.Api.Configuration;

public sealed class OpenAiSettings
{
    public const string SectionName = "OpenAi";

    [Required(ErrorMessage = "OpenAI API key is required. Set it via: dotnet user-secrets set \"OpenAi:ApiKey\" \"your-key\"")]
    public string ApiKey { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public int EmbeddingDimensions { get; set; } = 1536;
    public int MaxBatchSize { get; set; } = 100;
}
