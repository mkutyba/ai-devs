using System.Text.Json.Serialization;

namespace Agent.Application.RobotVerifier;

public class RobotVerifierMessage
{
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("msgID")]
    public required int MsgID { get; init; }
}