using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Agent.Application.Hq;

public class HqService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HqService> _logger;
    private readonly HqSettings _settings;

    public HqService(HttpClient httpClient, ILogger<HqService> logger, IOptions<HqSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<HttpResponseMessage> SendReportAsync(string taskName, object answer, CancellationToken ct)
    {
        var url = $"{_settings.BaseUrl.TrimEnd('/')}/report";
        var request = new HqRequestModel
        {
            Task = taskName,
            Apikey = _settings.ApiKey,
            Answer = answer
        };
        var requestJson = JsonConvert.SerializeObject(request, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content, ct);

        return response;
    }
}