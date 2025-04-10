using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Models.Prompts.Sections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class CombatPromptBuilder : BasePromptBuilder
    {
        public CombatPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<Prompt> BuildPromptAsync(PromptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                throw new ArgumentException("UserId must be provided for Combat prompt.");
            }

            if (string.IsNullOrWhiteSpace(request.UserInput))
            {
                throw new ArgumentException("UserInput (player action) must be provided for Combat prompt.");
            }

            var systemPromptBuilder = new StringBuilder();
            var userPromptBuilder = new StringBuilder();
            string gameNameForPrompt = "DefaultGame"; // Default if setting not found

            try
            {
                // 1. Load Combat State
                var combatState = await _storageService.LoadCombatStateAsync(request.UserId);
                if (combatState == null || !combatState.IsActive)
                {
                    throw new InvalidOperationException($"No active combat found for user {request.UserId}");
                }

                // 2. Load Enemy Stat Block
                var enemyStatBlock = await _storageService.LoadEnemyStatBlockAsync(request.UserId, combatState.EnemyStatBlockId);
                if (enemyStatBlock == null)
                {
                    throw new InvalidOperationException($"Enemy stat block not found for {combatState.EnemyStatBlockId}");
                }

                // 3. Load Player Data
                var player = await _storageService.GetPlayerAsync(request.UserId);
                if (player == null)
                {
                    throw new InvalidOperationException($"Player data not found for user {request.UserId}");
                }

                // 4. Load System Prompt Template
                string systemPromptTemplate = await _storageService.GetTemplateAsync("Combat/System.txt");
                string examples = await _storageService.GetTemplateAsync("Combat/ExampleResponses.txt");
                if (string.IsNullOrEmpty(systemPromptTemplate))
                {
                    throw new InvalidOperationException("Combat system prompt template not found");
                }

                // 5. Load Game Setting for context
                var gameSetting = await _storageService.GetGameSettingAsync(request.UserId);
                if (gameSetting != null && !string.IsNullOrWhiteSpace(gameSetting.GameName))
                {
                    gameNameForPrompt = gameSetting.GameName;
                }

                // 6. Build the prompt
                systemPromptBuilder.AppendLine(systemPromptTemplate);
                systemPromptBuilder.AppendLine("\n--- HERE ARE SOME EXAMPLES FOR YOUR REFERENCE ---");
                systemPromptBuilder.AppendLine(examples);
                systemPromptBuilder.AppendLine("\n--- COMBAT CONTEXT ---");

                // Add game setting context
                systemPromptBuilder.AppendLine("\nGame Setting:");
                new GameSettingSection(gameSetting, false).AppendTo(systemPromptBuilder);

                // Add enemy information
                systemPromptBuilder.AppendLine("\nEnemy:");
                systemPromptBuilder.AppendLine(SerializeForPrompt(enemyStatBlock));

                // Add player information (including RPG tags that can be used as combat tags)
                systemPromptBuilder.AppendLine("\nPlayer:");
                systemPromptBuilder.AppendLine(SerializeForPrompt(player));

                // Add current combat state
                systemPromptBuilder.AppendLine("\nCurrent Combat State:");
                systemPromptBuilder.AppendLine(SerializeForPrompt(combatState));

                //// Add player action and tags for this turn
                //List<string> playerTags = new List<string>();
                //if (request.Context != null)
                //{
                //    try
                //    {
                //        playerTags = System.Text.Json.JsonSerializer.Deserialize<List<string>>(request.Context);
                //    }
                //    catch (Exception ex)
                //    {
                //        _loggingService.LogWarning($"Failed to parse player tags from context: {ex.Message}. Assuming no tags provided.");
                //    }
                //}

                systemPromptBuilder.AppendLine("\n--- END COMBAT CONTEXT ---");

                userPromptBuilder.AppendLine("\nCurrent Turn Input:");
                userPromptBuilder.AppendLine($"Player Action: {request.UserInput}");
                //promptBuilder.AppendLine($"Applied Tags: {string.Join(", ", playerTags)}");

                // Add previous log entries for context
                //if (combatState.CombatLog.Count > 0)
                //{
                //    promptBuilder.AppendLine("\nPrevious Combat Log Entries:");
                //    int startIndex = Math.Max(0, combatState.CombatLog.Count - 5); // Only show last 5 entries
                //    for (int i = startIndex; i < combatState.CombatLog.Count; i++)
                //    {
                //        promptBuilder.AppendLine($"[Turn {i + 1}] {combatState.CombatLog[i]}");
                //    }
                //}

                
                //promptBuilder.AppendLine("\nProcess the current combat turn and provide a narrative response that includes the outcome of the player's action, the enemy's response, and any state changes (successes, conditions).");

                // 7. Create and Return Prompt Object
                string systemPrompt = systemPromptBuilder.ToString();
                string userPrompt = userPromptBuilder.ToString();
                //string userPromptContent = $"Player action: {request.UserInput}\nTags: {string.Join(", ", playerTags)}";

                // Optional: Load output schema if defined
                string outputSchema = await _storageService.GetTemplateAsync("Combat/OutputStructure.json");

                return new Prompt(systemPrompt, userPrompt, PromptType.Combat, outputSchema);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building Combat prompt for user '{request.UserId}': {ex.Message}");
                throw; // Re-throw exception
            }
        }
    }
} 