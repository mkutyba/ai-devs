using System.ComponentModel.DataAnnotations;

namespace Agent.Application.RobotVerifier;

public record RobotVerifierSettings
{
    [Required]
    public required string PageUrl { get; init; }

    public static string ConfigurationKey => "RobotVerifier";
}