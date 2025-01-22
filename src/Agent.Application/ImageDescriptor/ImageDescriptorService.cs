using System.Text;
using Agent.Application.Abstractions.Ai;

namespace Agent.Application.ImageDescriptor;

public class ImageDescriptorService
{
    private readonly IAiService _aiService;
    private readonly HttpClient _httpClient;

    public ImageDescriptorService(IHttpClientFactory httpClientFactory, IAiService aiService)
    {
        _aiService = aiService;
        _httpClient = httpClientFactory.CreateClient(HttpClientType.ResilientClient);
    }

    public async Task<string> DescribeImageAsync(Uri url, string alt, string caption, CancellationToken ct)
    {
        try
        {
            var context = new StringBuilder();
            ReadOnlyMemory<byte> imageBytes = await _httpClient.GetByteArrayAsync(url, ct);

            if (!string.IsNullOrEmpty(alt))
            {
                context.AppendLine($"Alt Text: {alt}");
            }

            if (!string.IsNullOrEmpty(caption))
            {
                context.AppendLine($"Caption: {caption}");
            }

            var systemMessage = """
                                You are a helpful assistant that describes images in detail based on their metadata.
                                Focus on creating a clear and detailed description that captures the main subject and important details.
                                Describe subject and other objects with matching nouns.
                                Describe what is the place visible on the image.
                                """;

            var imageDescription = await _aiService.GetChatCompletionWithImagesAsync(
                AiModelType.Gpt4o_202411,
                systemMessage,
                $"Based on this image metadata and content, provide a detailed description in Polish:\n{context}",
                [imageBytes],
                ct);

            return imageDescription + "\n\n" + context;
        }
        catch (Exception)
        {
            return $"{alt}\n{caption}";
        }
    }
}