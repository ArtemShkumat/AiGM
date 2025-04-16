using System;
using System.Threading;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AiGMBackEnd.Services
{
    /// <summary>
    /// Implementation of ILlmResponseDeserializer that provides resilient deserialization 
    /// with timeout capabilities for LLM JSON responses.
    /// </summary>
    public class LlmResponseDeserializer : ILlmResponseDeserializer
    {
        private readonly LoggingService _loggingService;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public LlmResponseDeserializer(LoggingService loggingService)
        {
            _loggingService = loggingService;
            
            // Configure JSON serialization options
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                AllowTrailingCommas = true
            };
            
            // Register custom converters
            _jsonSerializerOptions.Converters.Add(new CreationHookConverter());
            _jsonSerializerOptions.Converters.Add(new UpdatePayloadConverter());
            _jsonSerializerOptions.Converters.Add(new UpdatePayloadDictionaryConverter());
            _jsonSerializerOptions.Converters.Add(new CreationHookListConverter());
            _jsonSerializerOptions.Converters.Add(new LlmSafeIntConverter());
            _jsonSerializerOptions.Converters.Add(new NpcListConverter());
            _jsonSerializerOptions.Converters.Add(new PartialUpdatesConverter());
            _jsonSerializerOptions.Converters.Add(new SanitizedStringConverter());
        }

        /// <summary>
        /// Preprocesses JSON string to fix common issues before deserialization
        /// </summary>
        private string PreprocessJson(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return jsonString;
            }

            try
            {
                // Log the original JSON for debugging
                _loggingService.LogInfo($"Original JSON before preprocessing: Length={jsonString.Length}");
                
                // Fix 1: Handle problematic double backslash sequences that aren't valid JSON escapes
                // Look for patterns like \\ not followed by valid escape chars
                string processed = Regex.Replace(
                    jsonString,
                    @"\\\\(?![""\\\/bfnrt]|u[0-9a-fA-F]{4})",  // Match \\ not followed by valid escape chars
                    @"\");                                      // Replace with single \

                // Fix 2: Handle single backslashes that should be escaped
                processed = Regex.Replace(
                    processed,
                    @"(?<!\\)\\(?![""\\\/bfnrt]|u[0-9a-fA-F]{4})",  // Match single \ not preceded by \ and not followed by valid escape chars
                    @"\\");                                         // Replace with double \\
                    
                // Fix 3: Find all string values with "userFacingText" and clean them more aggressively
                processed = Regex.Replace(
                    processed,
                    @"(""userFacingText""[\s:]+"")(.*?)((?<!\\)"")",
                    match => {
                        // Get the actual text content
                        string content = match.Groups[2].Value;
                        // Remove any potentially problematic escape sequences
                        content = content.Replace("\\\\", "\\");
                        // Re-escape any quotes that need escaping
                        content = content.Replace("\"", "\\\"");
                        // Return the sanitized version
                        return match.Groups[1].Value + content + match.Groups[3].Value;
                    },
                    RegexOptions.Singleline);
                
                // Log changes if they occurred
                if (processed != jsonString)
                {
                    _loggingService.LogInfo($"JSON was preprocessed. New length={processed.Length}");
                }
                
                return processed;
            }
            catch (Exception ex)
            {
                // If any error occurs during preprocessing, log it and return the original
                _loggingService.LogWarning($"Error during JSON preprocessing: {ex.Message}. Using original JSON.");
                return jsonString;
            }
        }

        /// <inheritdoc />
        public async Task<(bool success, T result, Exception error)> TryDeserializeAsync<T>(string jsonString, TimeSpan timeout)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                _loggingService.LogWarning($"Received empty or null JSON string for deserialization into {typeof(T).Name}");
                return (false, default, new ArgumentException("JSON string cannot be null or whitespace"));
            }

            jsonString = jsonString.Trim();
            
            // Preprocess the JSON to fix common issues
            jsonString = PreprocessJson(jsonString);
            
            _loggingService.LogInfo($"Attempting to deserialize {typeof(T).Name}. Response length: {jsonString.Length}");

            T result = default;
            bool success = false;
            Exception error = null;
            var cts = new CancellationTokenSource(timeout);

            try
            {
                // Run the deserialization in a separate task with a timeout
                await Task.Run(() =>
                {
                    try
                    {
                        // Ensure the task respects cancellation if possible
                        cts.Token.ThrowIfCancellationRequested();
                        
                        // Perform the actual deserialization
                        result = JsonSerializer.Deserialize<T>(jsonString, _jsonSerializerOptions);
                        success = result != null;
                        
                        if (success)
                        {
                            _loggingService.LogInfo($"Successfully deserialized {typeof(T).Name}");
                        }
                        else
                        {
                            _loggingService.LogError($"Deserialization returned null for {typeof(T).Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                        _loggingService.LogError($"Error during deserialization of {typeof(T).Name}: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            _loggingService.LogError($"Inner exception: {ex.InnerException.Message}");
                        }
                    }
                }, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _loggingService.LogError($"Deserialization of {typeof(T).Name} timed out after {timeout.TotalSeconds} seconds. Response length: {jsonString.Length}");
                error = new TimeoutException($"Deserialization of {typeof(T).Name} timed out after {timeout.TotalSeconds} seconds");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Unexpected error during deserialization task for {typeof(T).Name}: {ex.Message}");
                error = ex;
            }
            finally
            {
                // Clean up the cancellation token source
                cts.Dispose();
            }

            // Return the deserialization result tuple
            return (success, result, error);
        }
    }
} 