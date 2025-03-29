using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiGMBackEnd.Services
{
    public class LoggingService
    {
        private readonly string _logPath;
        private readonly object _lockObj = new object();

        public LoggingService()
        {
            string rootDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
            var logsDirectory = Path.Combine(rootDirectory, "Logs");
            
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }
            
            _logPath = Path.Combine(logsDirectory, $"app_{DateTime.Now:yyyyMMdd}.log");
        }

        public void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public void LogWarning(string message)
        {
            Log("WARNING", message);
        }

        public void LogError(string message)
        {
            Log("ERROR", message);
        }

        public void LogFormattedInfo(string prefix, string jsonContent)
        {
            string formattedJson = FormatJsonString(jsonContent);
            Log("INFO", $"{prefix}:\n{formattedJson}");
        }

        private string FormatJsonString(string jsonContent)
        {
            try
            {
                // First, parse the JSON string
                JToken parsedJson = JToken.Parse(jsonContent);
                
                // Clean up the JSON to handle special characters and escape sequences
                CleanupJson(parsedJson);
                
                // Return nicely formatted JSON
                return parsedJson.ToString(Formatting.Indented);
            }
            catch (Exception ex)
            {
                // Log the exception but don't show it in the output
                Console.WriteLine($"Error formatting JSON: {ex.Message}");
                return jsonContent;
            }
        }
        
        private void CleanupJson(JToken token)
        {
            if (token is JObject obj)
            {
                foreach (var property in obj.Properties().ToList())
                {
                    if (property.Value is JValue jValue && jValue.Type == JTokenType.String)
                    {
                        string stringValue = jValue.Value<string>();
                        
                        // Check if this looks like escaped JSON (starts with { or [ after unescaping)
                        if (stringValue != null)
                        {
                            string unescapedValue = UnescapeJsonString(stringValue);
                            
                            if ((unescapedValue.TrimStart().StartsWith("{") && unescapedValue.TrimEnd().EndsWith("}")) || 
                                (unescapedValue.TrimStart().StartsWith("[") && unescapedValue.TrimEnd().EndsWith("]")))
                            {
                                try
                                {
                                    // Try to parse as JSON
                                    JToken nestedJson = JToken.Parse(unescapedValue);
                                    property.Value = nestedJson;
                                    
                                    // Continue cleaning up nested JSON
                                    CleanupJson(nestedJson);
                                }
                                catch
                                {
                                    // Not valid JSON, just clean up the string value
                                    property.Value = unescapedValue;
                                }
                            }
                            else
                            {
                                // Just a regular string, clean it up
                                property.Value = unescapedValue;
                            }
                        }
                    }
                    else if (property.Value is JObject || property.Value is JArray)
                    {
                        // Recursively clean up nested objects and arrays
                        CleanupJson(property.Value);
                    }
                }
            }
            else if (token is JArray array)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i] is JValue jValue && jValue.Type == JTokenType.String)
                    {
                        string stringValue = jValue.Value<string>();
                        
                        if (stringValue != null)
                        {
                            string unescapedValue = UnescapeJsonString(stringValue);
                            
                            if ((unescapedValue.TrimStart().StartsWith("{") && unescapedValue.TrimEnd().EndsWith("}")) || 
                                (unescapedValue.TrimStart().StartsWith("[") && unescapedValue.TrimEnd().EndsWith("]")))
                            {
                                try
                                {
                                    JToken nestedJson = JToken.Parse(unescapedValue);
                                    array[i] = nestedJson;
                                    CleanupJson(nestedJson);
                                }
                                catch
                                {
                                    array[i] = unescapedValue;
                                }
                            }
                            else
                            {
                                array[i] = unescapedValue;
                            }
                        }
                    }
                    else if (array[i] is JObject || array[i] is JArray)
                    {
                        CleanupJson(array[i]);
                    }
                }
            }
        }
        
        private string UnescapeJsonString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
                
            // Replace common JSON escape sequences with their actual characters
            string result = input
                .Replace("\\r\\n", "\n")
                .Replace("\\n", "\n")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\'", "'")
                .Replace("\\\\", "\\");
                
            // Handle Unicode escape sequences like \u0022
            result = Regex.Replace(result, "\\\\u([0-9a-fA-F]{4})", match => 
            {
                string hexValue = match.Groups[1].Value;
                int intValue = Convert.ToInt32(hexValue, 16);
                return ((char)intValue).ToString();
            });
            
            return result;
        }

        private void Log(string level, string message)
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            
            lock (_lockObj)
            {
                try
                {
                    File.AppendAllText(_logPath, logMessage + Environment.NewLine);
                    
                    // Also write to console in development
                    Console.WriteLine(logMessage);
                }
                catch (Exception ex)
                {
                    // Fallback to console if file logging fails
                    Console.WriteLine($"Logging error: {ex.Message}");
                    Console.WriteLine(logMessage);
                }
            }
        }
    }
}
