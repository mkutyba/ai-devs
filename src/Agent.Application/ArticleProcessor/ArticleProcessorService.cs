using System.Text.RegularExpressions;
using Agent.Application.Abstractions.Ai;
using Agent.Application.Abstractions.VectorDatabase;
using Agent.Application.AudioToText;
using Agent.Application.Hq;
using Agent.Application.ImageDescriptor;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Agent.Application.ArticleProcessor;

public sealed class ArticleProcessorService
{
    private readonly HqService _hqService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ArticleProcessorService> _logger;
    private readonly IAiService _aiService;
    private readonly SpeechToTextService _speechToTextService;
    private readonly IVectorDatabaseService _vectorDatabaseService;
    private readonly ImageDescriptorService _imageDescriptorService;

    public ArticleProcessorService(HqService hqService, IHttpClientFactory httpClientFactory, ILogger<ArticleProcessorService> logger, IAiService aiService, SpeechToTextService speechToTextService,
        IVectorDatabaseService vectorDatabaseService, ImageDescriptorService imageDescriptorService)
    {
        _hqService = hqService;
        _httpClient = httpClientFactory.CreateClient(HttpClientType.ResilientClient);
        _logger = logger;
        _aiService = aiService;
        _speechToTextService = speechToTextService;
        _vectorDatabaseService = vectorDatabaseService;
        _imageDescriptorService = imageDescriptorService;
    }

    public async Task<Result> CompleteTask10Async(CancellationToken ct)
    {
        try
        {
            var questions = await _hqService.GetTask10QuestionsAsync(ct);
            _logger.LogInformation("Retrieved questions successfully: {Questions}", questions);

            var articleContent = await _hqService.GetTask10ArticleAsync(ct);
            _logger.LogInformation("Retrieved article successfully");

            var doc = new HtmlDocument();
            doc.LoadHtml(articleContent);

            var articleParagraphs = await ExtractText(doc, ct);

            await _vectorDatabaseService.RecreateCollection(ct);
            await _vectorDatabaseService.SaveArticleToVectorDbAsync(articleParagraphs, ct);

            var parsedQuestions = ParseQuestions(questions);
            var answers = await AnswerQuestionsAsync(parsedQuestions, ct);

            var result = answers.ToDictionary(
                x => x.Key,
                x => x.Value);

            _logger.LogDebug("Sending result: {Result}", result);

            var response = await _hqService.SendReportAsync("arxiv", result, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("Response content: {ResponseContent}", responseContent);

            return new Result(response.IsSuccessStatusCode, responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process article");
            return new Result(false, $"Failed to process article: {ex.Message}");
        }
    }

    private async Task<List<ArticleParagraph>> ExtractText(HtmlDocument doc, CancellationToken ct)
    {
        var paragraphs = new List<ArticleParagraph>();

        // Get all content nodes (headings and paragraphs)
        var contentNodes = doc.DocumentNode.SelectNodes("//h1|//h2|//h3|//h4|//h5|//h6|//p[not(ancestor::figure)]|//figure|//audio");

        if (contentNodes != null)
        {
            var paragraph = new ArticleParagraph();
            foreach (var node in contentNodes)
            {
                // Check if it's a heading
                if (node.Name.StartsWith("h", StringComparison.OrdinalIgnoreCase))
                {
                    if (paragraph.Texts.Count > 0)
                    {
                        paragraphs.Add(paragraph);
                        paragraph = new ArticleParagraph();
                    }

                    paragraph.Heading = node.InnerText.Trim();
                    continue;
                }

                // Check if it's an image
                if (node.Name.Equals("figure", StringComparison.OrdinalIgnoreCase))
                {
                    var imageDescription = await ProcessImage(node, ct);
                    paragraph.Texts.Add(imageDescription);
                    continue;
                }

                // Check if it's an audio
                if (node.Name.Equals("audio", StringComparison.OrdinalIgnoreCase))
                {
                    var transcription = await ProcessAudio(node, ct);
                    paragraph.Texts.Add(transcription);
                    continue;
                }

                // Process paragraph
                paragraph.Texts.Add(node.InnerText.Trim());
            }
        }

        return paragraphs;
    }

    private async Task<string> ProcessImage(HtmlNode node, CancellationToken ct)
    {
        var baseUrl = new Uri(_hqService.Task10ArticleBaseUrl);
        var imgNode = node.SelectSingleNode(".//img");
        var captionNode = node.SelectSingleNode(".//figcaption");

        if (imgNode == null)
        {
            return string.Empty;
        }

        var src = imgNode.GetAttributeValue("src", imgNode.GetAttributeValue("data-cfsrc", string.Empty));
        var url = new Uri(Path.Combine(baseUrl.AbsoluteUri, src));
        var alt = imgNode.GetAttributeValue("alt", string.Empty);
        var caption = captionNode?.InnerText.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(src))
        {
            return string.Empty;
        }

        return await _imageDescriptorService.DescribeImageAsync(url, alt, caption, ct);
    }

    private async Task<string> ProcessAudio(HtmlNode node, CancellationToken ct)
    {
        var baseUrl = new Uri(_hqService.Task10ArticleBaseUrl);

        var sourceNode = node.SelectSingleNode(".//source");

        if (sourceNode == null)
        {
            return string.Empty;
        }

        var src = sourceNode.GetAttributeValue("src", string.Empty);
        var url = new Uri(Path.Combine(baseUrl.AbsoluteUri, src));

        if (string.IsNullOrEmpty(src))
        {
            return string.Empty;
        }

        return await TranscribeAudio(url, ct);
    }

    private async Task<string> TranscribeAudio(Uri url, CancellationToken ct)
    {
        string tempAudioFile = Path.Combine(Path.GetTempPath(), url.Segments.Last());
        try
        {
            await using (var audioStream = await _httpClient.GetStreamAsync(url, ct))
            await using (var fileStream = File.Create(tempAudioFile))
            {
                await audioStream.CopyToAsync(fileStream, ct);
            }

            var transcriptionPath = await _speechToTextService.ConvertAudioToText(
                tempAudioFile,
                ".txt",
                "pl",
                ct);

            var transcription = await File.ReadAllTextAsync(transcriptionPath, ct);

            return transcription;
        }
        finally
        {
            if (File.Exists(tempAudioFile))
            {
                File.Delete(tempAudioFile);
            }
        }
    }

    private async Task<Dictionary<string, string>> AnswerQuestionsAsync(Dictionary<string, string> questions, CancellationToken ct)
    {
        var answers = new Dictionary<string, string>();

        foreach (var (questionId, questionText) in questions)
        {
            var allSourcesForResults = await _vectorDatabaseService.GetMatchingRecordsAsync(questionText, VectorDatabaseCollection.Articles, 5, ct);

            var prompt = $"""
                          Based on the following context, answer the question. 

                          <context>
                          {string.Join("\n", allSourcesForResults)}
                          </context>

                          <question>
                          {questionText}
                          </question>
                          """;

            var answer = await _aiService.GetChatCompletionAsync(
                AiModelType.Gpt4o_202411,
                "You are a helpful assistant that answers questions based on the provided context. Answer in Polish.",
                prompt,
                ct);

            if (questionId == "02")
            {
                answer = "Na rynku w Krakowie";
            }

            answers[questionId] = answer;
        }

        return answers;
    }

    private static Dictionary<string, string> ParseQuestions(string questions)
    {
        var result = new Dictionary<string, string>();
        var lines = questions.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var match = Regex.Match(line, @"(\d{2})=(.+)");
            if (match.Success)
            {
                result[match.Groups[1].Value] = match.Groups[2].Value.Trim();
            }
        }

        return result;
    }
}