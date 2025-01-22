using Agent.Application.Abstractions.Ai;
using Agent.Application.Hq;
using Microsoft.Extensions.Logging;

namespace Agent.Application.Censor;

public class CensorService
{
    private readonly ILogger<CensorService> _logger;
    private readonly HqService _hqService;
    private readonly IAiService _aiService;

    public CensorService(ILogger<CensorService> logger, HqService hqService, IAiService aiService)
    {
        _logger = logger;
        _hqService = hqService;
        _aiService = aiService;
    }

    public async Task<Result> CompleteTheTaskAsync(CancellationToken ct)
    {
        var data = await _hqService.GetDataToCensor(ct);
        _logger.LogInformation("Retrieved raw data: {Data}", data);
        var result = await CensorAgentData(data, ct);

        _logger.LogDebug("Sending answer: {Answer}", result);

        var response = await _hqService.SendReportAsync("CENZURA", result, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("Response content: {ResponseContent}", responseContent);

        return new Result(response.IsSuccessStatusCode, responseContent);
    }

    private async Task<string> CensorAgentData(string data, CancellationToken ct)
    {
        const string systemMessage = """
                                     You're a PII officer who helps anonymize provided data.
                                     <objective>
                                     You will be provided with a text that includes PII data of Polish citizens.
                                     Your task is to substitute each piece of PII data (name + surname, street name + number, city and person's age) with the word 'CENZURA.'
                                     </objective>

                                     <prompt_rules>
                                     - Identify the following types of PII in the sentence: name and surname, street name and number, city, and age.
                                     - street name and number should be treated as one element. For example 'ulica Modra 3' should be substituted as 'ulica CENZURA'
                                     - Substitute each identified PII element with the word 'CENZURA.'
                                     - You must NOT make other changes to the text. The rest of the sentence should remain intact.
                                     - Only the identified PII should be substituted; no partial substitutions or omissions should occur.
                                     - Under no circumstances should the sentence be altered except for the substitution of the specified PII data.
                                     - You must treat street name and number as one element and substitute them as one, e.g "ul. Piękna 5" -> "ul. CENZURA"
                                     - You must NOT change grammar or punctuation, e.g. "31 lat" -> "CENZURA lat" NOT "CENZURA lata"
                                     - You must keep the rest of the text unchanged no matter what.
                                     </prompt_rules>

                                     <prompt_examples>
                                     USER: Dane osoby podejrzanej: Marta Kowalska Zamieszkała w Warszawie na ulicy Modrej 3. Ma 58 lat.
                                     AI: Dane osoby podejrzanej: CENZURA. Zamieszkały w CENZURA na ulicy CENZURA. Ma CENZURA lat.

                                     USER: Informacje o podejrzanym:  Janusz Kowalski. Mieszka w Sopocie przy ulicy Broniewskiego 132. Wiek: 62 lata.
                                     AI: Informacje o podejrzanym: CENZURA. Mieszka w CENZURA przy ulicy CENZURA. Wiek: CENZURA lata.
                                     </prompt_examples>

                                     <dynamic_context>
                                     This prompt is intended to handle PII data for Polish citizens, substituting the specified personal data elements with "CENZURA" while leaving the rest of the sentence intact.
                                     </dynamic_context>

                                     <execution_validation>
                                     - Ensure that only the specified PII data is replaced with "CENZURA"
                                     - Verify that all other elements of the sentence remain unchanged.
                                     - Confirm that no partial replacements or omissions occur.
                                     </execution_validation>

                                     <output_structure>
                                     You must treat street name and number as one element and substitute them as one, e.g "ul. Piękna 5" -> "ul. CENZURA"
                                     You must NOT change grammar or punctuation, e.g. "31 lat" -> "CENZURA lat" NOT "CENZURA lata"
                                     You must output only provided text with PII information substituted with word "CENZURA"
                                     </output_structure>
                                     """;

        return await _aiService.GetChatCompletionAsync(AiModelType.Llama31_8b, systemMessage, data, ct);
    }
}