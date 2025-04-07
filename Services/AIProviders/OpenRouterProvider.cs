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
    public class OpenRouterProvider : IAIProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _modelName;
        private readonly int _maxTokens;
        private readonly float _temperature;
        private readonly LoggingService _loggingService;
        private readonly string _apiKey;

        public string Name => "OpenRouter";

        public OpenRouterProvider(IConfiguration configuration, LoggingService loggingService)
        {
            _loggingService = loggingService;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://openrouter.ai/api/v1/");

            _apiKey = configuration["AIProviders:OpenRouter:ApiKey"];
            _modelName = configuration["AIProviders:OpenRouter:ModelName"] ?? "google/gemini-2.0-flash-001";
            
            if (!int.TryParse(configuration["AIProviders:OpenRouter:MaxTokens"], out _maxTokens))
            {
                _maxTokens = 2000;
            }
            
            if (!float.TryParse(configuration["AIProviders:OpenRouter:Temperature"], out _temperature))
            {
                _temperature = 0.7f;
            }

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new ArgumentException("OpenRouter API key is missing in configuration.");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            // Required for OpenRouter - identify your application
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://aigm.app");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "AIGM Backend");
        }

        public async Task<string> GetCompletionAsync(Prompt prompt)
        {
            try
            {
                _loggingService.LogInfo($"Sending {prompt.PromptType} prompt to OpenRouter using new Prompt class");

                object requestPayload;
                if (!string.IsNullOrEmpty(prompt.OutputStructureJsonSchema))
                {
                    // If schema is provided, use structured output format
                    _loggingService.LogInfo("Detected OutputStructureJsonSchema. Using structured output config.");
                    requestPayload = new
                    {
                        model = _modelName,
                        messages = new[]
                        {
                            new { role = "system", content = prompt.SystemPrompt },
                            new { role = "user", content = prompt.PromptContent }
                        },
                        max_tokens = _maxTokens,
                        temperature = _temperature,
                        response_format = new { type = "json_object", schema = JsonDocument.Parse(prompt.OutputStructureJsonSchema).RootElement } // Parse the schema string
                    };
                }
                else
                {
                    // Otherwise, use the standard request format
                    requestPayload = new
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
                }

                var json = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { WriteIndented = false }); // Avoid pretty printing for the actual request
                var requestContent = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                // Preprocess the JSON for better logging readability
                //var prettyJson = PreprocessJsonForLogging(json);
                //_loggingService.LogFormattedInfo("request", prettyJson);
                _loggingService.LogFormattedInfo("request", prompt.PromptContent);

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

                throw new Exception("Failed to parse response from OpenRouter.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in OpenRouter provider: {ex.Message}");
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