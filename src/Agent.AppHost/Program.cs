var builder = DistributedApplication.CreateBuilder(args);

var param1 = builder.AddParameter("RobotLogin-PageUrl");
var param2 = builder.AddParameter("RobotLogin-Username");
var param3 = builder.AddParameter("RobotLogin-Password");
var param4 = builder.AddParameter("RobotVerifier-PageUrl");
var param5 = builder.AddParameter("JsonCompleter-ReportUrl");
var param6 = builder.AddParameter("JsonCompleter-ApiKey", secret: true);
var param7 = builder.AddParameter("OpenAi-ApiKey", secret: true);

builder.AddProject<Projects.Agent_API>("api")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("RobotLogin:PageUrl", param1)
    .WithEnvironment("RobotLogin:Username", param2)
    .WithEnvironment("RobotLogin:Password", param3)
    .WithEnvironment("RobotVerifier:PageUrl", param4)
    .WithEnvironment("JsonCompleter:ReportUrl", param5)
    .WithEnvironment("JsonCompleter:ApiKey", param6)
    .WithEnvironment("OpenAi:ApiKey", param7);

builder.Build().Run();