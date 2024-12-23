using System.ComponentModel.DataAnnotations;

namespace Agent.Application.Hq;

public class HqSettings
{
    [Required]
    public required string BaseUrl { get; init; }

    [Required]
    public required string ApiKey { get; init; }

    public static string ConfigurationKey => "Hq";
}