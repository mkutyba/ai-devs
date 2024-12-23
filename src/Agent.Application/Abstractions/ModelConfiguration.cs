namespace Agent.Application.Abstractions;

public static class ModelConfiguration
{
    public static readonly IReadOnlyDictionary<ModelType, (string ModelId, AiProvider Provider)> ModelTypes =
        new Dictionary<ModelType, (string ModelId, AiProvider Provider)>
        {
            { ModelType.Gpt4o_Mini_202407, ("gpt-4o-mini-2024-07-18", AiProvider.OpenAI) },
            { ModelType.Llama31_8b, ("llama3.1:8b", AiProvider.Ollama) },
            { ModelType.Phi35, ("phi3.5", AiProvider.Ollama) },
        };
}