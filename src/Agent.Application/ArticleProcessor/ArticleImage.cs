namespace Agent.Application.ArticleProcessor;

public class ArticleImage
{
    public required Uri Url { get; set; }
    public string AltText { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}