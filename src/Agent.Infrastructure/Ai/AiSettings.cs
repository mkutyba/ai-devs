namespace Agent.Infrastructure.Ai;

public class AiSettings
{
    public static string ConfigurationKey => "Ai";
    public AiProviderSettings OpenAI { get; init; } = null!;
    public AiProviderSettings Ollama { get; init; } = null!;
}