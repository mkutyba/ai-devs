using ai_devs3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://poligon.aidevs.pl/");

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    services.AddSingleton(httpClient);
    services.AddTransient<Task0>();
});

var app = builder.Build();

var task0 = app.Services.GetRequiredService<Task0>();
var response = await task0.Run();
Console.WriteLine(response?.Code);
Console.WriteLine(response?.Message);
