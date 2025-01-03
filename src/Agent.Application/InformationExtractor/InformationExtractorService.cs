using System.Text;
using Agent.Application.Abstractions;
using Agent.Application.AudioToText;
using Agent.Application.Hq;
using Microsoft.Extensions.Logging;

namespace Agent.Application.InformationExtractor;

public class InformationExtractorService
{
    private readonly ILogger<InformationExtractorService> _logger;
    private readonly HqService _hqService;
    private readonly SpeechToTextService _speechToTextService;
    private readonly IAiService _aiService;

    public InformationExtractorService(ILogger<InformationExtractorService> logger, HqService hqService, SpeechToTextService speechToTextService, IAiService aiService)
    {
        _logger = logger;
        _hqService = hqService;
        _speechToTextService = speechToTextService;
        _aiService = aiService;
    }

    public async Task<Result> CompleteTask6Async(CancellationToken ct)
    {
        var audioFiles = Directory.GetFiles("data/przesluchania", "*.m4a", SearchOption.AllDirectories).ToList();
        var transcriptionFiles = new List<string>(audioFiles.Count);

        foreach (var audioFile in audioFiles)
        {
            _logger.LogInformation("Starting transcription of {AudioFile}", audioFile);
            var transcriptionFile = await _speechToTextService.ConvertAudioToText(audioFile, ".txt", "pl", ct);
            _logger.LogInformation("Finished transcription of {AudioFile}", audioFile);
            transcriptionFiles.Add(transcriptionFile);
        }

        _logger.LogInformation("Analyzing transcriptions");

        var combinedTestimonies = new StringBuilder();
        foreach (var transcriptionFile in transcriptionFiles)
        {
            var witnessName = Path.GetFileNameWithoutExtension(transcriptionFile);
            var testimony = await File.ReadAllTextAsync(transcriptionFile, ct);
            combinedTestimonies.Append($"<testimony witness=\"{witnessName}\">\n{testimony}\n</testimony>");
        }

        const string systemMessage = """
                                     Role: You are an investigator piecing together information about Professor Andrzej Maj's university institute street. Your task is to determine and clearly tag the specific street name of the institute where he teaches.

                                     <critical_instructions>
                                     1. ESSENTIAL: You must think OUT LOUD through your entire reasoning process
                                     2. CRUCIAL CONTEXT: 
                                     - The street name is not directly stated in the testimonies
                                     - You must use contextual clues from testimonies AND your internal knowledge
                                     - Some testimonies may be contradictory or unusual
                                     - Rafał's testimony should be given special attention but considered potentially unstable
                                     - Provided street names must be ignored as they are added to mislead
                                     3. REQUIRED: Place your final answer inside <answer></answer> tags
                                     </critical_instructions>

                                     Think out loud and provide a detailed explanation of your reasoning process. Your answer must be supported by clear evidence and logical connections.

                                     First figure out the city and institute name, then deduce the street name based on the testimonies.
                                     As the text comes in Polish conduct reasoning in Polish.

                                     <output_format>
                                         <thinking_process>
                                         .....
                                         </thinking_process>
                                         
                                         <answer>STREET_NAME</answer>
                                     </output_format>

                                     Remember: Provided street names must be ignored as they are added to mislead
                                     """;

        var userMessage = combinedTestimonies.ToString();

        var analysis = await _aiService.GetChatCompletionAsync(AiModelType.Gpt4o_202411, systemMessage, userMessage, ct);

        var match = System.Text.RegularExpressions.Regex.Match(analysis, "<answer>(.*?)</answer>");
        if (!match.Success)
        {
            throw new InvalidOperationException("Could not find street name in analysis response");
        }

        var streetName = match.Groups[1].Value;

        _logger.LogDebug("Sending street name: {StreetName}", streetName);

        var response = await _hqService.SendReportAsync("mp3", streetName, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("Response content: {ResponseContent}", responseContent);

        return new Result(response.IsSuccessStatusCode, responseContent);
    }
}