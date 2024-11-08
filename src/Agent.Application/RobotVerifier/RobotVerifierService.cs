using System.Net.Http.Json;
using Agent.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agent.Application.RobotVerifier;

public interface IRobotVerifierService
{
    Task<Result> VerifyAsync(CancellationToken ct);
}

public class RobotVerifierService : IRobotVerifierService
{
    private readonly HttpClient _httpClient;
    private readonly IOpenAiService _openAiService;
    private readonly ILogger<RobotVerifierService> _logger;
    private readonly RobotVerifierSettings _settings;

    public RobotVerifierService(HttpClient httpClient, IOpenAiService openAiService, ILogger<RobotVerifierService> logger, IOptions<RobotVerifierSettings> settings)
    {
        _httpClient = httpClient;
        _openAiService = openAiService;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<Result> VerifyAsync(CancellationToken ct)
    {
        var pageUrl = _settings.PageUrl;

        _logger.LogDebug("Sending READY message");

        var readyMessage = new RobotVerifierMessage
        {
            Text = "READY",
            MsgID = 0
        };
        var result1 = await _httpClient.PostAsJsonAsync(pageUrl, readyMessage, ct);

        var question = await result1.Content.ReadFromJsonAsync<RobotVerifierMessage>(ct);

        if (question == null)
        {
            return new Result(false, "Could not get question from the verifier");
        }

        _logger.LogDebug("Question: {Question}", question.Text);

        var aiResponse = await _openAiService.GetAnswerToRobotVerificationQuestionJsonAsync(question.Text, ct);

        var aiResponseMessage = new RobotVerifierMessage
        {
            Text = aiResponse,
            MsgID = question.MsgID
        };
        _logger.LogDebug("AI response: {AiResponse}", aiResponse);
        var result2 = await _httpClient.PostAsJsonAsync(pageUrl, aiResponseMessage, ct);

        var finalResponse = await result2.Content.ReadFromJsonAsync<RobotVerifierMessage>(ct);

        if (finalResponse == null)
        {
            return new Result(false, "Could not get the final response from the verifier");
        }

        _logger.LogDebug("Verification result: {FinalResponse}", finalResponse.Text);

        var isOk = finalResponse.Text == "OK" || finalResponse.Text.StartsWith("{{FLG:");
        return new Result(isOk, finalResponse.Text);
    }
}