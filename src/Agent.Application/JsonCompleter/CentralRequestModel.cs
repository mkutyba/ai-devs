using System.Text.Json.Serialization;

namespace Agent.Application.JsonCompleter;

public class CentralRequestModel
{
    [JsonPropertyName("task")]
    public required string Task { get; set; }

    [JsonPropertyName("apikey")]
    public required string Apikey { get; set; }

    [JsonPropertyName("answer")]
    public required string Answer { get; set; }
}