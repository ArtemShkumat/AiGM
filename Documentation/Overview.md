Below is a high-level, non-technical project/game overview summarizing the vision, goals, and overall flow of your text-based RPG system that integrates LLM-driven storytelling.

Project/Game Overview
1. Project Vision
Create a dynamic, text-based RPG where a Large Language Model (LLM) serves as the Game Master, narrating the world and interacting with players. Unlike a simple "chat with an AI" scenario, this game keeps persistent game data on disk, allowing for rich, long-term story arcs, fair quest structures, and complex NPC interactions that don't vanish when context windows run out.

2. Core Goals & Features
Lightweight Middle-Man Client

The system stands between the user and the LLM, feeding the LLM only the relevant data (location, NPC states, quest info) to maintain context.
Stores all critical game state (NPCs, quests, player data) in JSON files on disk, ensuring the LLM never "forgets" important details.
Fair & Pre-Planned Quests

Users can receive multi-step or open-ended quests with clear success/fail conditions.
Quests, NPCs, or entire areas can be created on-the-fly by the LLM, but once created, they become persistent data the system references in subsequent interactions.
Text-Based Roleplay

The user can talk to a DM prompt to explore the world ("What do I see?" "I move left toward the blacksmith.")
The user can talk to NPC prompts for one-on-one roleplay. NPCs have limited knowledge of the world, reflecting only what they truly know in their stored data.
Strong "Single Source of Truth"

JSON files (locations, NPCs, quests, lore, etc.) anchor the game's reality.
The LLM is explicitly instructed to respect these existing records (rather than invent contradictory elements).
Preventing "Track-Laying"

The LLM can't spontaneously retcon quest objectives or NPC states. If the LLM tries to do so, the middle-man client enforces the stored data.
The system can automatically pre-generate quest steps and store them, ensuring the user can fail or succeed logically.
3. Game Flow & Feel
Starting the Game

The user picks or creates a player character (stored in player.json).
The system loads world.json to identify the current day, weather, or global flags.
The user sees an opening "DM narrative" describing their starting location (like a village, tavern, or starship corridor--depending on the setting).
Moving Around

The user issues DM prompts: "I head to the marketplace" or "Explore the forest."
The system receives the input, creates a request object, and queues it in a background job service.
The job service orchestrates the process: a prompt builder gathers relevant location/NPC/player data, an AI service calls the LLM, and a response processor handles the narrative and any hidden JSON updates (like new items or NPC state changes), saving them to disk.
The DM response describes the new scene and can optionally add new entities (e.g., "a mysterious peddler arrives"), which get saved in the JSON via specialized processors.
Interacting with NPCs

The user switches to an NPC prompt: "Talk to Elena the Blacksmith."
The system loads npc_elenaBlacksmith.json, location data, and any logs (e.g., past interactions).
The system queues the request. The background service uses an NPC-specific prompt builder to gather relevant data (NPC state, location, conversation history), calls the LLM via the AI service, and processes the response.
The LLM responds in character, referencing only what Elena knows or believes. The response might include hidden JSON to update Elena's state or relationship with the player, handled by the response processor and saved to disk.
Quest Generation

If an NPC says, "I have a job for you," a behind-the-scenes quest creation step might occur (with user approval).
The system queues a quest creation request. The background service uses a quest-specific prompt builder, calls the LLM (potentially multiple times -- first for description, then for structured JSON), and uses a quest processor to validate and save the resulting quest JSON, including steps, conditions, and any new NPCs/locations.
Once fully generated, the user sees the final "Job details" in a fresh DM response.
Long-Term Persistence

The user can leave the game at any point. Next session, the system reloads all JSON data--locations, NPC states, active quests.
The LLM sees only relevant data for each scene, preserving continuity over days or weeks.
4. Concurrency & Performance
A Hangfire job queue ensures big tasks (like generating a whole new quest with multiple NPCs/locations) happen sequentially, preventing GPU overload.
For small interactions (like a quick conversation with a single NPC), the user might only see a brief queue wait if multiple tasks are pending.
5. Tone & Setting Customizations
The gameSetting field in each prompt ensures users can run "Medieval fantasy," "Space opera," or "Zombie apocalypse" worlds.
The system can tailor LLM responses to the chosen tone (lighthearted, dark, gritty, comedic) and complexity (casual or advanced roleplay).
6. Limitations & Future Enhancements
Local LLM Performance
If the user runs a local 7B model, they might experience slow generation times. The job queue ensures stability but can create longer waiting for big tasks.
Conversation Log Summarization
Currently, the system can wipe or summarize logs on location change to keep context short. Long-term, advanced summarization techniques might be introduced.
Combat & Mechanics
Future expansions could add structured combat prompts or specialized sub-systems (like skill checks, dice rolling).
Multiplayer
The system is designed for single-user sessions right now. Adding concurrency or multi-player support would require further design.
7. High-Level Technologies & Data
Backend Services (in C#):
PresenterService for user input (creates requests).
Hangfire for queuing and orchestrating all requests.
BackgroundJobService for queuing and orchestrating all requests.
PromptService (uses specialized `IPromptBuilder` implementations).
AiService (uses specialized `IAIProvider` implementations via a factory).
ResponseProcessingService (uses specialized `IEntityProcessor` / `UpdateProcessor` implementations).
StorageService for reading/writing JSON entity files.
LoggingService for tracking events or errors.
Data Folder per user session or "campaign."
world.json, player.json, plus subfolders for npcs/, quests/, locations/, lore/.
8. Desired Experience for the Player
Immersive Storytelling: The LLM's creative narration is the central experience.
Real Consequences: Quests can fail, NPC relationships matter. The system never forgets.
Flexible Roleplay: The user can approach challenges from multiple angles--diplomacy, stealth, brute force, or creative use of items.
Sense of Growth: Over multiple sessions, the world evolves. The player sees their actions reflected in the persistent data.
Conclusion
This document captures the vision, flow, and key features of your LLM-driven RPG:

Persistent, fair questlines
Rich NPC interactions
DM vs. NPC prompts for flexible roleplay
Single source of truth via JSON files
Job queue for concurrency control
Players get a robust, evolving narrative that never "forgets" important details, bridging the gap between AI-driven creativity and consistent, long-term RPG play.