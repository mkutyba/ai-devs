using NorthernNerds.Aspire.Hosting.Neo4j;

var builder = DistributedApplication.CreateBuilder(args);

var param1 = builder.AddParameter("RobotLogin-PageUrl");
var param2 = builder.AddParameter("RobotLogin-Username", secret: true);
var param3 = builder.AddParameter("RobotLogin-Password", secret: true);
var param4 = builder.AddParameter("RobotVerifier-PageUrl");
var param5 = builder.AddParameter("Hq-BaseUrl");
var param6 = builder.AddParameter("Hq-ApiKey", secret: true);
var param7 = builder.AddParameter("Ai-OpenAI-ApiKey", secret: true);
var param8 = builder.AddParameter("Neo4j-User", secret: true);
var param9 = builder.AddParameter("Neo4j-Password", secret: true);

var ollama = builder.AddOllama("ollama").WithDataVolume().WithHttpEndpoint(port: 11434, targetPort: 11434, name: "ollama");
var phi35 = ollama.AddModel("ollama-phi", "phi3.5");
var llama31_8b = ollama.AddModel("llama3.1:8b");
if (OllamaSupportsGpu())
{
    ollama.WithContainerRuntimeArgs("--gpus=all");
}

var qdrant = builder.AddQdrant("qdrant")
    .WithOtlpExporter()
    .WithHttpEndpoint(port: 33267, targetPort: 6333, name: "dashboard", isProxied: false)
    .WithDataVolume("rag")
    .WithLifetime(ContainerLifetime.Persistent);

var n8n = builder
    .AddContainer("n8n", "n8nio/n8n")
    .WithEnvironment("GENERIC_TIMEZONE", TimeZoneInfo.Local.Id)
    .WithEnvironment("TZ", TimeZoneInfo.Local.Id)
    .WithVolume("n8n", "/home/node/.n8n")
    .WithHttpEndpoint(port: 5678, targetPort: 5678, name: "n8n-http", isProxied: false)
    .WithLifetime(ContainerLifetime.Persistent);

var neo4jDb = builder.AddNeo4j("graph-db", param8, param9);

builder.AddProject<Projects.Agent_API>("api")
    .WithReference(phi35)
    .WithReference(llama31_8b)
    .WithReference(qdrant)
    .WithReference(neo4jDb)
    .WithHttpHealthCheck("/health")
    .WithEnvironment("RobotLogin:PageUrl", param1)
    .WithEnvironment("RobotLogin:Username", param2)
    .WithEnvironment("RobotLogin:Password", param3)
    .WithEnvironment("RobotVerifier:PageUrl", param4)
    .WithEnvironment("Hq:BaseUrl", param5)
    .WithEnvironment("Hq:ApiKey", param6)
    .WithEnvironment("Ai:OpenAI:ApiKey", param7)
    .WithEnvironment("N8n:BaseUrl", n8n.GetEndpoint("n8n-http"));

builder.Build().Run();

bool OllamaSupportsGpu()
{
    return false;
}