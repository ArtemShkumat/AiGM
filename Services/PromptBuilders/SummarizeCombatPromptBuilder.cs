using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class SummarizeCombatPromptBuilder : BasePromptBuilder
    {
        public SummarizeCombatPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<Prompt> BuildPromptAsync(PromptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                throw new ArgumentException("UserId must be provided for SummarizeCombat prompt.");
            }

            var promptBuilder = new StringBuilder();
            bool isPlayerVictory = false;

            try
            {
                // Load the combat state (it will be marked as inactive at this point)
                var combatState = await _storageService.LoadCombatStateAsync(request.UserId);
                if (combatState == null)
                {
                    throw new InvalidOperationException($"No combat state found for user {request.UserId} to summarize.");
                }

                // Parse player victory from the context string
                if (!string.IsNullOrEmpty(request.Context))
                {
                    bool.TryParse(request.Context, out isPlayerVictory);
                }

                // Load the enemy stat block for context
                var enemyStatBlock = await _storageService.LoadEnemyStatBlockAsync(request.UserId, combatState.EnemyStatBlockId);
                if (enemyStatBlock == null)
                {
                    throw new InvalidOperationException($"Enemy stat block not found for {combatState.EnemyStatBlockId}");
                }

                // Load the system prompt template
                string systemPromptTemplate = await _storageService.GetTemplateAsync("SummarizeCombat/System.txt");
                if (string.IsNullOrEmpty(systemPromptTemplate))
                {
                    throw new InvalidOperationException("SummarizeCombat system prompt template not found");
                }

                // Build the prompt
                promptBuilder.AppendLine(systemPromptTemplate);
                
                // Add combat information
                promptBuilder.AppendLine("\n## Combat Context");
                promptBuilder.AppendLine($"Enemy: {enemyStatBlock.Name}");
                promptBuilder.AppendLine($"Description: {enemyStatBlock.Description}");
                promptBuilder.AppendLine($"Level: {enemyStatBlock.Level}");
                promptBuilder.AppendLine($"Vulnerability: {enemyStatBlock.Vulnerability}");
                
                // Add outcome
                promptBuilder.AppendLine("\n## Combat Outcome");
                promptBuilder.AppendLine(isPlayerVictory 
                    ? "Player Victory: The player successfully defeated the enemy." 
                    : "Player Defeat: The player was defeated by the enemy.");
                
                if (!isPlayerVictory)
                {
                    promptBuilder.AppendLine($"Bad Stuff: {enemyStatBlock.BadStuff}");
                }
                
                // Add player conditions at end of combat
                promptBuilder.AppendLine("\n## Player Conditions at End of Combat");
                if (combatState.PlayerConditions.Count > 0)
                {
                    foreach (var condition in combatState.PlayerConditions)
                    {
                        promptBuilder.AppendLine($"- {condition}");
                    }
                }
                else
                {
                    promptBuilder.AppendLine("- No conditions");
                }
                
                // Add the combat log
                promptBuilder.AppendLine("\n## Combat Log");
                int turnNumber = 1;
                foreach (var entry in combatState.CombatLog)
                {
                    promptBuilder.AppendLine($"Turn {turnNumber}: {entry}");
                    turnNumber++;
                }
                
                // Create the prompt
                string userPromptContent = "Please summarize the combat that just occurred.";
                
                return new Prompt(promptBuilder.ToString(), userPromptContent, PromptType.SummarizeCombat);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building SummarizeCombat prompt for user '{request.UserId}': {ex.Message}");
                throw;
            }
        }
    }
} 