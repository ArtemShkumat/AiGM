using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class CreateEnemyStatBlockPromptBuilder : BasePromptBuilder
    {
        public CreateEnemyStatBlockPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<Prompt> BuildPromptAsync(PromptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NpcId))
            {
                throw new ArgumentException("NpcId must be provided for CreateEnemyStatBlock prompt.");
            }

            var promptBuilder = new StringBuilder();
            var templateName = "System.txt";
            string gameNameForPrompt = "DefaultGame"; // Default if setting not found

            try
            {
                // 1. Get System Prompt Template
                string templateFolder = "CreateEnemyStatBlock";
                string systemPromptTemplate = await _storageService.GetTemplateAsync(Path.Combine(templateFolder, templateName));
                promptBuilder.AppendLine(systemPromptTemplate);
                promptBuilder.AppendLine("\n--- CONTEXT ---");

                // 2. Load Context Data
                var gameSetting = await _storageService.GetGameSettingAsync(request.UserId);
                if (gameSetting != null && !string.IsNullOrWhiteSpace(gameSetting.GameName))
                {
                    gameNameForPrompt = gameSetting.GameName;
                }
                var world = await _storageService.GetWorldAsync(request.UserId);
                var npc = await _storageService.GetNpcAsync(request.UserId, request.NpcId);

                if (npc == null)
                {
                    _loggingService.LogWarning($"NPC '{request.NpcId}' not found when trying to create its stat block for user '{request.UserId}'. Stat block generation might be inaccurate.");
                    npc = new Npc { Id = request.NpcId, Name = request.NpcName ?? "Unknown NPC" };
                }

                // 3. Append Context Data
                promptBuilder.AppendLine("\nGame Setting:");
                promptBuilder.AppendLine(SerializeForPrompt(gameSetting ?? new GameSetting()));

                promptBuilder.AppendLine("\nWorld State:");
                promptBuilder.AppendLine(SerializeForPrompt(world ?? new World()));

                promptBuilder.AppendLine($"\nNPC to create stat block for (ID: {request.NpcId}):");
                promptBuilder.AppendLine(SerializeForPrompt(npc));

                if (!string.IsNullOrWhiteSpace(request.Context))
                {
                    promptBuilder.AppendLine("\nAdditional Instructions:");
                    promptBuilder.AppendLine(request.Context);
                }

                promptBuilder.AppendLine("\n--- END CONTEXT ---");
                promptBuilder.AppendLine("\nPlease generate the EnemyStatBlock JSON based on the provided context and instructions. Respond ONLY with the valid JSON object.");

                // 4. Create and Return Prompt Object using the constructor
                string systemPrompt = promptBuilder.ToString();
                string userPromptContent = "Generate EnemyStatBlock JSON"; // Fixed content for this type

                // Load the output schema for validation
                string outputSchema = await _storageService.GetTemplateAsync(Path.Combine(templateFolder, "OutputStructure.json"));

                return new Prompt(systemPrompt, userPromptContent, PromptType.CreateEnemyStatBlock, outputSchema);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building CreateEnemyStatBlock prompt for NPC '{request.NpcId}', user '{request.UserId}': {ex.Message} - StackTrace: {ex.StackTrace}");
                throw; // Re-throw exception
            }
        }
    }
} 