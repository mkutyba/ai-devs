using System.Text.Json.Serialization;

namespace Agent.Application.Abstractions.Ai;

public record AIResponse(
    [property: JsonPropertyName("thinking_process")]
    string ThinkingProcess,
    [property: JsonPropertyName("answer")]
    string Answer);