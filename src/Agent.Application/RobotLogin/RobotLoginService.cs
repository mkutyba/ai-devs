using System.Text.RegularExpressions;
using Agent.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agent.Application.RobotLogin;

public interface IRobotLoginService
{
    Task<RobotLoginResult> PerformLoginAsync(CancellationToken ct);
}

public class RobotLoginService : IRobotLoginService
{
    private readonly HttpClient _httpClient;
    private readonly IOpenAiService _openAiService;
    private readonly ILogger<RobotLoginService> _logger;
    private readonly RobotLoginSettings _robotLoginSettings;

    public RobotLoginService(HttpClient httpClient, IOptions<RobotLoginSettings> robotLoginSettings, IOpenAiService openAiService, ILogger<RobotLoginService> logger)
    {
        _httpClient = httpClient;
        _openAiService = openAiService;
        _logger = logger;
        _robotLoginSettings = robotLoginSettings.Value;
    }

    public async Task<RobotLoginResult> PerformLoginAsync(CancellationToken ct)
    {
        var loginPageUrl = _robotLoginSettings.PageUrl;

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
            return new RobotLoginResult(false, "Could not find the question on the login page.");
        }

        if (!match.Success || match.Groups.Count < 2)
        {
            return new RobotLoginResult(false, "Could not find the question on the login page.");
        }

        var question = match.Groups[1].Value.Trim();

        _logger.LogDebug("Login page question: {Question}", question);

        var answer = await _openAiService.GetAnswerToSimpleQuestionAsync(question, ct);

        _logger.LogDebug("Login page question answer: {Answer}", answer);

        var loginData = new Dictionary<string, string>
        {
            { "username", _robotLoginSettings.Username },
            { "password", _robotLoginSettings.Password },
            { "answer", answer }
        };

        var response = await _httpClient.PostAsync(loginPageUrl, new FormUrlEncodedContent(loginData));
        var responseContent = await response.Content.ReadAsStringAsync();

        return new RobotLoginResult(response.IsSuccessStatusCode, responseContent);
    }
}