using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Agent.Application.JsonCompleter;

public class CentralRequestModelTask3
{
    [JsonPropertyName("task")]
    public required string Task { get; set; }

    [JsonPropertyName("apikey")]
    public required string Apikey { get; set; }

    [JsonPropertyName("answer")]
    public required JObject Answer { get; set; }
}