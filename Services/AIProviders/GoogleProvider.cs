using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using Microsoft.Extensions.Configuration;

namespace AiGMBackEnd.Services.AIProviders
{
    public class GoogleProvider : IAIProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _modelName;
        private readonly int _maxTokens;
        private readonly float _temperature;
        private readonly LoggingService _loggingService;
        private readonly string _apiKey;
        private readonly string _basePath;

        public string Name => "Google";

        public GoogleProvider(IConfiguration configuration, LoggingService loggingService)
        {
            _loggingService = loggingService;
            _httpClient = new HttpClient();
            
            _apiKey = configuration["AIProviders:Google:ApiKey"];
            _modelName = configuration["AIProviders:Google:ModelName"] ?? "gemini-2.0-flash";
            _basePath = AppDomain.CurrentDomain.BaseDirectory;
            
            if (!int.TryParse(configuration["AIProviders:Google:MaxTokens"], out _maxTokens))
            {
                _maxTokens = 2000;
            }
            
            if (!float.TryParse(configuration["AIProviders:Google:Temperature"], out _temperature))
            {
                _temperature = 0.7f;
            }

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new ArgumentException("Google API key is missing in configuration.");
            }

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> GetCompletionAsync(Prompt prompt)
        {
            try
            {
                _loggingService.LogInfo($"Sending {prompt.PromptType} prompt to Google Gemini using model {_modelName}");

                // Construct the API endpoint URL with the API key
                string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent?key={_apiKey}";

                object requestPayload;

                // Create the base request with system instruction and user content
                var baseRequest = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[]
                            {
                                new { text = prompt.PromptContent }
                            }
                        }
                    },
                    systemInstruction = new
                    {
                        parts = new[]
                        {
                            new { text = prompt.SystemPrompt }
                        }
                    }
                };

                // Add structured output if schema is provided
                if (!string.IsNullOrEmpty(prompt.OutputStructureJsonSchema))
                {
                    
                    
                    string schemaToUse = prompt.OutputStructureJsonSchema;                    
                    
                    var schemaElement = JsonSerializer.Deserialize<JsonElement>(schemaToUse);
                    
                    // Create request with structured output configuration
                    requestPayload = new
                    {
                        contents = baseRequest.contents,
                        systemInstruction = baseRequest.systemInstruction,
                        generationConfig = new
                        {
                            temperature = _temperature,
                            maxOutputTokens = _maxTokens,
                            response_mime_type = "application/json",
                            response_schema = schemaElement
                        }
                    };
                }
                else
                {
                    // Standard request without structured output
                    requestPayload = new
                    {
                        contents = baseRequest.contents,
                        systemInstruction = baseRequest.systemInstruction,
                        generationConfig = new
                        {
                            temperature = _temperature,
                            maxOutputTokens = _maxTokens
                        }
                    };
                }

                var json = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions 
                { 
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Ensure proper camelCase for Google API
                });
                
                var requestContent = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                // For better logging readability
                //var prettyJson = PreprocessJsonForLogging(json);
                //_loggingService.LogFormattedInfo("request", prettyJson);

                var response = await _httpClient.PostAsync(apiUrl, requestContent);
                
                // Handle errors with more detailed information
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _loggingService.LogError($"Google API error: Status {response.StatusCode}, Response: {errorContent}");
                    throw new Exception($"Google API error: {response.StatusCode}. Details: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var prettyResponse = PreprocessJsonForLogging(responseContent);
                _loggingService.LogFormattedInfo("response", prettyResponse);
                
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Extract the content from Google's response format
                if (responseObject.TryGetProperty("candidates", out var candidates) && 
                    candidates.GetArrayLength() > 0)
                {
                    // Handle structured output (JSON) or standard text response
                    if (candidates[0].TryGetProperty("content", out var content))
                    {
                        if (content.TryGetProperty("parts", out var parts) &&
                            parts.GetArrayLength() > 0) 
                        {
                            // For both structured and unstructured responses
                            if (parts[0].TryGetProperty("text", out var text))
                            {
                                string textResponse = text.GetString();
                                
                                // Basic cleanup of any dummy properties we might have added in Google schemas
                                if (textResponse.Contains("\"any\""))
                                {
                                    textResponse = textResponse.Replace("\"any\": \"string\"", "");
                                    textResponse = textResponse.Replace(",  }", " }");
                                    textResponse = textResponse.Replace("{ }", "{}");
                                }
                                
                                return textResponse;
                            }
                        }
                    }
                }

                throw new Exception("Failed to parse response from Google Gemini API.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in Google provider: {ex.Message}");
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