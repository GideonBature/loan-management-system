using FirstLend.Application.Abstractions;
using Google.GenAI;
using Microsoft.Extensions.Configuration;


namespace FirstLend.Infrastructure.Services
{
public class GeminiService : IGeminiService
 {
private readonly Client _client;
private const string ModelName = "gemini-2.5-flash";

public GeminiService(IConfiguration configuration)
{
var apiKey = configuration["Gemini:ApiKey"];
if (string.IsNullOrWhiteSpace(apiKey))
throw new InvalidOperationException("Gemini API key is missing in configuration.");

_client = new Client(apiKey: apiKey);
 }

public async Task<string> AnalyzeLoanDataAsync(string prompt, CancellationToken cancellationToken = default)
{
var response = await _client.Models.GenerateContentAsync(
 model: ModelName,
contents: prompt
);

return response.Candidates[0].Content.Parts[0].Text;
}
    }
}