namespace ai_devs3;

public class RequestModel
{
    public required string Task { get; set; }
    public required string Apikey { get; set; }
    public required string[] Answer { get; set; }
}