using System.ComponentModel.DataAnnotations;

namespace Agent.Application.JsonCompleter;

public class JsonCompleterSettings
{
    [Required]
    public required string ReportUrl { get; init; }

    [Required]
    public required string ApiKey { get; init; }

    public static string ConfigurationKey => "JsonCompleter";
}