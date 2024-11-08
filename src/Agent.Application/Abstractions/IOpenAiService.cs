﻿namespace Agent.Application.Abstractions;

public interface IOpenAiService
{
    Task<string> GetAnswerToSimpleQuestionAsync(string question, CancellationToken ct);
    Task<string> GetAnswerToRobotVerificationQuestionJsonAsync(string question, CancellationToken ct);
}