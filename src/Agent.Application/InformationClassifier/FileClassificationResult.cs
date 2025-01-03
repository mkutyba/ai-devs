using System.Text.Json.Serialization;

namespace Agent.Application.InformationClassifier;

public record FileClassificationResult(
    [property: JsonPropertyName("people")]
    List<string> People,
    [property: JsonPropertyName("hardware")]
    List<string> Hardware);