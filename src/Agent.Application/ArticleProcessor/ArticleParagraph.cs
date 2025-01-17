namespace Agent.Application.ArticleProcessor;

public class ArticleParagraph
{
    public List<string> Texts { get; set; } = [];
    public string FullText => string.Join("\n", Texts);
    public string Heading { get; set; } = string.Empty;
}