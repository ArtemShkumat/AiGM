using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace AiGMBackEnd.Services.AIProviders
{
    public class OpenAIProvider : IAIProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _modelName;
        private readonly int _maxTokens;
        private readonly float _temperature;
        private readonly LoggingService _loggingService;
        private readonly string _apiKey;

        public string Name => "OpenAI";

        public OpenAIProvider(IConfiguration configuration, LoggingService loggingService)
        {
            _loggingService = loggingService;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");

            _apiKey = configuration["AIProviders:OpenAI:ApiKey"];
            _modelName = configuration["AIProviders:OpenAI:ModelName"] ?? "gpt-4o";
            
            if (!int.TryParse(configuration["AIProviders:OpenAI:MaxTokens"], out _maxTokens))
            {
                _maxTokens = 2000;
            }
            
            if (!float.TryParse(configuration["AIProviders:OpenAI:Temperature"], out _temperature))
            {
                _temperature = 0.7f;
            }

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new ArgumentException("OpenAI API key is missing in configuration.");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> GetCompletionAsync(string prompt, string promptType)
        {
            try
            {
                _loggingService.LogInfo($"Sending {promptType} prompt to OpenAI");

                var requestData = new
                {
                    model = _modelName,
                    messages = new[]
                    {
                        new { role = "system", content = "You are an AI game master helping with a text-based RPG game." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = _maxTokens,
                    temperature = _temperature
                };

                var requestContent = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("chat/completions", requestContent);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (responseObject.TryGetProperty("choices", out var choices) && 
                    choices.GetArrayLength() > 0 && 
                    choices[0].TryGetProperty("message", out var message) && 
                    message.TryGetProperty("content", out var content))
                {
                    return content.GetString();
                }

                throw new Exception("Failed to parse response from OpenAI.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in OpenAI provider: {ex.Message}");
                throw;
            }
        }
    }
}
