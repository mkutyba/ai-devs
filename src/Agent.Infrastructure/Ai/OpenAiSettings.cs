using System.ComponentModel.DataAnnotations;

namespace Agent.Infrastructure.Ai;

public class OpenAiSettings
{
    [Required]
    public required string ApiKey { get; init; }

    public static string ConfigurationKey => "OpenAi";
}