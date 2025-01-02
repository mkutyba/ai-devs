using Agent.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Agent.Application.VisionAnalysis;

public class MapAnalysisService
{
    private readonly IAiService _aiService;
    private readonly ILogger<MapAnalysisService> _logger;

    public MapAnalysisService(IAiService aiService, ILogger<MapAnalysisService> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<string> GetMapDescriptionAsync(byte[] imageBytes, CancellationToken ct)
    {
        const string systemMessage = """
                                     You are an expert at analyzing modern Polish maps. List street names road numbers and landmarks from the map fragment that could help identify the city.
                                     Think out loud and describe each found element in detail. Do not focus on the most famous places for a given city, look at the bigger picture.
                                     Use cross-referencing these street names with cities in Poland.";
                                     """;
        // const string userMessageOld = """
        //                            Analyze the map fragment and describe what you see to identify the city. Provide reasoning for your answer first and then list the street names, road numbers,
        //                            and landmarks that could help identify the city. Use cross-referencing these street names with cities in Poland."
        //                            """;
        const string userMessage = """
                                   Analyze the map fragment and describe what you see to identify the city. List the street names, road numbers,
                                   and landmarks that could help identify the city. Use cross-referencing these street names with cities in Poland."
                                   """;

        var imageData = new ReadOnlyMemory<byte>(imageBytes);

        return await _aiService.GetVisionChatCompletionAsync(AiModelType.Gpt4o_202411, systemMessage, userMessage, [imageData], ct);
    }

    public async Task<string> GuessTheCityAsync(List<string> imageDescriptions, CancellationToken ct)
    {
        const string systemMessage = """
                                     You are an expert at analyzing historical maps and identifying cities.
                                     Use cross-referencing these street names with cities in Poland.
                                     You MUST respond in a valid XML format with the following structure:
                                     <reasoning>explanation of your reasoning</reasoning>
                                     <answer>identified city name</answer>
                                     """;

        var descriptions = string.Join("\n\n", imageDescriptions.Select((x, i) => $"Map Fragment {i + 1}:\n{x}"));
        var userMessage = $"""
                           Based on the following descriptions of map fragments, determine which city they represent.
                           Key information: 
                           - Use cross-referencing of the street names with cities in Poland. From provided data and your own knowledge try to assemble a coherent picture of the city
                           - 3 fragments show different parts of the same city (the one that we're looking for) while one fragment is from a different city
                           - Combine provided reasoning with your own knowledge of Polish cities to identify the city
                           - The city we are looking for has some granaries and fortresses, but they don't appear in maps
                           - One fragment might be from a different city - it's crucial to identify any mismatching fragments
                           - Return ONLY an XML response without any additional text, just XML string

                           {descriptions}
                           """;

        var analysis = await _aiService.GetChatCompletionAsync(AiModelType.Gpt4o_202411, systemMessage, userMessage, ct);

        var match = System.Text.RegularExpressions.Regex.Match(analysis, "<answer>(.*?)</answer>");
        if (!match.Success)
        {
            throw new InvalidOperationException("Could not find city name in analysis response");
        }

        var cityName = match.Groups[1].Value;

        _logger.LogInformation("Full analysis: {Analysis}", analysis);
        _logger.LogInformation("City name: {CityName}", cityName);

        return cityName;
    }

    public async Task<Result> CompleteTask7Async(CancellationToken ct)
    {
        var imagePaths = Directory.GetFiles("data/mapka", "*.png", SearchOption.AllDirectories).ToList();

        var descriptions = new List<string>();
        foreach (var imagePath in imagePaths)
        {
            var imageBytes = await File.ReadAllBytesAsync(imagePath, ct);
            _logger.LogDebug("Processing image: {ImagePath}", imagePath);

            var description = await GetMapDescriptionAsync(imageBytes, ct);
            descriptions.Add(description);
            _logger.LogDebug("Image description: {Description}", description);
        }

        var result = await GuessTheCityAsync(descriptions, ct);

        return new Result(true, result);
    }
}