using Newtonsoft.Json.Linq;

namespace Agent.Application.Hq;

public class HqRequestModel
{
    public required string Task { get; set; }
    public required string Apikey { get; set; }
    public required object Answer { get; set; }
}