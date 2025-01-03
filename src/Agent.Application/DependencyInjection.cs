﻿using Agent.Application.Ai;
using Agent.Application.AudioToText;
using Agent.Application.Censor;
using Agent.Application.Hq;
using Agent.Application.InformationClassifier;
using Agent.Application.InformationExtractor;
using Agent.Application.JsonCompleter;
using Agent.Application.RobotLogin;
using Agent.Application.RobotVerifier;
using Agent.Application.VisionAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Agent.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services
            .AddHttpClient(HttpClientType.ResilientClient)
            .AddStandardResilienceHandler();
        services.AddTransient<RobotLoginService>();
        services.AddTransient<RobotVerifierService>();
        services.AddTransient<JsonCompleterService>();
        services.AddTransient<HqService>();
        services.AddTransient<IAiSimpleAnswerService, AiSimpleAnswerService>();
        services.AddTransient<CensorService>();
        services.AddTransient<InformationExtractorService>();
        services.AddTransient<SpeechToTextService>();
        services.AddTransient<MapAnalysisService>();
        services.AddTransient<ImageGenerationService>();
        services.AddTransient<InformationClassifierService>();

        return services;
    }
}