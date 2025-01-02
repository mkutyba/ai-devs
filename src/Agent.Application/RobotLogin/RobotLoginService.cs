using System.Text.RegularExpressions;
using Agent.Application.Ai;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agent.Application.RobotLogin;

public class RobotLoginService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RobotLoginService> _logger;
    private readonly IAiSimpleAnswerService _aiSimpleAnswerService;
    private readonly RobotLoginSettings _settings;

    public RobotLoginService(IHttpClientFactory httpClientFactory, IOptions<RobotLoginSettings> settings, ILogger<RobotLoginService> logger, IAiSimpleAnswerService aiSimpleAnswerService)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientType.ResilientClient);
        _logger = logger;
        _aiSimpleAnswerService = aiSimpleAnswerService;
        _settings = settings.Value;
    }

    public async Task<Result> PerformLoginAsync(CancellationToken ct)
    {
        var loginPageUrl = _settings.PageUrl;

        _logger.LogDebug("Getting login page content");

        var loginPageContent = await _httpClient.GetStringAsync(loginPageUrl, ct);

        var questionRegex = new Regex("""<p id="human-question">Question:<br />(.+?)</p>""", RegexOptions.Compiled, TimeSpan.FromSeconds(2));
        Match match;
        try
        {
            match = questionRegex.Match(loginPageContent);
        }
        catch (RegexMatchTimeoutException)
        {
            return new Result(false, "Could not find the question on the login page.");
        }

        if (!match.Success || match.Groups.Count < 2)
        {
            return new Result(false, "Could not find the question on the login page.");
        }

        var question = match.Groups[1].Value.Trim();

        _logger.LogDebug("Login page question: {Question}", question);

        var answer = await _aiSimpleAnswerService.GetAnswerToSimpleQuestionAsync(question, ct);

        _logger.LogDebug("Login page question answer: {Answer}", answer);

        var loginData = new Dictionary<string, string>
        {
            { "username", _settings.Username },
            { "password", _settings.Password },
            { "answer", answer }
        };

        var response = await _httpClient.PostAsync(loginPageUrl, new FormUrlEncodedContent(loginData), ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        return new Result(response.IsSuccessStatusCode, responseContent);
    }
}