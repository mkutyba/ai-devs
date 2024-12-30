var builder = DistributedApplication.CreateBuilder(args);

var param1 = builder.AddParameter("RobotLogin-PageUrl");
var param2 = builder.AddParameter("RobotLogin-Username");
var param3 = builder.AddParameter("RobotLogin-Password");
var param4 = builder.AddParameter("RobotVerifier-PageUrl");
var param5 = builder.AddParameter("Hq-BaseUrl");
var param6 = builder.AddParameter("Hq-ApiKey", secret: true);
var param7 = builder.AddParameter("Ai-OpenAI-ApiKey", secret: true);

var ollama = builder.AddOllama("ollama").WithDataVolume().WithHttpEndpoint(port: 11434, targetPort: 11434, name: "ollama");
var phi35 = ollama.AddModel("ollama-phi", "phi3.5");
var llama31_8b = ollama.AddModel("llama3.1:8b");
if (OllamaSupportsGpu())
{
    ollama.WithContainerRuntimeArgs("--gpus=all");
}

builder.AddProject<Projects.Agent_API>("api")
    .WithReference(phi35)
    .WithReference(llama31_8b)
    .WithHttpHealthCheck("/health")
    .WithEnvironment("RobotLogin:PageUrl", param1)
    .WithEnvironment("RobotLogin:Username", param2)
    .WithEnvironment("RobotLogin:Password", param3)
    .WithEnvironment("RobotVerifier:PageUrl", param4)
    .WithEnvironment("Hq:BaseUrl", param5)
    .WithEnvironment("Hq:ApiKey", param6)
    .WithEnvironment("Ai:OpenAI:ApiKey", param7);

builder.Build().Run();

bool OllamaSupportsGpu()
{
    return false;
}