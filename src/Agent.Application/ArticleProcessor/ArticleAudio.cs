namespace Agent.Application.ArticleProcessor;

public class ArticleAudio
{
    public required Uri Url { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string Transcription { get; set; } = string.Empty;
}