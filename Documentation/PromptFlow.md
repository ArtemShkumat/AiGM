# Application Interaction Flows

This document outlines the typical sequences for handling different user inputs and LLM interactions within the system. All flows utilize Hangfire for background job processing.

## 1. Standard DM Interaction Flow

This flow handles general user commands directed at the game world environment (e.g., "Look around", "Go north", "Check my inventory").

1.  **Input:** User provides text input (e.g., "I walk towards the noisy tavern.").
2.  **PresenterService:** Receives input, creates `PromptRequest` (`userId`, `userInput`, `PromptType.DM`).
3.  **Hangfire Enqueue:** `PresenterService` enqueues a job: `HangfireJobsService.ProcessUserInputAsync(request)`.
4.  **HangfireJobsService (Worker):**
    *   Calls `PromptService.BuildPromptAsync(request)` -> Selects `DMPromptBuilder`.
    *   `DMPromptBuilder`: Loads relevant data (`world`, `player`, `location`, NPC summaries, etc.) from `StorageService` and uses `PromptTemplates/DM/` files to construct the `Prompt`.
    *   Calls `AiService.GetCompletionAsync(prompt)` -> Uses `AIProviderFactory` to get the configured `IAIProvider`.
    *   `IAIProvider`: Sends prompt to LLM, receives JSON response string.
    *   Calls `ResponseProcessingService.HandleResponseAsync(llmResponse, PromptType.DM, userId)`.
5.  **ResponseProcessingService:**
    *   Deserializes the LLM JSON response into a `DmResponse` (or similar generic response model) using custom converters.
    *   Extracts `userFacingText`, `newEntities` (`List<ICreationHook>`), and `partialUpdates` (`Dictionary<string, IUpdatePayload>`).
    *   **If `newEntities` exist:** Calls appropriate `IEntityProcessor` implementations (e.g., `QuestProcessor`, `NPCProcessor`) via a loop or factory. Processors validate and save new entities using `StorageService`. *(See Flow 3)*.
    *   **If `partialUpdates` exist:** Calls `UpdateProcessor.ProcessUpdatesAsync(partialUpdates, userId)`.
    *   `UpdateProcessor`: Iterates the dictionary, calling `StorageService` methods (e.g., `ApplyPartialUpdate` or specific `Save<T>`) to persist changes to existing entities.
    *   Logs the interaction via `StorageService`.
    *   Returns `ProcessedResult` containing `userFacingText`.
6.  **Output:** The `userFacingText` is returned to the user.

## 2. Standard NPC Interaction Flow

This flow handles direct conversations initiated by the user with a specific Non-Player Character (e.g., "Talk to Bob the Bartender", followed by "What jobs do you have?").

1.  **Input:** User provides text input targeting an NPC (e.g., "Ask about the strange noises."). An `npcId` is associated with the request.
2.  **PresenterService:** Receives input, creates `PromptRequest` (`userId`, `userInput`, `PromptType.NPC`, `npcId`).
3.  **Hangfire Enqueue:** `PresenterService` enqueues a job: `HangfireJobsService.ProcessUserInputAsync(request)`.
4.  **HangfireJobsService (Worker):**
    *   Calls `PromptService.BuildPromptAsync(request)` -> Selects `NPCPromptBuilder`.
    *   `NPCPromptBuilder`: Loads specific NPC data (`npc_<id>.json`), relevant `location`, `player`, conversation history from `StorageService`, and uses `PromptTemplates/NPC/` files to construct the `Prompt`.
    *   Calls `AiService.GetCompletionAsync(prompt)`.
    *   `IAIProvider`: Sends prompt to LLM, receives JSON response string.
    *   Calls `ResponseProcessingService.HandleResponseAsync(llmResponse, PromptType.NPC, userId, npcId)`.
5.  **ResponseProcessingService:**
    *   Deserializes the LLM JSON response (likely into an `NpcResponse` or generic model).
    *   Extracts `userFacingText`, `newEntities`, and `partialUpdates`.
    *   **If `newEntities` exist:** Calls appropriate `IEntityProcessor` implementations. *(See Flow 3)*.
    *   **If `partialUpdates` exist:** Calls `UpdateProcessor.ProcessUpdatesAsync(partialUpdates, userId)`. Updates often target the specific NPC's state (`npc_<id>.json`).
    *   Logs the interaction via `StorageService` (often to a specific NPC log).
    *   Returns `ProcessedResult` containing `userFacingText`.
6.  **Output:** The `userFacingText` (the NPC's dialogue) is returned to the user.

## 3. Implicit Entity Creation Flow (Triggered within DM/NPC Response)

This flow describes how new game entities (Quests, NPCs, Locations, Items) are created based on instructions embedded within a standard DM or NPC response JSON.

1.  **Context:** Occurs within Step 5 of Flow 1 (DM) or Flow 2 (NPC), after `ResponseProcessingService` deserializes the LLM response.
2.  **ResponseProcessingService:**
    *   Detects non-empty `newEntities` (`List<ICreationHook>`) in the deserialized response.
    *   Iterates through the `ICreationHook` items.
    *   For each hook, determines the entity type (e.g., `QuestCreationHook`, `NpcCreationHook`).
    *   Selects the corresponding `IEntityProcessor` implementation (e.g., `QuestProcessor`, `NPCProcessor`).
    *   Calls `Processor.ProcessAsync(List<ICreationHook> relevantHooks, userId)`. *(Note: Might process hooks individually or in batches per type)*.
3.  **IEntityProcessor Implementation (e.g., `QuestProcessor`):**
    *   Receives the `ICreationHook` data.
    *   Validates the data structure and content required for the new entity.
    *   May perform additional logic (e.g., generate unique IDs, check for conflicts).
    *   Interacts with `StorageService` to save the new entity data to the appropriate JSON file (e.g., `quests/quest_005.json`).
    *   **Crucially:** If the created entity itself requires *further nested creation* (e.g., a Quest hook includes details for brand new NPCs or Locations needed for that quest), the `IEntityProcessor` might:
        *   Attempt direct creation via `StorageService` if data is complete.
        *   Enqueue *new, separate Hangfire jobs* using `HangfireJobsService.CreateEntityAsync(...)` with appropriate `PromptType` (e.g., `CreateNPC`, `CreateLocation`) to handle the nested creation. *(This triggers Flow 4)*.
4.  **Completion:** The creation process completes. Since this was part of a standard DM/NPC response, the user typically only sees the original `userFacingText` from that response. The creation happens silently in the background. Subsequent prompts will now be able to reference the newly created entity data.

## 4. Explicit Entity Creation Flow

This flow handles cases where the system deliberately triggers a prompt specifically designed to generate the full JSON data for a new entity. This might be initiated by an `IEntityProcessor` (as described in Flow 3) or potentially by a direct user command in the future (e.g., "GM, create a simple goblin NPC").

1.  **Trigger:** An internal process (like an `IEntityProcessor` needing more detail) or a potential future user command initiates creation.
2.  **PresenterService / Internal Service:** Creates `PromptRequest` (`userId`, *potentially input parameters*, `PromptType` like `CreateQuest`, `CreateNPC`, `CreateLocation`).
3.  **Hangfire Enqueue:** Enqueues a specific creation job: `HangfireJobsService.CreateEntityAsync(userId, entityId?, entityType, requestDetails)` (or similar dedicated method).
4.  **HangfireJobsService (Worker):**
    *   Calls `PromptService.BuildPromptAsync(request)` -> Selects the appropriate creation builder (e.g., `CreateQuestPromptBuilder`, `CreateNPCPromptBuilder`).
    *   **Creation Prompt Builder:** Gathers necessary context (world state, related entities, specific creation parameters) from `StorageService` and uses dedicated `PromptTemplates/Create.../` files. The prompt explicitly asks the LLM to return *only* the JSON structure for the new entity.
    *   Calls `AiService.GetCompletionAsync(prompt)`.
    *   `IAIProvider`: Sends prompt, receives JSON response string (expected to be the entity JSON).
    *   Calls `ResponseProcessingService.HandleCreateResponseAsync(llmResponse, promptType, userId)`.
5.  **ResponseProcessingService (HandleCreateResponseAsync):**
    *   Deserializes the LLM JSON response directly into a list of `ICreationHook` objects (or potentially a single, complex entity model). Focuses purely on the creation data, minimal/no `userFacingText` or `partialUpdates` expected.
    *   Validates the received structure.
    *   Selects the relevant `IEntityProcessor` based on `promptType`.
    *   Calls `Processor.ProcessAsync(creationHooks, userId)`.
6.  **IEntityProcessor Implementation:**
    *   Receives the `ICreationHook` data representing the fully generated entity.
    *   Validates, generates IDs if necessary.
    *   Saves the new entity using `StorageService`.
    *   May still trigger nested creation prompts (Flow 4 again) if the generated JSON includes placeholders or instructions for further entities.
7.  **Output:** Typically, no direct `userFacingText` is returned to the *end-user* from this flow. Confirmation might be logged, or a subsequent standard DM prompt (Flow 1) might be triggered to inform the user: "The quest details are now available." or "A new character, [Name], has appeared."
