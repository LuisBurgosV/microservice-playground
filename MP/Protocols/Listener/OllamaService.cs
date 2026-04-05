using System.Text;
using System.Text.Json;

namespace Listener
{
    public interface IOllamaService
    {
        Task<string> GenerateResponseAsync(string message, CancellationToken ct);
    }

    public class OllamaService : IOllamaService
    {
        private readonly HttpClient _httpClient;
        private const string OllamaUrl = "http://localhost:11434/api/generate";
        private const string Model = "llama3.1:8b";

        public OllamaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GenerateResponseAsync(string message, CancellationToken ct)
        {
            try
            {
                var requestBody = new
                {
                    model = Model,
                    prompt = message,
                    stream = false
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(OllamaUrl, jsonContent, ct);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(ct);
                using var jsonDoc = JsonDocument.Parse(responseContent);
                
                if (jsonDoc.RootElement.TryGetProperty("response", out var responseElement))
                {
                    return responseElement.GetString() ?? "No response from Ollama";
                }

                return responseContent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling Ollama: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
    }
}
