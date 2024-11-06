using System.ComponentModel.DataAnnotations;

namespace Agent.Infrastructure.OpenAi;

public class OpenAiSettings
{
    [Required]
    public required string ApiKey { get; init; }

    public static string ConfigurationKey => "OpenAi";
}