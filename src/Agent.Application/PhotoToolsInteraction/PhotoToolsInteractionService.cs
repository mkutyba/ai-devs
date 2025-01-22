using Agent.Application.Abstractions.Ai;
using Agent.Application.Hq;
using Agent.Application.ImageDescriptor;
using Microsoft.Extensions.Logging;

namespace Agent.Application.PhotoToolsInteraction;

public class PhotoToolsInteractionService
{
    private readonly ILogger<PhotoToolsInteractionService> _logger;
    private readonly HqService _hqService;
    private readonly IAiService _aiService;
    private readonly ImageDescriptorService _imageDescriptorService;
    private readonly HttpClient _httpClient;

    public PhotoToolsInteractionService(ILogger<PhotoToolsInteractionService> logger, HqService hqService, IAiService aiService, ImageDescriptorService imageDescriptorService,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _hqService = hqService;
        _aiService = aiService;
        _imageDescriptorService = imageDescriptorService;
        _httpClient = httpClientFactory.CreateClient(HttpClientType.ResilientClient);
    }

    public async Task<Result> CompleteTask16Async(CancellationToken ct)
    {
        var initialResponse = await _hqService.SendReportAsync("photos", "START", ct);
        var photoUrls = await ExtractPhotoUrlsPrompt(await initialResponse.Content.ReadAsStringAsync(ct), ct);

        List<string> photoResults = [];
        foreach (var photoUrl in photoUrls)
        {
            var photoResult = await ProcessSinglePhoto(photoUrl, ct);
            _logger.LogInformation("Processing result: {Result}", photoResult);

            if (!string.IsNullOrEmpty(photoResult))
            {
                photoResults.Add(photoResult);
            }
        }

        var result = await GeneratePersonDescription(photoResults, ct);

        _logger.LogDebug("Sending result: {Result}", result);

        var response = await _hqService.SendReportAsync("photos", result, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("Response content: {ResponseContent}", responseContent);

        return new Result(response.IsSuccessStatusCode, responseContent);
    }

    private async Task<string> GeneratePersonDescription(List<string> photoResults, CancellationToken ct)
    {
        var userMessage = $"""
                           Based on the following photo analyses, create a detailed Polish language description (rysopis) of a person:
                           <descriptions>
                           {string.Join("\n\n", photoResults)}
                           </descriptions>

                           Extract the most important features and characteristics a person from all provided descriptions.
                           Make it formal, precise, and focused on permanent physical characteristics.
                           If some information is inconsistent or unclear, use the most common or repeated details.
                           Use Polish language
                           Avoid mentioning temporary features like clothing unless they appear consistently across photos.
                           """;

        return await _aiService.GetChatCompletionAsync(AiModelType.Gpt4o_202411, string.Empty, userMessage, ct);
    }

    private async Task<List<string>> ExtractPhotoUrlsPrompt(string text, CancellationToken ct)
    {
        const string systemMessage = """
                                     You are working as a precise text processor. Extract photo URLs from the text.

                                     1. Extract the base URL (e.g., https://example.com/photos/)
                                     2. Extract all image filenames (e.g., photo1.jpg)
                                     3. Combine them to create full URLs

                                     Return the complete URLs, one per line, in this format:
                                     https://example.com/photos/photo1.jpg
                                     https://example.com/photos/photo2.jpg
                                     etc.

                                     Return only the complete URLs, nothing else, each in separate line.

                                     If you can't find the full URL, return only the filename.
                                     """;

        var userMessage = $"""
                           From this text extract photo URLs:
                           <text>
                           {text}
                           </text>
                           """;

        var result = await _aiService.GetChatCompletionAsync(AiModelType.Gpt4o_202411, systemMessage, userMessage, ct);
        return [.. result.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }

    private async Task<string?> ProcessSinglePhoto(string? photoUrl, CancellationToken ct)
    {
        while (true)
        {
            _logger.LogInformation("Processing photo: {Url}", photoUrl);

            if (photoUrl == null)
            {
                return null;
            }

            var systemMessage = """
                                You are working as a photo analyst. The goal is to decide whether it's good quality and you can describe the photo or it needs additional processing.

                                Important: If you cannot clearly identify or analyze the person in the photo, use the available correction tools to improve image quality before analysis:
                                - repair_photo for removing noise and glitches
                                - brighten_photo for too dark images
                                - darken_photo for overexposed (too bright) images

                                Otherwise, if the photo is clear and you can describe the person, proceed with the analysis.

                                Evaluate image quality for identification:
                                - If features are unclear or hard to distinguish, apply appropriate corrections and return the required correction type as a result, nothing more,
                                  no additional characters, one of: repair_photo, brighten_photo, darken_photo
                                - if the person is clearly visible, respond with describe as a result, nothing more, no additional characters

                                Expected response (one of the below):
                                - repair_photo
                                - brighten_photo
                                - darken_photo
                                - describe

                                Very important: if the photo is clear and good quality, and you can see the person, respond with describe.
                                """;

            var userMessage = $"Analyze this photo: {photoUrl}";

            ReadOnlyMemory<byte> imageBytes = await _httpClient.GetByteArrayAsync(photoUrl, ct);

            // var result = await _aiService.GetChatCompletionAsync(AiModelType.Gpt4o_202411, systemMessage, userMessage, ct);
            var result = await _aiService.GetChatCompletionWithImagesAsync(AiModelType.Gpt4o_202411, systemMessage, userMessage, [imageBytes], ct);

            _logger.LogInformation("Generated result: {Result}", result);

            switch (result)
            {
                case "repair_photo":
                    photoUrl = await RepairPhoto(photoUrl, ct);
                    continue;
                case "brighten_photo":
                    photoUrl = await BrightenPhoto(photoUrl, ct);
                    continue;
                case "darken_photo":
                    photoUrl = await DarkenPhoto(photoUrl, ct);
                    continue;
                case "describe":
                    return await DescribePhoto(photoUrl, ct);
            }
        }
    }

    private async Task<string?> DescribePhoto(string? photoUrl, CancellationToken ct)
    {
        _logger.LogInformation("Describing photo: {Url}", photoUrl);

        if (photoUrl == null)
        {
            return null;
        }

        // var systemMessage = """
        //                     Analyze the photo and provide a detailed description of the person.
        //                     It may happen that analyzed image does not contain a person. In such case return only 'No person detected' message (without quotes).
        //                     """;

        // var userMessage = $"Describe this photo: {photoUrl}";

        // var result = await _aiService.GetChatCompletionAsync(AiModelType.Gpt4o_202411, systemMessage, userMessage, ct);

        // return result == "No person detected" ? null : result;

        var result = await _imageDescriptorService.DescribeImageAsync(photoUrl, string.Empty, string.Empty, ct);

        _logger.LogInformation("Generated result: {Result}", result);

        return result;
    }

    private async Task<string?> RepairPhoto(string photoUrl, CancellationToken ct)
    {
        _logger.LogInformation("Repairing photo: {Url}", photoUrl);

        var fileName = Path.GetFileName(photoUrl);
        var result = await _hqService.SendReportAsync("photos", $"REPAIR {fileName}", ct);
        var urls = await ExtractPhotoUrlsPrompt(await result.Content.ReadAsStringAsync(ct), ct);

        var resultUrl = urls.Count > 0 ? urls.First() : null;
        _logger.LogInformation("Repaired photo: {Url}", resultUrl);

        if (resultUrl == null)
        {
            _logger.LogWarning("Failed to repair photo: {Url}", photoUrl);
            return null;
        }

        return photoUrl.Replace(fileName, Path.GetFileName(resultUrl));
    }

    private async Task<string?> BrightenPhoto(string photoUrl, CancellationToken ct)
    {
        _logger.LogInformation("Brightening photo: {Url}", photoUrl);

        var fileName = Path.GetFileName(photoUrl);
        var result = await _hqService.SendReportAsync("photos", $"BRIGHTEN {fileName}", ct);
        var urls = await ExtractPhotoUrlsPrompt(await result.Content.ReadAsStringAsync(ct), ct);

        var resultUrl = urls.Count > 0 ? urls.First() : null;
        _logger.LogInformation("Brightened photo: {Url}", resultUrl);

        return photoUrl.Replace(fileName, Path.GetFileName(resultUrl));
    }

    private async Task<string?> DarkenPhoto(string photoUrl, CancellationToken ct)
    {
        _logger.LogInformation("Darkening photo: {Url}", photoUrl);

        var fileName = Path.GetFileName(photoUrl);
        var result = await _hqService.SendReportAsync("photos", $"DARKEN {fileName}", ct);
        var urls = await ExtractPhotoUrlsPrompt(await result.Content.ReadAsStringAsync(ct), ct);

        var resultUrl = urls.Count > 0 ? urls.First() : null;
        _logger.LogInformation("Darkened photo: {Url}", resultUrl);

        return photoUrl.Replace(fileName, Path.GetFileName(resultUrl));
    }
}