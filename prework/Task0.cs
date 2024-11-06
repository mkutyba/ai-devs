using System.Net.Http.Json;

namespace ai_devs3;

public class Task0(HttpClient httpClient)
{
    public async Task<ResponseModel?> Run()
    {
        var apiKey = Environment.GetEnvironmentVariable("API_KEY") ?? string.Empty;

        var dataRs = await httpClient.GetAsync("dane.txt");
        dataRs.EnsureSuccessStatusCode();
        var data = await dataRs.Content.ReadAsStringAsync();
        var strings = data.Split("\n");

        var rq = new RequestModel
        {
            Task = "POLIGON",
            Apikey = apiKey,
            Answer = strings.Where(x => !string.IsNullOrEmpty(x)).ToArray(),
        };

        var rs = await httpClient.PostAsJsonAsync("verify", rq);
        return await rs.Content.ReadFromJsonAsync<ResponseModel>();
    }
}