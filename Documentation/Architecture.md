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
*   **Time Management:** ISO 8601 formatted timestamps for consistent game time tracking across scenarios and games. Time is stored as a property within the `World` object (`world.json`) and loaded/saved as part of the world state. Time progression is handled by updating this property.

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
Uses `System.Text.Json` with custom converters (`CreationHookConverter`, `UpdatePayloadConverter`, `UpdatePayloadDictionaryConverter`, `LlmSafeIntConverter` located in `Models/Converters.cs`) and potentially the helper class `LlmResponseDeserializer` to deserialize the entire LLM response string into strongly-typed models (e.g., `DmResponse`).
Extracts `userFacingText`, `newEntities` (`List<ICreationHook>`), and `partialUpdates` (`Dictionary<string, IUpdatePayload>`) from the deserialized object.
Delegates the handling of `newEntities` to specialized `IEntityProcessor` implementations and `partialUpdates` to the `UpdateProcessor` based on the `PromptType`.
Uses `Services/Processors/` implementations.
Methods:
HandleResponseAsync(llmResponse, promptType, userId, npcId?): Handles standard DM/NPC responses. Deserializes the `llmResponse` JSON string, extracts data, and calls appropriate processors for `newEntities` and `partialUpdates`.
HandleCreateResponseAsync(llmResponse, promptType, userId): Handles responses that are primarily JSON for entity creation. Deserializes the `llmResponse`, validates, and calls the relevant `IEntityProcessor` for the `newEntities`.

2.6. Storage Services (`Services/Storage/`)
Role: Load, save, and manage persistence of game data in JSON files stored under `/Data/userData/<UserId>/`.

Structure:
*   **`IBaseStorageService` / `BaseStorageService.cs`**: Defines and implements the core, low-level file operations (LoadAsync, SaveAsync, GetFilePath, ApplyPartialUpdateAsync, CopyDirectory). Located in `Services/Storage/`. Uses `System.Text.Json` for serialization and `Newtonsoft.Json.Linq` for partial updates. Constructs paths relative to the `/Data/` directory at the project root.
*   **Specialized Storage Services**: Located within `Services/Storage/`, these classes handle the logic for specific data types, often utilizing `BaseStorageService` for the actual file I/O. Key services include:
    *   `IEntityStorageService` / `EntityStorageService.cs`: Handles loading and saving core game entities like `Player`, `World`, `GameSetting`, `GamePreferences`, `Location`, `Npc`, `Quest`. Also provides methods for querying entities (e.g., `GetNpcsInLocationAsync`, `GetAllNpcsAsync`, `GetAllQuestsAsync`, `GetActiveQuestsAsync`). Crucially, it handles loading the `World` object, which contains the current `GameTime`.
    *   `IConversationLogService` / `ConversationLogService.cs`: Manages the `conversation_log.json` file (adding user/DM messages, wiping).
    *   `IEnemyStatBlockService` / `EnemyStatBlockService.cs`: Manages loading, saving, and checking existence of `EnemyStatBlock` data in `/Data/userData/<UserId>/enemies/`.
    *   `ICombatStateService` / `CombatStateService.cs`: Manages loading, saving, and deleting the `CombatState` data in `/Data/userData/<UserId>/active_combat.json`.
    *   `IInventoryStorageService` / `InventoryStorageService.cs`: Handles adding/removing items and currency from the player's inventory (persisted within `player.json`).
    *   `IGameScenarioService` / `GameScenarioService.cs`: Handles loading data from starting scenarios (`/Data/startingScenarios/`) and creating new game instances based on them.
    *   `IRecentEventsService` / `RecentEventsService.cs`: Manages the `recent_events.json` log.
    *   `ITemplateService` / `TemplateService.cs`: Loads prompt template strings from the `/PromptTemplates/` directory.
    *   `IValidationService` / `ValidationService.cs`: Provides validation methods, such as finding dangling references.
    *   `IWorldSyncService` / `WorldSyncService.cs`: Handles synchronization tasks, like ensuring NPC locations in the world file match individual NPC files.
*   **Game Time Management**: There is no dedicated `GameTimeService`. Game time (`DateTimeOffset`, stored as ISO 8601 string) is a property within the `World` model. It's loaded and saved as part of the `World` object via `EntityStorageService`. Time advancement involves loading the `World`, modifying the `GameTime` property, and saving the `World` back.

2.7. LoggingService
Role: Provide centralized logs for prompt requests, errors, time taken, etc. Located directly in `Services/LoggingService.cs`.
Implementation can be very simple or use a robust library.

2.8. GameNotificationService
Role: Sends real-time notifications to connected clients (UI) using SignalR via the `GameHub` (located in the `/Hubs/` directory). This is used to inform the UI about specific state changes that require a refresh without a full page reload or explicit user action.
Uses `Microsoft.AspNetCore.SignalR`.
Methods:
*   **`NotifyInventoryChangedAsync(string gameId)`**: Sends an "InventoryChanged" message to clients connected to the specified `gameId` group, indicating the player's inventory has been updated and the UI should refetch it.
*   **`NotifyCombatStartedAsync(string gameId, CombatStartInfo initialState)`**: Signals the UI to enter combat mode, providing initial enemy and player state.
*   **`NotifyCombatEndedAsync(string gameId, bool playerVictory)`**: Signals the UI that combat is over and whether the player won.
*   **`NotifyCombatTurnUpdateAsync(string gameId, CombatTurnInfo currentState)`**: Sends turn-by-turn updates to the UI during combat.
*   **`NotifyLocationChangedAsync(string gameId)`**: Notifies clients that the player's location has changed. Sent after all entity updates are processed to ensure UI consistency.
*   **`NotifyGenericAsync(string gameId, string message)`**: Sends a generic notification with a custom message to clients.
*   **`NotifyErrorAsync(string gameId, string errorMessage)`**: Sends an error notification with an error message to clients.

2.9. StatusTrackingService
Role: Tracks the status (e.g., Pending, Running, Completed, Failed) of long-running background jobs, particularly entity creation jobs initiated via Hangfire. This allows API endpoints (like `EntityStatusController`) to report progress to the frontend. Located in `Services/StatusTrackingService.cs`.
*(Specific methods TBD based on implementation)*

2.10. Models (`/Models/`)
Where: `/Models/`
What: Classes for Npc, Player, Quest, World, Prompt, PromptRequest, ProcessedResult, CombatState, EnemyStatBlock, etc. Includes interfaces like `ICreationHook` and `IUpdatePayload` and their implementations (e.g., `NpcCreationHook`, `PlayerUpdatePayload`) to represent structured LLM outputs. Also includes custom `JsonConverter` implementations in `/Models/Converters.cs`. JSON data from `/Data/userData/<UserId>/` is loaded into these model classes at runtime.

Location Model Structure (`/Models/Locations/`):
- `Location.cs` (abstract base class): Defines common properties for all location types.
- `GenericLocation.cs`: A concrete implementation for simple nested locations.
- Specialized Location Classes (`Building.cs`, `Delve.cs`, `Settlement.cs`, `Wilds.cs`): Extend the base `Location` class with type-specific properties.
- `PointOfInterest.cs`, `Valuable.cs`: Additional models likely used within Location contexts.
- `LocationConverter.cs`: JSON converter for proper serialization/deserialization of the location hierarchy.

World Model Structure (`/Models/World.cs`):
- `World`: The game world container with properties including `GameTime` (DateTimeOffset stored as ISO 8601 string), `CurrentPlayer`, `Locations`, `Npcs`, `Quests`, etc.
- Game time is managed as a property within this object.

2.11. AI Providers (`/Services/AIProviders/`)
Role: Handle communication specifics for different LLM APIs (e.g., OpenAI, OpenRouter).
Components:
`IAIProvider`: Interface defining `GetCompletionAsync(Prompt)`.
Implementations (e.g., `OpenAIProvider`, `OpenRouterProvider`): Concrete classes implementing `IAIProvider`.
`AIProviderFactory`: Creates instances of `IAIProvider` based on configuration.

2.12. Prompt Builders (`/Services/PromptBuilders/`)
Role: Construct the specific prompt string and associated metadata for different `PromptType`s.
Components:
`IPromptBuilder`: Interface defining `BuildPromptAsync(PromptRequest)`.
Implementations (e.g., `DMPromptBuilder`, `NPCPromptBuilder`, implementations within `/Services/PromptBuilders/Create/` like `CreateQuestPromptBuilder`, etc.): Concrete classes implementing `IPromptBuilder`, each responsible for gathering necessary data (using services from `/Services/Storage/`) and formatting the prompt for a specific scenario.
*   **`CreateEnemyStatBlockPromptBuilder`**: Gathers NPC/context data to request JSON for enemy combat stats.
*   **`CombatPromptBuilder`**: Loads `CombatState`, `EnemyStatBlock`, `Player` data for combat turns.

2.13. Processors (`/Services/Processors/`)
Role: Handle the domain logic associated with processing LLM responses related to specific entities or applying general updates.
Components:
`IEntityProcessor`: Interface defining `ProcessAsync(List<ICreationHook> creationHooks, string userId)`. Implemented by specific processors like `NPCProcessor`.
Implementations (e.g., `LocationProcessor`, `QuestProcessor`, `NPCProcessor`, `PlayerProcessor`, **`EnemyStatBlockProcessor`**): Concrete classes implementing `IEntityProcessor`, responsible for validating and saving new entities defined in the `creationHooks` (using services from `/Services/Storage/`).
`UpdateProcessor`: Handles applying partial updates. Defines `ProcessUpdatesAsync(Dictionary<string, IUpdatePayload> updateData, string userId)`. Responsible for parsing the `updateData` dictionary and calling appropriate storage services to apply changes to existing entities.
**`ICombatResponseProcessor`**: Interface defining `ProcessCombatResponseAsync(string llmResponse, string userId)`.
**`CombatResponseProcessor`**: Concrete implementation for handling combat turn JSON responses, updating `CombatState` (via `CombatStateService`), and checking for combat end conditions.
**`ISummarizePromptProcessor`**: Interface defining methods for processing summary requests.
**`SummarizePromptProcessor`**: Concrete implementation handling both general summaries and combat summaries (using `ProcessSummaryAsync` and `ProcessCombatSummaryAsync`).

2.14. Controllers (`/Controllers/`)
Role: Expose API endpoints for frontend interaction.
Components:
`InteractionController`: Handles general user input (`/api/interaction/input`).
**`CombatController`**: Handles combat-specific actions (`/api/combat/action`). *(Note: Start/End/Get might be elsewhere or integrated)*
`EntityStatusController`: Handles checking status of background entity creation jobs (`/api/status/{jobId}`), likely using `StatusTrackingService`.
`GameStateController`: Provides endpoints to retrieve various parts of the game state (e.g., player data, location details, inventory) for the UI (`/api/gamestate/...`).
`GameManagementController`: Handles game creation, loading, saving, and potentially listing available games/scenarios (`/api/management/...`).
`GameAdminController`: Provides administrative endpoints, possibly for debugging, data validation, or scenario management (`/api/admin/...`).

2.15. PromptTemplates (`/PromptTemplates/`)
Role: Provide standardized, consistent instructions and formatting for all AI LLM interactions. These templates ensure reliable JSON responses by specifying expected return format and showcasing examples of valid responses.

Components:
- **Base Templates**: Each template type includes core components such as instructions, expected JSON structure, constraints, and example inputs/outputs.
- **Specialized Subdirectories**: Organized by function (e.g., `/DmPrompt/`, `/NPCPrompt/`, `/Create/`, `/Combat/`, `/Summarize/`, `/SummarizeCombat/`, `/System/`), each containing template variants.

Key Template Types:
- **DM Templates** (`/DmPrompt/`): Used for general game narration and world responses.
- **NPC Templates** (`/NPCPrompt/`): Used for NPC dialogue and interactions.
- **Creation Templates** (`/Create/`): Contains sub-templates or specific files for generating entities. Examples likely include:
  - Quest generation (`CreateQuest`)
  - NPC generation (`CreateNPC`)
  - Location generation (`CreateLocation`)
  - Enemy Stat Block generation (`CreateEnemyStatBlock`) is a separate top-level folder: `/CreateEnemyStatBlock/`.
- **Combat Templates** (`/Combat/`): Used to process combat actions and generate turn-based combat responses.
- **Summarization Templates** (`/Summarize/`, `/SummarizeCombat/`): Used to create narrative summaries.
- **System Templates** (`/System/`): Templates for system-level tasks (specifics TBD).

Structure:
Usage:
- Templates are loaded by the `TemplateService` (from `/Services/Storage/`) using methods like `GetDmTemplateAsync()` or `GetNpcTemplateAsync()`.
- The appropriate `PromptBuilder` implementation adds context-specific information to the template.
- The resulting prompt is sent to the LLM, which uses the template instructions to format its response properly.
- The structured responses enable reliable parsing by the `ResponseProcessingService` (potentially using `LlmResponseDeserializer`).

3. Data & File Structure
Data Folder (`/Data/`):
*   `/Data/userData/<UserId>/`: Contains persistent data for each game instance (user).
    ```
    /Data/userData/<UserId>/
    |-- world.json          (Contains world state including GameTime)
    |-- player.json         (Contains player stats, inventory, active quests, etc.)
    |-- gameSetting.json
    |-- gamePreferences.json
    |-- npcs/
    |   |-- npc_001.json
    |   |-- ...
    |-- locations/
    |   |-- loc_001.json
    |-- enemies/
    |   |-- enemy_001.json  (Generated enemy stat blocks)
    |-- quests/
    |   |-- quest_001.json
    |-- logs/
    |   |-- conversation_log.json
    |   |-- recent_events.json
    |-- active_combat.json  (Exists only during active combat)
    ```
*   `/Data/startingScenarios/`: Contains template data for starting new games.

PromptTemplates Folder (`/PromptTemplates/`):
```
/PromptTemplates/
|-- DmPrompt/
|-- NPCPrompt/
|-- Create/             (Contains templates for Quest, NPC, Location etc.)
|-- CreateEnemyStatBlock/
|-- Combat/
|-- Summarize/
|-- SummarizeCombat/
|-- System/
```
All final game state data gets persisted in the JSON files under `/Data/userData/<UserId>/` by the relevant services in `/Services/Storage/`.

4. Data Flow (Sequence)
4.1. Standard "DM" Prompt Example
User inputs: "I look around the market."
PresenterService receives input, creates `PromptRequest` (userId, input, `PromptType.DM`).
PresenterService calls `BackgroundJob.Enqueue<HangfireJobsService>(x => x.ProcessUserInputAsync(request))`.
HangfireJobsService (Worker):
  Processes the `PromptRequest`.
  Calls `PromptService.BuildPromptAsync(request)` -> `DMPromptBuilder` is selected.
  `DMPromptBuilder` loads `world.json` (via `EntityStorageService`, includes game time), `player.json`, relevant `location.json`, NPC summaries, merges with templates -> returns `Prompt` object.
  Calls `AiService.GetCompletionAsync(prompt)` -> `AIProviderFactory` creates default `IAIProvider`.
  `IAIProvider` sends the prompt string to the LLM API -> returns LLM response string (as a single JSON object).
  Calls `ResponseProcessingService.HandleResponseAsync(llmResponse, PromptType.DM, userId)`.
  `ResponseProcessingService`:
    Deserializes `llmResponse` into a `DmResponse` object (using converters/`LlmResponseDeserializer`).
    Extracts `userFacingText`, `newEntities`, and `partialUpdates`.
    **Checks for `CombatTriggered == true`**. If true, initiates **Combat Initiation Flow (See 4.3)** and returns `CombatPending = true`.
    If `CombatTriggered` is false:
      Calls relevant `IEntityProcessor` implementations for any `newEntities`.
      Calls `UpdateProcessor.ProcessUpdatesAsync(partialUpdates, userId)`.
      `UpdateProcessor` iterates through `partialUpdates`, calling appropriate storage services (e.g., `EntityStorageService`, `InventoryStorageService`) to save changes.
      When a location change is detected, potentially triggers other actions (e.g., summarization via `ConversationLogService` or `RecentEventsService`).
      Adds DM message to log via `ConversationLogService`.
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
  `CreateEnemyStatBlockPromptBuilder` loads necessary data, uses templates (via `TemplateService`).
  Calls `AiService.GetCompletionAsync` -> gets LLM response (JSON for `EnemyStatBlock`).
  Calls `ResponseProcessingService.HandleCreateResponseAsync`.
  `ResponseProcessingService` deserializes JSON into `EnemyStatBlock` (via hook/processor).
  Calls `EnemyStatBlockProcessor.ProcessAsync` (or similar logic).
  `EnemyStatBlockProcessor` validates and calls `EnemyStatBlockService.SaveEnemyStatBlockAsync`.
Completion: Hangfire marks the creation job complete. The **Combat Initiation Flow** continuation can now proceed.

4.3. Combat Initiation Flow
Trigger: `ResponseProcessingService.HandleResponseAsync` detects `CombatTriggered == true`.
Hangfire Enqueues: `HangfireJobsService.EnsureEnemyStatBlockAndInitiateCombatAsync(userId, enemyId, initialCombatText)`.
HangfireJobsService (Worker - `Ensure...`):
  Calls `EnemyStatBlockService.CheckIfStatBlockExistsAsync`.
  If **False**: Enqueues `CreateEnemyStatBlockAsync` (Flow 4.2) and sets `InitiateCombatAsync` as its continuation.
  If **True**: Enqueues `InitiateCombatAsync` directly.
HangfireJobsService (Worker - `InitiateCombatAsync`):
  Loads `EnemyStatBlock` using `EnemyStatBlockService`.
  Creates new `CombatState` object.
  Calls `CombatStateService.SaveCombatStateAsync`.
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
  `CombatPromptBuilder` loads `CombatState` (via `CombatStateService`), `EnemyStatBlock` (via `EnemyStatBlockService`), `Player` (via `EntityStorageService`).
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
  If **Ending**: Sets `CombatState.IsActive = false`, saves state (via `CombatStateService`), calls `NotifyCombatEndedAsync`, enqueues summarization job (Flow 4.5).
  If **Continuing**: Saves updated `CombatState` (via `CombatStateService`), calls `NotifyCombatTurnUpdateAsync` (optional).
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
  Calls `RecentEventsService.AddSummaryToRecentEventsAsync`.
  Calls `ConversationLogService.AddDmMessageAsync`.
HangfireJobsService (Worker - `ProcessSummarizationJobAsync`):
  **After successful processing**: Calls `CombatStateService.DeleteCombatStateAsync`.
Completion: Summary logged, combat state deleted.

5. Handling Concurrency with Hangfire
Hangfire provides a robust job processing framework...
(Rest of section remains the same)

6. Class Diagram (Textual)
(Update with new components)
 PresenterService
   -> Hangfire (Enqueues background jobs to HangfireJobsService)

 CombatController, GameStateController, GameManagementController, GameAdminController
   -> PresenterService (for actions requiring LLM/background processing)
   -> Storage Services (for direct state reads/writes)
   -> StatusTrackingService (potentially)

 HangfireJobsService (Processes jobs)
   -> PromptService
      -> IPromptBuilder (Implementations in `/Services/PromptBuilders/` and `/Services/PromptBuilders/Create/`)
         -> Storage Services (Load data)
   -> AiService
      -> AIProviderFactory
         -> IAIProvider
            -> (External LLM API)
   -> ResponseProcessingService
      -> LlmResponseDeserializer (Helper)
      -> (Uses appropriate processor based on PromptType in `/Services/Processors/`)
         -> IEntityProcessor (Implementations: ..., `EnemyStatBlockProcessor`)
         -> UpdateProcessor
         -> `ICombatResponseProcessor` / `CombatResponseProcessor`
         -> `ISummarizePromptProcessor` / `SummarizePromptProcessor`
         -> Storage Services (Save entities/updates)
   -> GameNotificationService
      -> IHubContext<GameHub> (SignalR, in `/Hubs/`)
   -> Storage Services (e.g., `CombatStateService.DeleteCombatStateAsync`, `WorldSyncService`)
   -> StatusTrackingService (Update job status)

 EntityStatusController
   -> StatusTrackingService

 (All services potentially use LoggingService)
 (Storage Services in `/Services/Storage/` interact with JSON files in `/Data/userData/<UserId>/`)

7. Implementation Tips
...
(Add notes about combat)
*   **Combat State:** Ensure `/Data/userData/<UserId>/active_combat.json` is reliably created at the start by `CombatStateService` and deleted *only after* successful summarization by the Combat Resolution Flow (via `CombatStateService.DeleteCombatStateAsync`).
*   **Stat Block Creation:** Handle potential failures during on-demand stat block creation (Flow 4.2) gracefully. The `EnsureEnemyStatBlockAndInitiateCombatAsync` job should handle cases where `EnemyStatBlockService` fails to create/save the block.
*   **Combat Processor Validation:** `CombatResponseProcessor` should rigorously validate LLM state updates (success counts, condition application) against game rules before updating the `CombatState` via `CombatStateService`.

*   **Time Management:**
    - Game time (`DateTimeOffset`) is stored as an ISO 8601 formatted string within the `World` object (`/Models/World.cs`).
    - The `World` object is loaded and saved via `EntityStorageService` located in `/Services/Storage/`.
    - Time progression requires loading the `World` object, modifying its `GameTime` property, and saving the updated `World` object back using `EntityStorageService`.
    - Time-related prompts should retrieve the current game time by accessing the `GameTime` property of the loaded `World` object.

*   **NPC Location Context:** The system tracks NPCs exclusively via their `currentLocationId` property stored in their respective JSON files (`/Data/userData/<UserId>/npcs/`). This data is loaded/saved via `EntityStorageService`. There are no separate visibility flags or location-based NPC lists. The narrative description of NPCs is managed through the DM prompt based on narrative context. `WorldSyncService` might be used to ensure consistency between NPC files and any world-level summaries if needed.

...
(Rest of section remains largely the same)

8. Development Roadmap
...
(Update Processors and Expand Features)
Processors (`/Services/Processors/`)
  *   Implement `IEntityProcessor` interface.
  *   Implement concrete entity processors (Quest, NPC, Location, `EnemyStatBlockProcessor`).
  *   Implement `UpdateProcessor`.
  *   Implement `ICombatResponseProcessor` and `CombatResponseProcessor`.
  *   Implement `ISummarizePromptProcessor` and `SummarizePromptProcessor`.
Core Services Refactoring
  * Review interaction between `BaseStorageService` and specific storage services in `/Services/Storage/`.
  * Implement `StatusTrackingService`.
  * Add interfaces (`IBaseStorageService`, `IEntityStorageService`, etc.) in `Services/Storage/Interfaces/` if not already present.
...
Testing
  *   Unit tests for builders, processors, providers, storage services.
  *   Integration tests for the flow through Hangfire, **including combat initiation and turns**, using actual storage services.
Expand Features (Quest steps, **Advanced Combat Options**, Inventory interactions, etc.)

Conclusion
...
(Remains the same)