Below is a technical architecture document that lays out the core components, data flow, and class/service responsibilities for your text-based RPG system. This document is intended for a developer about to start coding, giving a clear roadmap of what to build, where to place it, and how everything fits together.

Technical Architecture Document
1. High-Level System Overview
The system is a C# backend that processes user inputs (e.g., from a console or a web/UI layer) and uses a Large Language Model (LLM) to generate narrative responses and content for a text-based RPG. We keep persistent JSON files to store game state so that important data doesn't rely solely on the LLM's context window.

Key Goals:

Prompt creation that injects relevant data into LLM requests.
LLM calls that return **a structured JSON response** containing narrative and/or content updates.
Response parsing that **deserializes the JSON** and applies any updates or data changes to local storage.
A job queue (Hangfire, mandatory for all calls) ensuring concurrency limits.
*   **Modular Design:** Utilize interfaces and specific implementations for AI providers, prompt building, and response processing to enhance flexibility and testability.
*   **Strong Typing:** Employ strongly-typed models and custom JSON converters for robust data handling and serialization/deserialization of LLM responses.

2. Primary Components
Below is a breakdown of the services and models:

2.1. PresenterService
Role: Entry point for user requests.
If a user says "I look around," PresenterService is called with that input, `userId`, and `promptType`.
It creates a `PromptRequest` object and passes it to the `HangfireJobsService` for processing via Hangfire. All requests go through the background job processing.
Methods:
HandleUserInputAsync(userId, userInput, promptType, npcId?): Returns a string (the final user-facing text) upon completion of the background job.

2.2. HangfireJobsService (Mandatory for All Prompts)
Role: Handles background jobs enqueued through Hangfire, processing `PromptRequest` jobs.
For each job, it calls:
1.  `PromptService.BuildPromptAsync` to get the fully constructed `Prompt`.
2.  `AiService.GetCompletionAsync` to get the LLM response string (expected to be a single JSON object).
3.  `ResponseProcessingService.HandleResponseAsync` (or `HandleCreateResponseAsync` for creation prompts) to process the JSON response and apply updates.
Returns a Task<string> representing the final user-facing text upon job completion.
Methods:
ProcessUserInputAsync(PromptRequest request): Processes a user input job and returns the result.
CreateEntityAsync(userId, entityId, entityType, request): Creates new entities in the background.

2.3. PromptService
Role: Orchestrates prompt construction. Selects and delegates the actual building process to specialized `IPromptBuilder` implementations based on the `PromptType` in the `PromptRequest`.
Uses `Services/PromptBuilders/` implementations.
Methods:
BuildPromptAsync(PromptRequest request) → Task<Prompt>: Finds the appropriate `IPromptBuilder` and calls its `BuildPromptAsync` method.

2.4. AiService
Role: Orchestrates the actual LLM call. Selects the appropriate LLM provider based on configuration (`appsettings.json`) using the `AIProviderFactory` and delegates the call.
Uses `Services/AIProviders/` implementations.
Method:
GetCompletionAsync(Prompt prompt) → Task<string>: Uses `AIProviderFactory` to get an `IAIProvider` instance and calls its `GetCompletionAsync` method.

2.5. ResponseProcessingService
Role: Orchestrates the interpretation and processing of the LLM's structured JSON response.
Uses `System.Text.Json` with custom converters (`CreationHookConverter`, `UpdatePayloadConverter`, `UpdatePayloadDictionaryConverter`, `LlmSafeIntConverter`) to deserialize the entire LLM response string into strongly-typed models (e.g., `DmResponse`).
Extracts `userFacingText`, `newEntities` (`List<ICreationHook>`), and `partialUpdates` (`Dictionary<string, IUpdatePayload>`) from the deserialized object.
Delegates the handling of `newEntities` to specialized `IEntityProcessor` implementations and `partialUpdates` to the `UpdateProcessor` based on the `PromptType`.
Uses `Services/Processors/` implementations.
Methods:
HandleResponseAsync(llmResponse, promptType, userId, npcId?): Handles standard DM/NPC responses. Deserializes the `llmResponse` JSON string, extracts data, and calls appropriate processors for `newEntities` and `partialUpdates`.
HandleCreateResponseAsync(llmResponse, promptType, userId): Handles responses that are primarily JSON for entity creation. Deserializes the `llmResponse`, validates, and calls the relevant `IEntityProcessor` for the `newEntities`.

2.6. StorageService
Role: Load / Save game data in JSON.
One subfolder per user or campaign.
Possibly merges partial updates.

Storage services include:

**BaseStorageService**:
- `LoadAsync<T>(userId, fileId)` → Task<T>: Load object from JSON file.
- `SaveAsync<T>(userId, fileId, entity)` → Task: Save entity to JSON file.
- `ApplyPartialUpdateAsync(userId, fileId, jsonPatch)` → Task: Apply partial updates to existing JSON.
- `GetFilePath(userId, fileId)` → string: Get the full path to a file.
- `CopyDirectory(sourceDir, destinationDir)` → void: Copy directory and its contents.

**EntityStorageService**:
- `GetPlayerAsync(userId)` → Task<Player>: Get player entity.
- `GetWorldAsync(userId)` → Task<World>: Get world entity.
- `GetGameSettingAsync(userId)` → Task<GameSetting>: Get game settings.
- `GetGamePreferencesAsync(userId)` → Task<GamePreferences>: Get game preferences.
- `GetLocationAsync(userId, locationId)` → Task<Location>: Get location entity.
- `GetNpcAsync(userId, npcId)` → Task<Npc>: Get NPC entity.
- `GetQuestAsync(userId, questId)` → Task<Quest>: Get quest entity.
- `GetNpcsInLocationAsync(userId, locationId)` → Task<List<Npc>>: Get all NPCs in a location.
- `GetAllNpcsAsync(gameId)` → Task<List<Npc>>: Get all NPCs in a game.
- `GetAllVisibleNpcsAsync(gameId)` → Task<List<Npc>>: Get all visible NPCs.
- `GetVisibleNpcsInLocationAsync(gameId, locationId)` → Task<List<NpcInfo>>: Get visible NPCs in location.
- `GetAllQuestsAsync(gameId)` → Task<List<Quest>>: Get all quests.
- `GetActiveQuestsAsync(userId, activeQuestIds)` → Task<List<Quest>>: Get active quests.
- `AddEntityToWorldAsync(userId, entityId, entityName, entityType)` → Task: Add entity to world.

**ConversationLogService**:
- `GetConversationLogAsync(userId)` → Task<ConversationLog>: Get conversation log.
- `AddUserMessageAsync(userId, content)` → Task: Add player message to log.
- `AddDmMessageAsync(userId, content)` → Task: Add DM message to log.
- `AddUserMessageToNpcLogAsync(userId, npcId, content)` → Task: Add player message to NPC log.
- `AddDmMessageToNpcLogAsync(userId, npcId, content)` → Task: Add DM message to NPC log.
- `WipeLogAsync(userId)` → Task: Wipe log keeping only last message.

**EnemyStatBlockService**:
- `LoadEnemyStatBlockAsync(userId, enemyId)` → Task<EnemyStatBlock?>: Load enemy stats.
- `SaveEnemyStatBlockAsync(userId, statBlock)` → Task: Save enemy stats.
- `CheckIfStatBlockExistsAsync(userId, enemyId)` → Task<bool>: Check if stats exist.

**CombatStateService**:
- `SaveCombatStateAsync(userId, combatState)` → Task: Save combat state.
- `LoadCombatStateAsync(userId)` → Task<CombatState?>: Load combat state.
- `DeleteCombatStateAsync(userId)` → Task: Delete combat state.

**InventoryStorageService**:
- `AddItemToPlayerInventoryAsync(userId, newItem)` → Task<bool>: Add item to inventory.
- `RemoveItemFromPlayerInventoryAsync(userId, itemName, quantity)` → Task<bool>: Remove item.
- `AddCurrencyAmountAsync(userId, currencyName, amount)` → Task<bool>: Add currency.
- `RemoveCurrencyAmountAsync(userId, currencyName, amount)` → Task<bool>: Remove currency.

**GameScenarioService**:
- `GetScenarioIds()` → List<string>: Get all scenario IDs.
- `LoadScenarioSettingAsync<T>(scenarioId, fileId)` → Task<T>: Load scenario setting.
- `CreateGameFromScenarioAsync(scenarioId, preferences)` → Task<string>: Create new game.
- `GetGameIds()` → List<string>: Get all game IDs.

**RecentEventsService**:
- `GetRecentEventsAsync(userId)` → Task<RecentEvents>: Get recent events.
- `AddSummaryToRecentEventsAsync(userId, summary)` → Task: Add summary to events.

**TemplateService**:
- `GetTemplateAsync(templatePath)` → Task<string>: Get template content.
- `GetDmTemplateAsync(templateName)` → Task<string>: Get DM template.
- `GetNpcTemplateAsync(templateName)` → Task<string>: Get NPC template.
- `GetCreateQuestTemplateAsync(templateName)` → Task<string>: Get quest creation template.
- `GetCreateNpcTemplateAsync(templateName)` → Task<string>: Get NPC creation template.
- `GetCreateLocationTemplateAsync(templateName, locationType)` → Task<string>: Get location template.
- `GetCreatePlayerTemplateAsync(templateName)` → Task<string>: Get player creation template.
- `GetSummarizeTemplateAsync(templateName)` → Task<string>: Get summarize template.

**ValidationService**:
- `FindDanglingReferencesAsync(userId)` → Task<List<DanglingReferenceInfo>>: Find dangling references.

**WorldSyncService**:
- `SyncWorldWithEntitiesAsync(userId)` → Task: Sync world with all entities.
- `SyncNpcLocationsAsync(gameId)` → Task<(int, List<object>)>: Sync NPCs with locations.

2.7. LoggingService
Role: Provide centralized logs for prompt requests, errors, time taken, etc.
Implementation can be very simple or use a robust library.

2.8. GameNotificationService
Role: Sends real-time notifications to connected clients (UI) using SignalR via the `GameHub`. This is used to inform the UI about specific state changes that require a refresh without a full page reload or explicit user action.
Uses `Microsoft.AspNetCore.SignalR`.
Methods:
NotifyInventoryChangedAsync(string gameId): Sends an "InventoryChanged" message to clients connected to the specified `gameId` group, indicating the player's inventory has been updated and the UI should refetch it. (Currently the only notification implemented).
*   **`NotifyCombatStartedAsync(string userId, CombatStartInfo initialState)`**: Signals the UI to enter combat mode, providing initial enemy and player state.
*   **`NotifyCombatEndedAsync(string userId, bool playerVictory)`**: Signals the UI that combat is over and whether the player won.
*   **`NotifyCombatTurnUpdateAsync(string userId, CombatTurnInfo currentState)`**: (Optional) Sends turn-by-turn updates to the UI.

2.9. Models
Where: RPGGame/Models/
What: Classes for Npc, Player, Location, Quest, World, Prompt, PromptRequest, ProcessedResult. Includes interfaces like `ICreationHook` and `IUpdatePayload` and their implementations (e.g., `NpcCreationHook`, `PlayerUpdatePayload`) to represent structured LLM outputs. Also includes custom `JsonConverter` implementations in `Models/Converters.cs`. JSON is stored in RPGGame/Data/<UserId>/..., loaded into these model classes at runtime.

2.10. AI Providers (`Services/AIProviders/`)
Role: Handle communication specifics for different LLM APIs (e.g., OpenAI, OpenRouter).
Components:
`IAIProvider`: Interface defining `GetCompletionAsync(Prompt)`.
Implementations (e.g., `OpenAIProvider`, `OpenRouterProvider`): Concrete classes implementing `IAIProvider`.
`AIProviderFactory`: Creates instances of `IAIProvider` based on configuration.

2.11. Prompt Builders (`Services/PromptBuilders/`)
Role: Construct the specific prompt string and associated metadata for different `PromptType`s.
Components:
`IPromptBuilder`: Interface defining `BuildPromptAsync(PromptRequest)`.
Implementations (e.g., `DMPromptBuilder`, `NPCPromptBuilder`, `CreateQuestPromptBuilder`, etc.): Concrete classes implementing `IPromptBuilder`, each responsible for gathering necessary data (from `StorageService`) and formatting the prompt for a specific scenario.
*   **`CreateEnemyStatBlockPromptBuilder`**: Gathers NPC/context data to request JSON for enemy combat stats.
*   **`CombatPromptBuilder`**: Loads `CombatState`, `EnemyStatBlock`, `Player` data for combat turns.
`BasePromptBuilder`: Optional base class for common functionality.

2.12. Processors (`Services/Processors/`)
Role: Handle the domain logic associated with processing LLM responses related to specific entities or applying general updates.
Components:
`IEntityProcessor`: Interface defining `ProcessAsync(List<ICreationHook> creationHooks, string userId)`. Implemented by specific processors like `NPCProcessor`.
Implementations (e.g., `LocationProcessor`, `QuestProcessor`, `NPCProcessor`, `PlayerProcessor`, **`EnemyStatBlockProcessor`**): Concrete classes implementing `IEntityProcessor`, responsible for validating and saving new entities defined in the `creationHooks`.
`UpdateProcessor`: Handles applying partial updates. Defines `ProcessUpdatesAsync(Dictionary<string, IUpdatePayload> updateData, string userId)`. Responsible for parsing the `updateData` dictionary and calling `StorageService` to apply changes to existing entities.
**`ICombatResponseProcessor`**: Interface defining `ProcessCombatResponseAsync(string llmResponse, string userId)`.
**`CombatResponseProcessor`**: Concrete implementation for handling combat turn JSON responses, updating `CombatState`, and checking for combat end conditions.
**`ISummarizePromptProcessor`**: Interface defining methods for processing summary requests.
**`SummarizePromptProcessor`**: Concrete implementation handling both general summaries and combat summaries (using `ProcessSummaryAsync` and `ProcessCombatSummaryAsync`).

2.13. Controllers (`Controllers/`)
Role: Expose API endpoints for frontend interaction.
Components:
`InteractionController`: Handles general user input (`/api/interaction/input`) and character creation.
**`CombatController`**: Handles combat-specific actions (`/api/combat/start`, `/api/combat/action`, `/api/combat/end`, `/api/combat/{gameId}`).
`EntityStatusController`: Handles checking status of background entity creation jobs.

3. Data & File Structure
Data Folder:
lua
Copy
Edit
/RPGGame/Data/<UserId>/
|-- world.json
|-- player.json
|-- npcs/
|   |-- npc_001.json
|   |-- ...
|-- locations/
|   |-- loc_001.json
|-- enemies/            <-- Added
|   |-- enemy_001.json
|-- quests/
|   |-- quest_001.json
|-- logs/
|   |-- conversation_log.json
|   |-- recent_events.json
|-- active_combat.json  <-- Added
PromptTemplates Folder:
swift
Copy
Edit
/RPGGame/PromptTemplates/DM/
/RPGGame/PromptTemplates/NPC/
/RPGGame/PromptTemplates/CreateQuest/
/RPGGame/PromptTemplates/CreateEnemyStatBlock/ <-- Added
/RPGGame/PromptTemplates/Combat/               <-- Added
/RPGGame/PromptTemplates/SummarizeCombat/      <-- Added
All final data (like NPC changes, quest states, **enemy stats, combat state**) get persisted in these JSON files by StorageService.

4. Data Flow (Sequence)
4.1. Standard "DM" Prompt Example
User inputs: "I look around the market."
PresenterService receives input, creates `PromptRequest` (userId, input, `PromptType.DM`).
PresenterService calls `BackgroundJob.Enqueue<HangfireJobsService>(x => x.ProcessUserInputAsync(request))`.
HangfireJobsService (Worker):
  Processes the `PromptRequest`.
  Calls `PromptService.BuildPromptAsync(request)` -> `DMPromptBuilder` is selected.
  `DMPromptBuilder` loads `world.json`, `player.json`, relevant `location.json`, NPC summaries from `StorageService`, merges with templates -> returns `Prompt` object.
  Calls `AiService.GetCompletionAsync(prompt)` -> `AIProviderFactory` creates default `IAIProvider`.
  `IAIProvider` sends the prompt string to the LLM API -> returns LLM response string (as a single JSON object).
  Calls `ResponseProcessingService.HandleResponseAsync(llmResponse, PromptType.DM, userId)`.
  `ResponseProcessingService`:
    Deserializes `llmResponse` into a `DmResponse` object using custom converters.
    Extracts `userFacingText`, `newEntities`, and `partialUpdates`.
    **Checks for `CombatTriggered == true`**. If true, initiates **Combat Initiation Flow (See 4.3)** and returns `CombatPending = true`.
    If `CombatTriggered` is false:
      Calls relevant `IEntityProcessor` implementations for any `newEntities`.
      Calls `UpdateProcessor.ProcessUpdatesAsync(partialUpdates, userId)`.
      `UpdateProcessor` iterates through the `partialUpdates` dictionary, calling `StorageService` to save changes for each entity ID.
      Adds DM message to log via `StorageService`.
      Returns `ProcessedResult` (containing `userFacingText`).
Hangfire completes the job.
PresenterService polls and returns `userFacingText` or status (e.g., Combat Pending) to the front-end.

4.2. Enemy Stat Block Creation Flow (On-Demand)
(This flow is triggered by the Combat Initiation Flow if needed)
Trigger: `HangfireJobsService.EnsureEnemyStatBlockAndInitiateCombatAsync` detects missing stat block.
Hangfire Enqueues: `HangfireJobsService.CreateEnemyStatBlockAsync(userId, enemyId, enemyName, context)`.
HangfireJobsService (Worker):
  Processes the `CreateEnemyStatBlockAsync` job.
  Constructs `PromptRequest` (Type: `CreateEnemyStatBlock`, providing `npcId`, `npcName`, `context`).
  Calls `CreateEntityAsync` helper.
  `CreateEntityAsync` calls `PromptService.BuildPromptAsync` -> `CreateEnemyStatBlockPromptBuilder` selected.
  `CreateEnemyStatBlockPromptBuilder` loads NPC data, context, uses templates.
  Calls `AiService.GetCompletionAsync` -> gets LLM response (JSON for `EnemyStatBlock`).
  Calls `ResponseProcessingService.HandleCreateResponseAsync`.
  `ResponseProcessingService` deserializes JSON into `EnemyStatBlock` (via hook/processor).
  Calls `EnemyStatBlockProcessor.ProcessAsync` (or similar logic).
  `EnemyStatBlockProcessor` validates and calls `StorageService.SaveEnemyStatBlockAsync`.
Completion: Hangfire marks the creation job complete. The **Combat Initiation Flow** continuation can now proceed.

4.3. Combat Initiation Flow
Trigger: `ResponseProcessingService.HandleResponseAsync` detects `CombatTriggered == true`.
Hangfire Enqueues: `HangfireJobsService.EnsureEnemyStatBlockAndInitiateCombatAsync(userId, enemyId, initialCombatText)`.
HangfireJobsService (Worker - `Ensure...`):
  Calls `StorageService.CheckIfStatBlockExistsAsync`.
  If **False**: Enqueues `CreateEnemyStatBlockAsync` (Flow 4.2) and sets `InitiateCombatAsync` as its continuation.
  If **True**: Enqueues `InitiateCombatAsync` directly.
HangfireJobsService (Worker - `InitiateCombatAsync`):
  Loads `EnemyStatBlock` (should exist now).
  Creates new `CombatState` object.
  Calls `StorageService.SaveCombatStateAsync`.
  Calls `GameNotificationService.NotifyCombatStartedAsync` (SignalR).
Completion: Combat state is saved, frontend notified.

4.4. Combat Turn Flow
Input: User submits action via combat UI to `/api/combat/action`.
CombatController (`SubmitCombatAction`):
  Receives `CombatActionRequest`.
  Creates `PromptRequest` (Type: `Combat`, `userInput`).
  Calls `PresenterService.HandleUserInputAsync`.
PresenterService:
  Enqueues job: `HangfireJobsService.ProcessUserInputAsync(request)`.
HangfireJobsService (Worker - `ProcessUserInputAsync`):
  Calls `PromptService.BuildPromptAsync` -> `CombatPromptBuilder` selected.
  `CombatPromptBuilder` loads `CombatState`, `EnemyStatBlock`, `Player` data.
  Calls `AiService.GetCompletionAsync` -> gets LLM response (JSON for combat turn).
  Calls `ResponseProcessingService.HandleResponseAsync`.
ResponseProcessingService (`HandleResponseAsync` detects `PromptType.Combat`):
  Delegates to `HandleCombatResponseAsync`.
ResponseProcessingService (`HandleCombatResponseAsync`):
  Calls `_combatResponseProcessor.ProcessCombatResponseAsync`.
CombatResponseProcessor (`ProcessCombatResponseAsync`):
  Deserializes combat turn JSON.
  Validates state changes.
  Updates `CombatState.CombatLog`.
  Checks end conditions (successes required, player conditions).
  If **Ending**: Sets `CombatState.IsActive = false`, saves state, calls `NotifyCombatEndedAsync`, enqueues summarization job (Flow 4.5).
  If **Continuing**: Saves updated `CombatState`, calls `NotifyCombatTurnUpdateAsync` (optional).
  Returns `ProcessedResult` with `userFacingText` (if continuing).
Completion: Hangfire job finishes. Frontend receives `userFacingText` via polling the job result.

4.5. Combat Resolution Flow
Trigger: `CombatResponseProcessor` enqueues summarization job after setting `CombatState.IsActive = false`.
Hangfire Enqueues: `HangfireJobsService.ProcessSummarizationJobAsync(request)` (with `PromptType.SummarizeCombat`, `Context` indicating victory).
HangfireJobsService (Worker - `ProcessSummarizationJobAsync`):
  Parses victory status from `request.Context`.
  Calls `PromptService.BuildPromptAsync` -> `SummarizeCombatPromptBuilder`.
  `SummarizeCombatPromptBuilder` loads final `CombatState` (including log).
  Calls `AiService.GetCompletionAsync` -> gets summary text.
  Calls `_responseProcessingService.ProcessCombatSummaryAsync(summary, userId, playerVictory)`.
ResponseProcessingService (`ProcessCombatSummaryAsync`):
  Calls `_summarizePromptProcessor.ProcessCombatSummaryAsync(summary, userId, playerVictory)`.
SummarizePromptProcessor (`ProcessCombatSummaryAsync`):
  Formats summary (e.g., adds prefix).
  Calls `_recentEventsService.AddSummaryToRecentEventsAsync`.
  Calls `_conversationLogService.AddDmMessageAsync`.
HangfireJobsService (Worker - `ProcessSummarizationJobAsync`):
  **After successful processing**: Calls `StorageService.DeleteCombatStateAsync`.
Completion: Summary logged, combat state deleted.

5. Handling Concurrency with Hangfire
Hangfire provides a robust job processing framework...
(Rest of section remains the same)

6. Class Diagram (Textual)
(Update with new components)
 PresenterService
   -> Hangfire (Enqueues background jobs to HangfireJobsService)

 CombatController
   -> PresenterService

 HangfireJobsService (Processes jobs)
   -> PromptService
      -> IPromptBuilder (Implementations: DMPromptBuilder, NPCPromptBuilder, Create..., **CombatPromptBuilder**, **SummarizeCombatPromptBuilder**)
         -> StorageService (Load data)
   -> AiService
      -> AIProviderFactory
         -> IAIProvider
            -> (External LLM API)
   -> ResponseProcessingService
      -> (Uses appropriate processor based on PromptType)
         -> IEntityProcessor (Implementations: ..., **EnemyStatBlockProcessor**)
         -> UpdateProcessor
         -> **ICombatResponseProcessor**
         -> **ISummarizePromptProcessor**
         -> StorageService (Save entities/updates)
   -> GameNotificationService
      -> IHubContext<GameHub> (SignalR)
   -> StorageService (SyncWorld, DeleteCombatState)

 (All services potentially use LoggingService)
 (StorageService interacts with JSON files: world, player, npcs, locations, quests, **enemies, active_combat**, logs)

7. Implementation Tips
...
(Add notes about combat)
*   **Combat State:** Ensure `active_combat.json` is reliably created at the start and deleted *only after* successful summarization.
*   **Stat Block Creation:** Handle potential failures during on-demand stat block creation gracefully (e.g., notify user combat cannot start).
*   **Combat Processor Validation:** `CombatResponseProcessor` should rigorously validate LLM state updates (success counts, condition application) against game rules.
...
(Rest of section remains largely the same)

8. Development Roadmap
...
(Update Processors and Expand Features)
Processors (`Services/Processors/`)
  *   Implement `IEntityProcessor` interface.
  *   Implement concrete entity processors (Quest, NPC, Location, **EnemyStatBlock**).
  *   Implement `UpdateProcessor`.
  *   Implement `ICombatResponseProcessor` and `CombatResponseProcessor`.
  *   Implement `ISummarizePromptProcessor` and `SummarizePromptProcessor`.
Core Services Refactoring
...
Hangfire Integration
...
PresenterService
...
LoggingService (Existing)
Configure Dependency Injection (`Program.cs`)
Testing
  *   Unit tests for builders, processors, providers.
  *   Integration tests for the flow through Hangfire, **including combat initiation and turns**.
Expand Features (Quest steps, **Advanced Combat Options**, etc.)

Conclusion
...
(Remains the same)