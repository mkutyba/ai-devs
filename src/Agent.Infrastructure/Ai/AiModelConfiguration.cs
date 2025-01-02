using Agent.Application.Abstractions;

namespace Agent.Infrastructure.Ai;

public static class AiModelConfiguration
{
    public static readonly IReadOnlyDictionary<AiModelType, (string ModelId, AiProvider Provider)> ModelTypes =
        new Dictionary<AiModelType, (string ModelId, AiProvider Provider)>
        {
            { AiModelType.Gpt4o_Mini_202407, ("gpt-4o-mini-2024-07-18", AiProvider.OpenAI) },
            { AiModelType.Gpt4o_202411, ("gpt-4o-2024-11-20", AiProvider.OpenAI) },
            { AiModelType.Llama31_8b, ("llama3.1:8b", AiProvider.Ollama) },
            { AiModelType.Phi35, ("phi3.5", AiProvider.Ollama) },
            { AiModelType.Whisper1, ("whisper-1", AiProvider.OpenAIAudio) },
            { AiModelType.Dalle3, ("dall-e-3", AiProvider.OpenAIImage) },
        };
}