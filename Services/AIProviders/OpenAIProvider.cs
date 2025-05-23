using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
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

        public async Task<string> GetCompletionAsync(Prompt prompt)
        {
            try
            {
                _loggingService.LogInfo($"Sending {prompt.PromptType} prompt to OpenAI using new Prompt class");

                var requestData = new
                {
                    model = _modelName,
                    messages = new[]
                    {
                        new { role = "system", content = prompt.SystemPrompt },
                        new { role = "user", content = prompt.PromptContent }
                    },
                    max_tokens = _maxTokens,
                    temperature = _temperature
                };

                var json = JsonSerializer.Serialize(requestData);
                var requestContent = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                // Preprocess the JSON for better logging readability
                var prettyJson = PreprocessJsonForLogging(json);
                _loggingService.LogFormattedInfo("request", prettyJson);
                
                var response = await _httpClient.PostAsync("chat/completions", requestContent);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                // Preprocess the response JSON for better logging
                var prettyResponse = PreprocessJsonForLogging(responseContent);
                _loggingService.LogFormattedInfo("response", prettyResponse);
                
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

        private string PreprocessJsonForLogging(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json;
                
            // Replace escape sequences directly in the JSON string
            string processed = json
                .Replace("\\r\\n", Environment.NewLine)
                .Replace("\\n", Environment.NewLine)
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\'", "'")
                .Replace("\\\\", "\\");
                
            // Replace unicode escapes
            processed = System.Text.RegularExpressions.Regex.Replace(
                processed, 
                "\\\\u([0-9a-fA-F]{4})", 
                match => {
                    string hex = match.Groups[1].Value;
                    int code = Convert.ToInt32(hex, 16);
                    return ((char)code).ToString();
                });
                
            return processed;
        }
    }
}
