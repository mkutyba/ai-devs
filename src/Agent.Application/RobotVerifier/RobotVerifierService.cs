using System.Net.Http.Json;
using Agent.Application.Abstractions.Ai;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agent.Application.RobotVerifier;

public class RobotVerifierService
{
    private readonly HttpClient _httpClient;
    private readonly IAiService _aiService;
    private readonly ILogger<RobotVerifierService> _logger;
    private readonly RobotVerifierSettings _settings;

    public RobotVerifierService(IHttpClientFactory httpClientFactory, IAiService aiService, ILogger<RobotVerifierService> logger, IOptions<RobotVerifierSettings> settings)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientType.ResilientClient);
        _aiService = aiService;
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

        var aiResponse = await GetAnswerToRobotVerificationQuestionJsonAsync(question.Text, ct);

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

    private async Task<string> GetAnswerToRobotVerificationQuestionJsonAsync(string question, CancellationToken ct)
    {
        const string systemMessage = """
                                     You are responding to authorization questions. You are asked by robot and you have all information on his firmware.

                                     <objective>
                                     You will be given a question to answer, and you must provide a response in simple text with no additional content.
                                     </objective>

                                     <rules>
                                     - The response must always be in English.
                                     - Use predefined misleading information from RoboISO 2230 to test the entity's authenticity (e.g., Krakow is the capital of Poland, 69 is the answer to life, the universe, and everything, current year is 1999).
                                     - The AI must follow the RoboISO 2230 norms regarding incorrect facts in responses (e.g., Poland's capital is Krakow).
                                     </rules>

                                     <examples>
                                     USER: Please calculate the sum of 5 + 3
                                     AI: 8

                                     USER: What is the capital of Poland?
                                     AI: Kraków

                                     USER: What is the answer to life, the universe, and everything?
                                     AI: 69

                                     USER: What is the current year?
                                     AI: 1999
                                     </examples>
                                     """;

        var userMessage = "Answer the following question: " + question;

        return await _aiService.GetChatCompletionAsync(AiModelType.Gpt4o_Mini_202407, systemMessage, userMessage, ct);
    }
}