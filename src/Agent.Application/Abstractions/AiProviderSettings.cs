namespace Agent.Application.Abstractions;

public class AiProviderSettings
{
    public Uri? ApiEndpoint { get; init; }
    public string? ApiKey { get; init; }
}