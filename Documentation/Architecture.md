Below is a technical architecture document that lays out the core components, data flow, and class/service responsibilities for your text-based RPG system. This document is intended for a developer about to start coding, giving a clear roadmap of what to build, where to place it, and how everything fits together.

Technical Architecture Document
1. High-Level System Overview
The system is a C# backend that processes user inputs (e.g., from a console or a web/UI layer) and uses a Large Language Model (LLM) to generate narrative responses and content for a text-based RPG. We keep persistent JSON files to store game state so that important data doesn't rely solely on the LLM's context window.

Key Goals:

Prompt creation that injects relevant data into LLM requests.
LLM calls that return narrative or content.
Response parsing that applies any JSON updates or data changes to local storage.
A job queue (Hangfire, mandatory for all calls) ensuring concurrency limits.
*   **Modular Design:** Utilize interfaces and specific implementations for AI providers, prompt building, and response processing to enhance flexibility and testability.

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
2.  `AiService.GetCompletionAsync` to get the LLM response string.
3.  `ResponseProcessingService.HandleResponseAsync` (or `HandleCreateResponseAsync` for creation prompts) to process the response and apply updates.
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
Role: Orchestrates the interpretation and processing of the LLM's response.
Splits out user-facing text from hidden JSON (`<donotshow/>`).
Delegates the handling of JSON content (updates or new entity creation) to specialized `IEntityProcessor` implementations or the `UpdateProcessor` based on the `PromptType`.
Uses `Services/Processors/` implementations.
Methods:
HandleResponseAsync(llmResponse, promptType, userId, npcId?): Handles standard DM/NPC responses containing user text and optional hidden JSON. Calls `ProcessHiddenJsonAsync`.
HandleCreateResponseAsync(llmResponse, promptType, userId): Handles responses that are purely JSON for entity creation (e.g., from `CreateQuest` prompts). Cleans/validates JSON and calls `ProcessHiddenJsonAsync`.
ProcessHiddenJsonAsync(jsonContent, promptType, userId): (Private helper) Determines the correct processor (`UpdateProcessor`, `QuestProcessor`, `NPCProcessor`, etc.) based on `PromptType` and calls its `ProcessAsync` or `ProcessUpdatesAsync` method.

2.6. StorageService
Role: Load / Save game data in JSON.
One subfolder per user or campaign.
Possibly merges partial updates.
Methods:
Load<T>(string fileId) → T
Save<T>(string fileId, T entity)
ApplyPartialUpdate(string fileId, JObject patchData)
*   Load specific entity types (e.g., `LoadNPCAsync`, `LoadLocationAsync`).
*   Save specific entity types (e.g., `SaveNPCAsync`, `SaveLocationAsync`).
*   Add messages to conversation logs (e.g., `AddDmMessageAsync`, `AddDmMessageToNpcLogAsync`).

2.7. LoggingService
Role: Provide centralized logs for prompt requests, errors, time taken, etc.
Implementation can be very simple or use a robust library.

2.8. Models
Where: RPGGame/Models/
What: Classes for Npc, Player, Location, Quest, World, Prompt, PromptRequest, ProcessedResult, plus any DTOs. JSON is stored in RPGGame/Data/<UserId>/..., loaded into these model classes at runtime.

2.9. AI Providers (`Services/AIProviders/`)
Role: Handle communication specifics for different LLM APIs (e.g., OpenAI, OpenRouter).
Components:
`IAIProvider`: Interface defining `GetCompletionAsync(Prompt)`.
Implementations (e.g., `OpenAIProvider`, `OpenRouterProvider`): Concrete classes implementing `IAIProvider`.
`AIProviderFactory`: Creates instances of `IAIProvider` based on configuration.

2.10. Prompt Builders (`Services/PromptBuilders/`)
Role: Construct the specific prompt string and associated metadata for different `PromptType`s.
Components:
`IPromptBuilder`: Interface defining `BuildPromptAsync(PromptRequest)`.
Implementations (e.g., `DMPromptBuilder`, `NPCPromptBuilder`, `CreateQuestPromptBuilder`, etc.): Concrete classes implementing `IPromptBuilder`, each responsible for gathering necessary data (from `StorageService`) and formatting the prompt for a specific scenario.
`BasePromptBuilder`: Optional base class for common functionality.

2.11. Processors (`Services/Processors/`)
Role: Handle the domain logic associated with processing LLM responses related to specific entities or applying general updates.
Components:
`IEntityProcessor`: Interface defining `ProcessAsync(JObject jsonData, string userId)`.
Implementations (e.g., `LocationProcessor`, `QuestProcessor`, `NPCProcessor`, `PlayerProcessor`): Concrete classes implementing `IEntityProcessor`, responsible for validating and saving new entities created by the LLM.
`UpdateProcessor`: Handles applying partial updates defined in the hidden JSON of DM/NPC responses (e.g., modifying existing NPC state, updating player inventory). Defines `ProcessUpdatesAsync(JObject updateData, string userId)`.

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
|-- quests/
|   |-- quest_001.json
|-- lore/
|   |-- ...
PromptTemplates Folder:
swift
Copy
Edit
/RPGGame/PromptTemplates/DM/
  |-- SystemDM.txt
  |-- ResponseInstructions.txt
  |-- ExampleResponses.txt
/RPGGame/PromptTemplates/NPC/
  ...
All final data (like NPC changes, quest states) get persisted in these JSON files by StorageService.

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
  `IAIProvider` sends the prompt string to the LLM API -> returns LLM response string.
  Calls `ResponseProcessingService.HandleResponseAsync(llmResponse, PromptType.DM, userId)`.
  `ResponseProcessingService`:
    Extracts user-facing text and hidden JSON (`<donotshow/>`).
    Calls `ProcessHiddenJsonAsync(hiddenJson, PromptType.DM, userId)`.
    `ProcessHiddenJsonAsync` calls `UpdateProcessor.ProcessUpdatesAsync(hiddenJson, userId)`.
    `UpdateProcessor` parses JSON, calls `StorageService` to save changes (e.g., update NPC state, add item to player).
    Adds DM message to log via `StorageService`.
    Returns `ProcessedResult` (containing user-facing text) back up the chain.
Hangfire completes the job and returns the final user-facing text.
PresenterService polls and returns user-facing text to the front-end.

4.2. Large Quest Generation Example
User triggers "Get a job from the barkeep." (This might first be a standard NPC interaction).
The NPC interaction response might contain hidden JSON indicating a quest should be created, processed by `UpdateProcessor`.
`UpdateProcessor` (or similar logic) might then enqueue a *new* Hangfire job with `PromptType.CreateQuest`.
PresenterService enqueues the "Create Quest" job via Hangfire.
HangfireJobsService (Worker):
  Processes the `PromptRequest`.
  Calls `PromptService.BuildPromptAsync(request)` -> `CreateQuestPromptBuilder` selected.
  `CreateQuestPromptBuilder` builds the prompt for quest generation.
  Calls `AiService.GetCompletionAsync(prompt)` -> gets LLM response (expected to be JSON).
  Calls `ResponseProcessingService.HandleCreateResponseAsync(llmResponse, PromptType.CreateQuest, userId)`.
  `ResponseProcessingService`:
    Cleans and validates the response JSON.
    Calls `ProcessHiddenJsonAsync(jsonContent, PromptType.CreateQuest, userId)`.
    `ProcessHiddenJsonAsync` calls `QuestProcessor.ProcessAsync(jsonContent, userId)`.
    `QuestProcessor` validates the quest JSON, potentially generates IDs, interacts with `StorageService` to save the new `quest_xyz.json`, and possibly related new NPCs/Locations if included (or triggers *further* creation prompts).
    Returns `ProcessedResult` (likely empty user-facing text for pure creation).
Hangfire completes the job with a confirmation message or status.
A subsequent DM prompt might be needed to inform the user: "The barkeep reveals a dangerous mission..."

5. Handling Concurrency with Hangfire
Hangfire provides a robust job processing framework with a queue system and multiple worker threads to process jobs efficiently. This ensures sequential access to resources like the LLM and prevents race conditions during data updates triggered by response processing. Hangfire also provides retry mechanisms, dashboards for monitoring, and persistent storage options.

6. Class Diagram (Textual)
Here's a simplified "who calls whom / uses what":

 PresenterService
   -> Hangfire (Enqueues background jobs to HangfireJobsService)

 HangfireJobsService (Processes jobs)
   -> PromptService
      -> IPromptBuilder (Implementations: DMPromptBuilder, etc.)
         -> StorageService (Load data)
   -> AiService
      -> AIProviderFactory
         -> IAIProvider (Implementations: OpenAIProvider, etc.)
            -> (External LLM API)
   -> ResponseProcessingService
      -> (Uses appropriate processor based on PromptType)
         -> IEntityProcessor (Implementations: QuestProcessor, etc.)
            -> StorageService (Save new entities)
         -> UpdateProcessor
            -> StorageService (Save updates)
      -> StorageService (Save logs)

 (All services potentially use LoggingService)
 (StorageService interacts with JSON files)


7. Implementation Tips
Prompt Templates: Keep them modifiable text files. Load them in the relevant `IPromptBuilder` implementations.
Partial Updates: The `UpdateProcessor` is key for handling changes to existing entities described in hidden JSON from DM/NPC prompts. Ensure its logic is robust.
Entity Creation: `IEntityProcessor` implementations handle creation. Ensure they perform validation and ID generation/management before saving via `StorageService`. Consider how to handle creation of multiple related entities (e.g., a quest creating a new NPC and location). This might involve the processor enqueuing further `CreateNPC`/`CreateLocation` jobs.
Configuration: Use `appsettings.json` for AI provider keys, default provider choice, model names, etc. Access via `IConfiguration` injected into services like `AiService`.
Dependency Injection: Configure DI in `Program.cs` to register services, factories, and potentially the dictionaries of builders/processors if not created inline.
Error Handling: Implement robust try-catch blocks in service methods, especially around LLM calls and JSON parsing/processing. Log errors using `LoggingService`.
Async/Await: Use `async`/`await` correctly throughout the call chain, especially for I/O operations (file access in `StorageService`, LLM calls in `IAIProvider`).

8. Development Roadmap
Set Up Folder Structure (Existing)
StorageService (Refine?)
  *   Ensure methods for loading/saving specific entities and logs are present.
AI Providers (`Services/AIProviders/`)
  *   Implement `IAIProvider` interface.
  *   Implement concrete providers (e.g., `OpenAIProvider`).
  *   Implement `AIProviderFactory`.
Prompt Builders (`Services/PromptBuilders/`)
  *   Implement `IPromptBuilder` interface and `BasePromptBuilder` (optional).
  *   Implement concrete builders for each `PromptType` (DM, NPC, Create...).
Processors (`Services/Processors/`)
  *   Implement `IEntityProcessor` interface.
  *   Implement concrete entity processors (Quest, NPC, Location...).
  *   Implement `UpdateProcessor`.
Core Services Refactoring
  *   Update `PromptService` to use builders.
  *   Update `AiService` to use providers/factory.
  *   Update `ResponseProcessingService` to use processors.
Hangfire Integration
  *   Configure Hangfire in Program.cs.
  *   Implement the HangfireJobsService with job processing methods.
PresenterService
  *   Update to create `PromptRequest` and call appropriate Hangfire job methods.
LoggingService (Existing)
Configure Dependency Injection (`Program.cs`)
Testing
  *   Unit tests for builders, processors, providers.
  *   Integration tests for the flow through Hangfire.
Expand Features (Quest steps, Combat, etc.)

Conclusion
This technical architecture ensures:

Modularity (each service/builder/processor has a focused responsibility).
Persistence (JSON for game data, no LLM memory reliance).
Scalability (Hangfire to prevent GPU overload and manage sequential processing).
Flexibility (Easy to add new AI providers, prompt types, or entity types by adding new implementations).
Ease of extension (adding new prompt types or new domain entities involves creating new builder/processor classes).
By following this document, a developer can start coding each service method, hooking them up in a clean, layered manner. The result is a robust, flexible text-based RPG framework that harnesses LLM creativity while maintaining consistent, fair gameplay.