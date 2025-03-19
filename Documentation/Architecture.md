Below is a technical architecture document that lays out the core components, data flow, and class/service responsibilities for your text-based RPG system. This document is intended for a developer about to start coding, giving a clear roadmap of what to build, where to place it, and how everything fits together.

Technical Architecture Document
1. High-Level System Overview
The system is a C# backend that processes user inputs (e.g., from a console or a web/UI layer) and uses a Large Language Model (LLM) to generate narrative responses and content for a text-based RPG. We keep persistent JSON files to store game state so that important data doesn’t rely solely on the LLM’s context window.

Key Goals:

Prompt creation that injects relevant data into LLM requests.
LLM calls that return narrative or content.
Response parsing that applies any JSON updates or data changes to local storage.
A job queue (optional for all calls, mandatory for big tasks) ensuring concurrency limits.
2. Primary Components
Below is a breakdown of the services and models:

2.1. PresenterService
Role: Entry point for user requests.
If a user says “I look around,” PresenterService is called with that input.
It decides if we need to call the LLM immediately (e.g., quick NPC or DM prompt) or enqueue a bigger job (e.g., generating a brand-new quest with multiple NPCs).
Methods:
HandleUserInputAsync(userId, userInput): Returns a string (the final user-facing text).
Internally calls the BackgroundJobService or PromptService → AiService → ResponseProcessingService chain.
2.2. BackgroundJobService (Optional for All Prompts, Recommended for Heavy Calls)
Role: Queues up “prompt jobs” so only one big LLM request runs at a time (especially with a local GPU).
Methods:
EnqueuePromptAsync(PromptJob job): Places a prompt job in a FIFO queue.
A background loop processes each job: builds the prompt, calls the LLM, processes the response.
Returns a Task<string> that completes when the job is finished with a final user-facing text.
2.3. PromptService
Role: Constructs the string prompt for the LLM, pulling from data such as world.json, player.json, location.json, etc.
Key Method: BuildPrompt(PromptType, userInput) → string
Loads templates from PromptTemplates/ folder (system instructions, example responses).
Merges relevant JSON data (locations, NPC states, quests, etc.) depending on the prompt type.
Returns a final multiline string to send to the LLM.
2.4. AiService
Role: Makes the actual LLM call.
Could be a local inference model or an external API like OpenAI.
Possibly chooses the model based on PromptType.
Method:
GetCompletionAsync(promptString, promptType) → Task<string>
2.5. ResponseProcessingService
Role: Interprets the LLM’s response.
Splits out user-facing text from hidden JSON updates (<donotshow> or dmUpdates).
If the JSON indicates new NPCs, quest states, or partial updates, it calls StorageService to apply them.
Methods:
HandleResponse(llmResponse, promptType) → ProcessedResult
Possibly a separate HandleDmResponse(...) vs. HandleNpcResponse(...) vs. HandleCreateQuestResponse(...).
2.6. StorageService
Role: Load / Save game data in JSON.
One subfolder per user or campaign.
Possibly merges partial updates.
Methods:
Load<T>(string fileId) → T
Save<T>(string fileId, T entity)
ApplyPartialUpdate(string fileId, JObject patchData)
2.7. LoggingService
Role: Provide centralized logs for prompt requests, errors, time taken, etc.
Implementation can be very simple or use a robust library.
2.8. Models
Where: RPGGame/Models/
What: Classes for Npc, Player, Location, Quest, World, plus any DTOs (e.g., ProcessedResult, DmUpdates).
JSON is stored in RPGGame/Data/<UserId>/..., loaded into these model classes at runtime.
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
4.1. Standard “DM” Prompt Example
User inputs: “I look around the market.”
PresenterService → calls BackgroundJobService.EnqueuePromptAsync(...) or directly calls PromptService.BuildPrompt(...) if it’s a quick operation.
PromptService loads the partial world.json, player.json, relevant location & NPC summaries, merges them with the DM system text.
AiService sends that final prompt to the LLM.
ResponseProcessingService:
Splits the response at <donotshow>.
Takes the user-facing text → returns to PresenterService.
Takes any JSON block → updates npcs, quests, or historyLog.
PresenterService returns user-facing text to front-end.
4.2. Large Quest Generation Example
User triggers “Get a job from the barkeep.”
PresenterService enqueues a “Create Quest” job.
The job:
Prompts LLM for a freeform quest.
Converts that freeform quest to JSON.
Possibly calls the LLM for new NPC or Location generation.
Saves all new data.
Once complete, a final DM text is produced: “The barkeep reveals a dangerous mission. …”
5. Handling Concurrency with BackgroundJobService
We store PromptJob objects in a queue.
The queue processes one job at a time → ensures the local GPU doesn’t handle multiple calls simultaneously.
For short DM or NPC prompts that do NOT produce subsequent calls, it’s effectively just one pass.
If the user tries to do something else while we’re generating big content, we either block or let them do a second job in the queue.
6. Class Diagram (Textual)
Here’s a simplified “who calls whom”:

pgsql
Copy
Edit
 PresenterService
   -> (optionally calls) BackgroundJobService
      -> (calls in a loop) PromptService
           -> AiService
           <- (result from AiService)
         -> ResponseProcessingService
           -> StorageService (Load/Save data)
           <- (result or updates)
      <- (final user-facing text)
   <- (returns string to user)
7. Implementation Tips
Prompt Templates: Keep them modifiable text files (System instructions, example responses). Load them in PromptService to reduce hard-coded strings.
Partial Updates: If the LLM says “containsNewStuff: true” with new NPC or location data, parse that JSON in ResponseProcessingService and create new .json files.
Multiple “Queues” (Optional): If you want short user interactions to skip the queue for speed, have a “quick lane” for single calls and a “heavy lane” for multi-entity generation.
Guard Rails: You might want to validate new IDs or ensure no collisions when the LLM introduces an NPC with an existing ID.
Summaries: If conversation logs get large, you can summarize them in ResponseProcessingService or in a specialized SummarizationService.
8. Development Roadmap
Set Up Folder Structure

/Models, /Services, /Data/<UserId>, /PromptTemplates.
StorageService

Implement basic Load<T>, Save<T> for JSON.
PromptService

Start with DM & NPC prompts. Load from PromptTemplates/DM/... etc.
AiService

Create a wrapper to call the local LLM or an external API.
ResponseProcessingService

Parse <donotshow> blocks, handle new entity creation or partial updates.
PresenterService & BackgroundJobService

Tie it all together so that user input → queued job → prompt building → LLM → response processing → user-facing text.
LoggingService

Add logs for each step so you can trace performance or handle errors.
Expand

Implement quest creation prompts, location creation prompts, or advanced features (combat, factions, etc.) as needed.
Conclusion
This technical architecture ensures:

Modularity (each service has a focused responsibility).
Persistence (JSON for game data, no LLM memory reliance).
Scalability (BackgroundJobService to prevent GPU overload).
Ease of extension (adding new prompt types or new domain entities is straightforward).
By following this document, a developer can start coding each service method, hooking them up in a clean, layered manner. The result is a robust, flexible text-based RPG framework that harnesses LLM creativity while maintaining consistent, fair gameplay.