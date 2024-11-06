using System.ComponentModel.DataAnnotations;

namespace Agent.Application.RobotLogin;

public record RobotLoginSettings
{
    [Required]
    public required string PageUrl { get; init; }

    [Required]
    public required string Username { get; init; }

    [Required]
    public required string Password { get; init; }

    public static string ConfigurationKey => "RobotLogin";
}