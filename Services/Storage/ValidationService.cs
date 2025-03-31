using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using AiGMBackEnd.Services;

namespace AiGMBackEnd.Services.Storage
{
    public class ValidationService
    {
        private readonly LoggingService _loggingService;
        private readonly string _dataPath;

        public ValidationService(LoggingService loggingService)
        {
            _loggingService = loggingService;
            
            // Set the data path
            string rootDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
            _dataPath = Path.Combine(rootDirectory, "Data");
        }

        public async Task<List<StorageService.DanglingReferenceInfo>> FindDanglingReferencesAsync(string userId)
        {
            var userPath = Path.Combine(_dataPath, "userData", userId);
            var danglingReferencesResult = new List<StorageService.DanglingReferenceInfo>();
            // Dictionary to map ReferenceId -> Set of FilePaths where it was found
            var foundReferencesMap = new Dictionary<string, HashSet<string>>();

            // Define entity prefixes
            string[] entityPrefixes = { "npc_", "loc_", "quest_" };

            try
            {
                // 1. Get existing entity IDs
                var existingNpcIds = GetExistingEntityIds(Path.Combine(userPath, "npcs"));
                var existingLocationIds = GetExistingEntityIds(Path.Combine(userPath, "locations"));
                var existingQuestIds = GetExistingEntityIds(Path.Combine(userPath, "quests"));

                // 2. List files to scan
                var filesToScan = new List<string>();
                var worldFilePath = Path.Combine(userPath, "world.json");
                var playerFilePath = Path.Combine(userPath, "player.json");

                if (File.Exists(worldFilePath)) filesToScan.Add(worldFilePath);
                if (File.Exists(playerFilePath)) filesToScan.Add(playerFilePath);
                if (Directory.Exists(Path.Combine(userPath, "npcs"))) filesToScan.AddRange(Directory.GetFiles(Path.Combine(userPath, "npcs"), "*.json"));
                if (Directory.Exists(Path.Combine(userPath, "locations"))) filesToScan.AddRange(Directory.GetFiles(Path.Combine(userPath, "locations"), "*.json"));
                if (Directory.Exists(Path.Combine(userPath, "quests"))) filesToScan.AddRange(Directory.GetFiles(Path.Combine(userPath, "quests"), "*.json"));

                // 3. Scan files for references
                foreach (var filePath in filesToScan)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(filePath);
                        var token = JToken.Parse(json);
                        // Pass filePath to the recursive function
                        FindReferencesRecursive(token, foundReferencesMap, entityPrefixes, filePath);
                    }
                    catch (Newtonsoft.Json.JsonException jsonEx)
                    {
                        _loggingService.LogWarning($"Skipping file due to JSON parse error: {filePath}. Error: {jsonEx.Message}");
                    }
                    catch (Exception fileEx)
                    {
                        _loggingService.LogError($"Error reading or processing file {filePath}: {fileEx.Message}");
                    }
                }

                // 4. Compare found references to existing IDs
                foreach (var kvp in foundReferencesMap)
                {
                    var referenceId = kvp.Key;
                    var filePaths = kvp.Value;

                    bool isDangling = false;
                    if (referenceId.StartsWith("npc_") && !existingNpcIds.Contains(referenceId))
                    {
                        isDangling = true;
                    }
                    else if (referenceId.StartsWith("loc_") && !existingLocationIds.Contains(referenceId))
                    {
                        isDangling = true;
                    }
                    else if (referenceId.StartsWith("quest_") && !existingQuestIds.Contains(referenceId))
                    {
                        isDangling = true;
                    }

                    if (isDangling)
                    {
                        foreach (var filePath in filePaths)
                        {
                            danglingReferencesResult.Add(new StorageService.DanglingReferenceInfo
                            {
                                ReferenceId = referenceId,
                                FilePath = filePath // Store the file path
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error finding dangling references for user {userId}: {ex.Message}");
                // Optionally rethrow or return an indication of error
            }

            return danglingReferencesResult; // Return the detailed list
        }

        private HashSet<string> GetExistingEntityIds(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return new HashSet<string>();
            }
            return Directory.GetFiles(directoryPath, "*.json")
                            .Select(Path.GetFileNameWithoutExtension)
                            .ToHashSet();
        }

        // Updated to track file paths
        private void FindReferencesRecursive(JToken token, Dictionary<string, HashSet<string>> referencesMap, string[] prefixes, string filePath)
        {
            if (token == null) return;

            if (token is JValue value && value.Type == JTokenType.String)
            {
                var stringValue = value.ToString();
                foreach (var prefix in prefixes)
                {
                    if (stringValue.StartsWith(prefix))
                    {
                        // If referenceId is not in the map, add it with a new HashSet
                        if (!referencesMap.ContainsKey(stringValue))
                        {
                            referencesMap[stringValue] = new HashSet<string>();
                        }
                        // Add the current file path to the set for this referenceId
                        referencesMap[stringValue].Add(filePath);
                        break; // Found a match, no need to check other prefixes for this string
                    }
                }
            }
            else if (token is JObject obj)
            {
                foreach (var property in obj.Properties())
                {
                    // Pass filePath down recursively
                    FindReferencesRecursive(property.Value, referencesMap, prefixes, filePath);
                }
            }
            else if (token is JArray array)
            {
                foreach (var item in array)
                {
                    // Pass filePath down recursively
                    FindReferencesRecursive(item, referencesMap, prefixes, filePath);
                }
            }
        }
    }
} 