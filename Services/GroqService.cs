using System.Text;
using System.Text.Json;

namespace SmartLibrary.Services;

public class GroqService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GroqService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> AskAsync(string question)
    {
        var apiKey = _configuration["Groq:ApiKey"];

        _httpClient.DefaultRequestHeaders.Clear();

        _httpClient.DefaultRequestHeaders.Add(
            "Authorization",
            $"Bearer {apiKey}");

        var request = new
        {
            model = _configuration["Groq:Model"] ?? "llama-3.3-70b-versatile",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = question
                }
            }
        };

        var json = JsonSerializer.Serialize(request);

        var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            "https://api.groq.com/openai/v1/chat/completions",
            content);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();

        using JsonDocument doc = JsonDocument.Parse(result);

        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "Aucune réponse";
    }
}