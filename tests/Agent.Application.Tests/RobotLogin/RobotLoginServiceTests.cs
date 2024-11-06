using System.Net;
using Agent.Application.RobotLogin;
using Agent.Infrastructure.OpenAi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace Agent.Application.Tests.RobotLogin;

public class RobotLoginServiceTests
{
    private readonly HttpClient _httpClient;
    private readonly IOpenAiService _openAiService;
    private readonly ILogger<RobotLoginService> _logger;
    private readonly IOptions<RobotLoginSettings> _robotLoginSettings;
    private readonly RobotLoginService _sut;
    private readonly MockHttpMessageHandler _handlerMock = new ();

    public RobotLoginServiceTests()
    {
        _httpClient = new HttpClient(_handlerMock);
        _openAiService = Substitute.For<IOpenAiService>();
        _logger = Substitute.For<ILogger<RobotLoginService>>();
        _robotLoginSettings = Options.Create(new RobotLoginSettings
        {
            PageUrl = "http://example.com/login",
            Username = "testuser",
            Password = "testpassword"
        });

        _sut = new RobotLoginService(
            _httpClient,
            _robotLoginSettings,
            _openAiService,
            _logger);
    }

    [Fact]
    public async Task PerformLoginAsync_ShouldReturnSuccess_WhenLoginIsSuccessful()
    {
        // Arrange
        var loginPageContent = """<p id="human-question">Question:<br />What is 2+2?</p>""";
        var answer = "4";

        _handlerMock
            .When(HttpMethod.Get, "http://example.com/login")
            .Respond(HttpStatusCode.OK, "text/plain", loginPageContent);

        _handlerMock
            .When(HttpMethod.Post, "http://example.com/login")
            .Respond(HttpStatusCode.OK, "text/plain", "Login successful");

        _openAiService.GetAnswerToSimpleQuestionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(answer));

        // Act
        var result = await _sut.PerformLoginAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Login successful", result.Content);
    }

    [Fact]
    public async Task PerformLoginAsync_ShouldReturnFailure_WhenQuestionNotFound()
    {
        // Arrange
        var loginPageContent = "<p>No question here</p>";

        _handlerMock
            .When("http://example.com/login")
            .Respond(HttpStatusCode.OK, "text/plain", loginPageContent);

        // Act
        var result = await _sut.PerformLoginAsync(CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Could not find the question on the login page.", result.Content);
    }
}