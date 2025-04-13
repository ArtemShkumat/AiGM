# Guide: Creating New Starting Scenarios

This guide provides detailed instructions on how to create the necessary JSON files for a new starting scenario in the text-based RPG system. Adhering to these structures and conventions is crucial for the game engine to correctly load and interpret the initial game state.

## 1. Overview

A starting scenario defines the initial state of the game world when a user begins a new session or campaign. It consists of several JSON files placed within a dedicated data directory (`Data/startingScenarios/<scenarioId>/`). These files describe the world, the player character, the starting location, and any NPCs present at the start.

The following folder structure is required for a starting scenario:
```
Data/
  startingScenarios/
    <scenarioId>/          # Your scenario's unique identifier (e.g., "fantasy_emberhold")
      player.json          # The player character definition
      world.json           # Global world parameters
      gameSetting.json     # Genre, theme, and setting information
      gamePreferences.json # Game preferences and settings
      conversationLog.json # Empty conversation log
      recentEvents.json    # Empty recent events
      locations/           # Folder containing all location files
        loc_*.json         # Individual location files
      npcs/                # Folder containing all NPC files
        npc_*.json         # Individual NPC files
      quests/              # Folder containing all quest files
        quest_*.json       # Individual quest files
      lore/                # Folder containing all lore files
        lore_*.json        # Individual lore files
```

When a user starts a new game based on a scenario, the system copies these files to `Data/userData/<gameId>/` where they are modified during gameplay.

## 2. Core Scenario Files

The following JSON files are essential for a complete starting scenario:

*   `world.json`: Defines global world parameters, game time, and references to locations, NPCs, and quests.
*   `player.json`: Defines the player character's initial state.
*   `gameSetting.json`: Defines the genre, theme, description, and starting location for the scenario.
*   `gamePreferences.json`: Defines game mechanic preferences specific to the scenario.
*   `conversationLog.json`: Initially contains an empty messages array, will store conversation history during gameplay.
*   `recentEvents.json`: Initially empty, will store recent game events during gameplay.
*   Location files in the `locations/` subfolder (e.g., `locations/loc_emberhold.json`): Define all locations in the world.
*   NPC files in the `npcs/` subfolder (e.g., `npcs/npc_elderVarric.json`): Define all NPCs in the world.
*   Quest files in the `quests/` subfolder (e.g., `quests/quest_emberCrystal.json`): Define all quests in the world.
*   Lore files in the `lore/` subfolder (e.g., `lore/lore_emberCrystal.json`): Define all lore items in the world.

## 3. File Structures and Key Fields

### 3.1. `world.json`

This file holds global information about the game world. Structure it according to the `World.cs` model:

```json
{
  "type": "WORLD", // Always set this to "WORLD"
  "gameTime": "1247-05-15T14:30:00Z", // ISO timestamp for the starting game time
  "daysSinceStart": 1, // Days elapsed since the beginning of the scenario
  "currentPlayer": "", // Usually empty at start
  "worldStateEffects": { // Dictionary of global state conditions
    "weather": "clear",
    "season": "spring"
  },

  "lore": [ // Important world lore elements
    {
      "id": "lore_emberCrystal",
      "title": "The Ember Crystal",
      "summary": "An ancient artifact with significant story importance."
    }
  ],

  "locations": [ // List of location summaries
    {
      "id": "loc_starting_tavern",
      "name": "The Rusty Anchor"
    },
    {
      "id": "loc_town_square",
      "name": "Town Square"
    }
  ],

  "npcs": [ // List of NPC summaries
    {
      "id": "npc_friendly_barkeep",
      "name": "Bob the Barkeep"
    }
  ],

  "quests": [ // List of quest summaries
    {
      "id": "quest_missing_shipment", 
      "title": "The Missing Shipment"
    }
  ]
}
```

**Key Points:**

*   `type`: Must be exactly `"WORLD"`.
*   `gameTime`: Sets the starting time in the world. Use ISO format.
*   `worldStateEffects`: Used for global conditions like weather, season, or special world states.
*   The lists for `lore`, `locations`, `npcs`, and `quests` contain summary objects, not just IDs.
*   These summary objects must match the full detailed objects in their respective files.

### 3.2. `player.json`

This file defines the player character. Structure it according to `PromptTemplates/Create/Player/OutputStructure.txt`.

```json
{
  "type": "PLAYER", // MUST be "PLAYER"
  "id": "", // Empty for the new player.
  "name": "Player Character Name",
  "age": 30, // Set a reasonable starting age
  "currentLocationId": "loc_starting_location_id", // MUST match the ID of the starting location file

  "visualDescription": {
    "gender": "Male/Female",
    "body": "Brief description (e.g., 'Average build, scar over left eye')",
    "visibleClothing": "Starting clothes (e.g., 'Simple tunic and trousers')",
    "condition": "Initial state (e.g., 'Healthy', 'Tired')"
  },

  "backstory": "A short backstory relevant to the scenario.",

  "relationships": [], // Usually empty at start, unless starting with pre-existing connections

  "inventory": [
    // { "name": "Rusted Dagger", "description": "A simple, worn dagger.", "quantity": 1 },
    // { "name": "Waterskin", "description": "Holds water.", "quantity": 1 }
  ],

  "money": 10, // Starting currency amount

  "statusEffects": [], // Usually empty at start

  "rpgTags": [
    // { "name": "Determined", "description": "Doesn't give up easily." }
  ],

  "activeQuests": [], // Always empty for a new character

  "playerLog": [], // Always empty for a new character

  "notes": "Optional notes about the character's immediate goals or situation."
}
```

**Key Points:**

*   `type`: Must be exactly `"PLAYER"`.
*   `id`: Must be unique.
*   `currentLocationId`: **Critically important.** Must match the `id` field of the starting `location_*.json` file.
*   `inventory`, `money`, `rpgTags`: Define the player's starting resources and abilities.
*   `activeQuests`, `playerLog`: Must be empty arrays `[]` initially.

### 3.3. `location_*.json`

Locations define the game world's spaces. Place them in the `locations/` subfolder (e.g., `locations/loc_town_square.json`). The structure depends on the `locationType`.

**Common Fields (All Location Types):**

*   `type`: Must be `"LOCATION"`.
*   `id`: Unique identifier (e.g., `"loc_town_square"`). This is referenced by `player.json` and `npc.json`.
*   `name`: Human-readable name (e.g., `"Town Square"`).
*   `description`: Narrative description used by the LLM.
*   `parentLocationId`: ID of the containing location (e.g., a building might have a settlement as a parent). `null` or omitted if it's a top-level location.
*   `connectedLocations`: Array of IDs of directly accessible locations (e.g., `["loc_tavern", "loc_north_road"]`).
*   `npcs`: Array of IDs of NPCs currently present in this location (e.g., `["npc_town_crier", "npc_merchant_eliza"]`).

**Location Types:**

1.  **`BUILDING`**: Indoor locations like taverns, shops, houses.
    *   Use `PromptTemplates/Create/Location/Building/OutputStructure.txt`.
    *   Key Fields: `exterior_description`, `purpose`, `floors` (containing `rooms`, `points_of_interest`, `valuables`, `npcs` within rooms, `connected_rooms`).

2.  **`SETTLEMENT`**: Towns, villages, cities. Can contain buildings and districts.
    *   Use `PromptTemplates/Create/Location/Settlement/OutputStructure.txt`.
    *   Key Fields: `core_identity` (size, purpose, history), `demographics` (population, factions), `districts` (containing `notable_features`, `npcs`, `buildings`).

3.  **`DELVE`**: Dungeons, ruins, dangerous structured locations (often following the 5-room dungeon model).
    *   Use `PromptTemplates/Create/Location/Delve/OutputStructure.txt`.
    *   Key Fields: `purpose`, `rooms` (structured with roles: Entrance, Puzzle, Setback, Climax, Reward), `hazard_or_guardian`, `reward_or_revelation`.

4.  **`WILDS`**: Outdoor areas like forests, mountains, swamps.
    *   Use `PromptTemplates/Create/Location/Wilds/OutputStructure.txt`.
    *   Key Fields: `terrain`, `dangers`, `details` (wildlife, weather), `points_of_interest`, `traversal` (difficulty, obstacles, paths).

**Key Points:**

*   Choose the correct `locationType` and use the corresponding structure file.
*   Ensure `id` is unique and correctly referenced elsewhere.
*   Populate `connectedLocations` to define map navigation.
*   Accurately list NPC IDs in the main `npcs` array or within specific rooms/districts as appropriate for the location type.

### 3.4. `npc_*.json`

NPC files define non-player characters. Place them in the `npcs/` subfolder (e.g., `npcs/npc_barkeep_bob.json`). Structure them according to `PromptTemplates/Create/NPC/OutputStructure.txt`.

```json
{
  "type": "NPC", // MUST be "NPC"
  "id": "npc_unique_identifier", // e.g., "npc_barkeep_bob"
  "name": "NPC Name", // e.g., "Bob the Barkeep"
  "currentLocationId": "loc_starting_tavern", // MUST match the ID of the location where the NPC starts
  "knownToPlayer": false, // Usually false at start, unless the player knows them
  "knowsPlayer": false, // Usually false at start
  "visibleToPlayer": true, // Set to true if the NPC should be immediately visible when the player enters the starting location

  "visualDescription": {
    "gender": "Male",
    "body": "Burly, with a stained apron",
    "visibleClothing": "Simple shirt and apron",
    "condition": "Busy",
    "resemblingCelebrity": "Optional: e.g., 'Nick Offerman'" // Fun flavor
  },

  "personality": {
    "temperament": "Gruff but fair",
    "quirks": "Constantly polishing mugs",
    "motivations": "Running a successful tavern",
    "fears": "Running out of ale",
    "secrets": [
      // "Waters down the expensive whiskey"
    ]
  },

  "backstory": "Brief history of the NPC.",
  "currentGoal": "What the NPC is doing right now (e.g., 'Serving drinks').",
  "age": 45,
  "dispositionTowardsPlayer": "NEUTRAL", // Initial attitude

  "knownEntities": {
    "npcsKnown": [
      // { "name": "Town Guard Captain", "levelOfFamiliarity": "FAMILIAR", "disposition": "NEUTRAL" }
    ],
    "locationsKnown": [
      // "loc_town_square", "loc_brewery"
    ]
  },

  "questInvolvement": [], // List of Quest IDs this NPC is part of

  "inventory": [
    // { "name": "Keys to the cellar", "description": "A ring of brass keys.", "quantity": 1 }
  ],

  "conversationLog": [] // Always empty at start
}
```

**Key Points:**

*   `type`: Must be exactly `"NPC"`.
*   `id`: Must be unique and referenced correctly in location files.
*   `currentLocationId`: **Critically important.** Must match the `id` of the location the NPC starts in.
*   `knownToPlayer`, `knowsPlayer`, `visibleToPlayer`: Set these boolean flags carefully to define the initial interaction state. If the NPC is in the starting room with the player, `visibleToPlayer` should likely be `true`.
*   `personality`, `backstory`, `currentGoal`: Provide enough detail for the LLM to roleplay the NPC effectively.
*   `knownEntities`: Defines the NPC's awareness of the world.
*   `conversationLog`: Must be an empty array `[]` initially.

### 3.5. `gameSetting.json`

This file defines the genre, theme, and overall setting for the scenario:

```json
{
  "genre": "fantasy", 
  "theme": "adventure",
  "description": "A detailed description of your scenario's setting. This field should contain rich background information about the world, including any unique aspects of the setting, historical events, factions, key locations, timeline information, and magical reality. Group related information under headings like TIMELINE, KEY LOCATIONS, MAJOR FACTIONS, etc.",
  "startingLocation": "loc_emberhold",
  "gameName": "The Emberhold",
  "setting": "medieval",
  "currencies": ["Copper", "Silver", "Gold"],
  "economy": "Currency conversion: 1 Gold = 10 Silver = 100 Copper\n\nTypical costs:\n- Simple meal at tavern: 3 Silver\n- Ale, mug: 4 Copper\n- Inn stay (modest): 5 Silver per night\n- Basic dagger: 2 Gold\n\nTypical incomes:\n- Village (poor): 5-10 Gold monthly\n- Town (modest): 30-60 Gold monthly\n- City (wealthy): 200+ Gold monthly"
}
```

**Key Points:**

*   `genre`: The broad genre classification (e.g., "fantasy", "sci-fi", "post-apocalyptic").
*   `theme`: The thematic focus (e.g., "adventure", "mystery", "horror").
*   `description`: A comprehensive description of the scenario setting. This field should contain all essential background information about the world, its history, key locations, and factions.
*   `startingLocation`: **Critically important.** Must match the `id` of the location where the player starts.
*   `gameName`: The display name for the scenario.
*   `setting`: The time period or technological level (e.g., "medieval", "modern", "futuristic").
*   `currencies`: List of currency types used in the game world.
*   `economy`: Detailed information about the economic system, including currency conversion rates, typical costs for common items and services, and average income levels for different social classes in various settlement sizes.

### 3.6. `gamePreferences.json`

This file defines game mechanic preferences specific to the scenario:

```json
{
  "tone": "atmospheric",
  "complexity": "high",
  "ageAppropriateness": "adult"
}
```

**Key Points:**

*   `tone`: Describes the general tone of the scenario (e.g., "atmospheric", "light-hearted", "gritty", "immersive", "neutral").
*   `complexity`: Indicates the complexity level of the game mechanics (e.g., "low", "medium", "high").
*   `ageAppropriateness`: Specifies the intended age range (e.g., "child", "teen", "adult").

## 4. Linking Entities and Consistency

The `id` fields are the glue holding the scenario together.

*   `player.json`'s `currentLocationId` points to a `location_*.json` file.
*   `npc.json`'s `currentLocationId` points to a `location_*.json` file.
*   `location_*.json`'s `npcs` array lists `npc_*.json` IDs present in that location.
*   `location_*.json`'s `connectedLocations` array lists other `location_*.json` IDs.
*   `location_*.json`'s `parentLocationId` points to another `location_*.json` ID (for nested locations like buildings within settlements).
*   NPC `knownEntities` and `relationships` reference other NPC/Location IDs.

**Ensure all referenced IDs exist and are consistent across all files.** Mismatched IDs will break navigation and interactions.

## 5. Setting the Initial State

*   **Player Position:** Double-check that the `player.json`'s `currentLocationId` matches the intended starting `location_*.json` file's `id`.
*   **NPC Visibility:** For NPCs intended to be immediately visible to the player upon starting, set their `visibleToPlayer` flag to `true` in their respective `npc_*.json` file AND ensure their `currentLocationId` matches the player's starting location ID AND ensure they are listed in the `npcs` array of that location file.
*   **Initial Knowledge:** Set `knownToPlayer` and `knowsPlayer` to `false` for NPCs the player shouldn't know at the very beginning.
*   **Starting Gear/Resources:** Populate player `inventory` and `money` appropriately for the scenario.

## 6. Best Practices

*   **Start Small:** Begin with a single starting location, the player, and maybe 1-2 essential NPCs. Expand from there.
*   **Use Descriptive IDs:** While not mandatory, using readable IDs like `loc_sleepy_hollow_inn` or `npc_grumpy_old_wizard` makes debugging easier than using GUIDs everywhere initially.
*   **Validate JSON:** Use a JSON validator to ensure all your files are syntactically correct before trying to load the scenario.
*   **Test Thoroughly:** Load the scenario and test basic actions: looking around, checking inventory, moving to a connected location (if any), interacting with starting NPCs.
*   **Consistency is Key:** Ensure descriptions, NPC personalities, and location purposes align with the overall `world.json` `gameSetting` and scenario theme.

By following these guidelines and carefully structuring your JSON files, you can create robust and engaging starting scenarios for the RPG system.

## 7. How Scenarios Are Used by the Game

When a user starts a new game:

1. They select a starting scenario from the available options in `Data/startingScenarios/`.
2. The system creates a new game ID (a GUID).
3. The entire scenario folder structure is copied from `Data/startingScenarios/<scenarioId>/` to `Data/userData/<gameId>/`.
4. The player's ID in `player.json` and the currentPlayer field in `world.json` are updated to match the new game ID.
5. Any provided game preferences are applied.
6. The game begins with the player at the starting location specified in `gameSetting.json`.

This copying mechanism ensures that the original scenario remains unchanged and can be used to start multiple different games. Each user's game state evolves independently in their own game folder.

## 8. Empty File Templates

### 8.1 `conversationLog.json`

The conversationLog.json file should have the following structure:

```json
{
  "messages": []
}
```

This file will store the history of conversations during gameplay. It must start with an empty messages array.

### 8.2 `recentEvents.json`

The recentEvents.json file should initially be an empty array:

```json
[]
```

