using System.ComponentModel.DataAnnotations;

namespace AiCopilot.Infrastructure.Options;

public sealed class PlmApiOptions
{
    public const string SectionName = "PlmApi";

    [Required]
    public string BaseUrl { get; init; } = "http://localhost:5099/";
}
