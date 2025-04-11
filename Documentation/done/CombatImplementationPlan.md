# Combat System Implementation Plan

This document outlines the steps required to integrate the narrative puzzle combat system (described in `CombatOverview.md`) into the existing application architecture.

## 1. Foundation & Models

*   **Update `PromptType` Enum:** Add new values:
    *   `Combat`
    *   `CreateEnemyStatBlock`
    *   `SummarizeCombat` (or similar, for post-combat summarization)
*   **Create `EnemyStatBlock.cs` Model (in `Models/`):**
    *   Based on `CombatOverview.md`, include fields like:
        *   `Id` (string, e.g., `enemy_goblin_01`)
        *   `Name` (string)
        *   `Level` (int, 1-10)
        *   `SuccessesRequired` (int, calculated or set)
        *   `Description` (string, visual identity)
        *   `Vulnerability` (string, narrative description of how to score successes)
        *   `BadStuff` (string, what happens on player defeat)
        *   `Tags` (List<string>, optional, for enemy abilities/resistances)
        *   `CurrentSuccesses` (int, runtime tracking, maybe belongs in `CombatState`?)
*   **Create `CombatState.cs` Model (in `Models/`):**
    *   Represents the state of an *active* combat instance.
    *   `CombatId` (string, unique identifier for this combat session)
    *   `UserId` (string)
    *   `EnemyStatBlockId` (string, references the enemy being fought)
    *   `CurrentEnemySuccesses` (int, tracks hits on the enemy)
    *   `PlayerConditions` (List<string>, tracking Minor/Moderate/Severe conditions)
    *   `CombatLog` (List<string>, accumulates narrative turns for summarization)
    *   `IsActive` (bool)
*   **Update LLM Response Models (e.g., `DmResponse`, `NpcResponse` in `Models/`):**
    *   Add an optional boolean field: `combatTriggered` (defaults to false).
    *   Add an optional field: `enemyToEngageId` (string, ID of the NPC/entity triggering combat, needed for on-the-fly stat block checks).

## 2. Enemy Stat Block Storage & Creation

*   **Storage (`StorageService`):**
    *   Add methods like:
        *   `SaveEnemyStatBlockAsync(string userId, EnemyStatBlock statBlock)`
        *   `LoadEnemyStatBlockAsync(string userId, string enemyId) -> EnemyStatBlock?`
        *   `CheckIfStatBlockExistsAsync(string userId, string enemyId) -> bool`
    *   Define storage location (e.g., `/Data/<UserId>/enemies/enemy_<id>.json`).
*   **Create Enemy Stat Block Prompt (`PromptType.CreateEnemyStatBlock`):**
    *   **Templates (`PromptTemplates/CreateEnemyStatBlock/`):** Create templates instructing the LLM to generate *only* the JSON for an `EnemyStatBlock` based on context (e.g., NPC description, requested difficulty, scenario).
    *   **Builder (`CreateEnemyStatBlockPromptBuilder`):** Implements `IPromptBuilder`. Gathers context (NPC data, location, desired difficulty/level if known) and uses the templates.
    *   **Processor (`EnemyStatBlockProcessor`? or logic within `ResponseProcessingService.HandleCreateResponseAsync`):**
        *   Handles the JSON response from the `CreateEnemyStatBlock` prompt.
        *   Deserializes the JSON into `EnemyStatBlock`.
        *   Validates the data (e.g., level within range).
        *   Calculates `SuccessesRequired` (Level / 2, rounded up).
        *   Assigns a unique ID if not provided.
        *   Calls `StorageService.SaveEnemyStatBlockAsync`.
*   **On-the-Fly Creation Trigger:**
    *   **Requirement:** When the player attacks an NPC/entity, we need to ensure a stat block exists *before* starting combat.
    *   **Logic:** In `ResponseProcessingService.HandleResponseAsync` (when `combatTriggered` is true and `enemyToEngageId` is present):
        1.  Check `StorageService.CheckIfStatBlockExistsAsync(userId, enemyToEngageId)`.
        2.  If **false**:
            *   Enqueue a *new background job* via `HangfireJobsService` to run `CreateEntityAsync` with `PromptType.CreateEnemyStatBlock` for the `enemyToEngageId`.
            *   **Crucially:** The system must *wait* for this creation job to complete before proceeding to initiate the actual combat flow. This might involve polling job status or using Hangfire continuations.
            *   **Alternative:** The initial `combatTriggered` response could perhaps *include* the basic stat block JSON directly if the LLM determines it's needed on the fly, simplifying this trigger but making DM/NPC prompts more complex. Evaluate trade-offs. (Let's initially plan for the separate creation job).
        3.  If **true** (or after successful on-the-fly creation): Proceed to Combat Initiation (Step 3/4).

## 3. Combat Triggering Mechanism

*   **LLM Responsibility:** DM and NPC prompts need instructions (in their System templates) to set `combatTriggered: true` and provide `enemyToEngageId` when combat should begin based on narrative events or player actions (e.g., player states "I attack the guard").
*   **Processing (`ResponseProcessingService.HandleResponseAsync`):**
    *   After deserializing DM/NPC response, check if `combatTriggered` is true.
    *   If true:
        *   Perform the "On-the-Fly Stat Block Check" (from Step 2).
        *   Assuming stat block exists/is created:
            *   Initiate Combat State (Step 4).
            *   Call `GameNotificationService.NotifyCombatStartedAsync(userId, combatState)` (Step 7).
            *   **Do not** return the original `userFacingText` from the triggering response immediately. The response to the user will come from the *first* combat turn.

## 4. Combat State Management

*   **Initiation:** When combat is triggered (after stat block check):
    *   Create a new `CombatState` object.
    *   Generate a unique `CombatId`.
    *   Set `UserId`, `EnemyStatBlockId`.
    *   Initialize `CurrentEnemySuccesses = 0`, `PlayerConditions = []`, `CombatLog = []`, `IsActive = true`.
    *   Save the initial state using `StorageService.SaveCombatStateAsync(userId, combatState)`. (Need this method). Define location (e.g., `/Data/<UserId>/active_combat.json` - assuming only one combat at a time per user).
*   **Persistence:** The `CombatState` needs to be loaded at the start of each combat turn processing and saved at the end.
*   **Storage (`StorageService`):**
    *   `SaveCombatStateAsync(string userId, CombatState state)`
    *   `LoadCombatStateAsync(string userId) -> CombatState?`
    *   `DeleteCombatStateAsync(string userId)`

## 5. Combat Prompt Flow (`PromptType.Combat`)

*   **Templates (`PromptTemplates/Combat/`):**
    *   **System Prompt:** Explain the combat rules (`CombatOverview.md`), the structure of `CombatState`, the enemy's stat block (especially `Vulnerability`), and the required JSON response format. Instruct the LLM to manage the turn flow (player action -> GM difficulty/modifiers -> player tags -> GM resolution -> enemy response -> player defense roll -> condition application -> repeat).
    *   **Response Format:** Define the JSON structure the LLM should return for *each combat turn*. This might include:
        *   `userFacingText` (narrative description of the turn's events)
        *   `updatedCombatState` (the modified `CombatState` object reflecting hits, conditions, log entries)
        *   `combatEnded` (boolean: true if enemy defeated or player defeated)
        *   `playerVictory` (boolean: true if player won)
*   **Builder (`CombatPromptBuilder`):**
    *   Loads the current `CombatState` from `StorageService`.
    *   Loads the relevant `EnemyStatBlock` from `StorageService`.
    *   Loads player data (`Player.json`).
    *   Constructs the prompt using the Combat templates, injecting the current `CombatState`, enemy data, and player data.
*   **Processing (`ResponseProcessingService.HandleCombatResponseAsync` - new method):**
    *   Receives the LLM response string and the `userId`.
    *   Deserializes the response into the defined combat turn JSON structure.
    *   Extracts `userFacingText`, `updatedCombatState`, `combatEnded`, `playerVictory`.
    *   **Crucially:** Validates the `updatedCombatState` received from the LLM (e.g., ensures conditions applied correctly based on rules, successes don't exceed required, etc.). The service enforces rules the LLM might miss.
    *   Appends the `userFacingText` to the `CombatLog` within the `updatedCombatState`.
    *   Saves the `updatedCombatState` using `StorageService.SaveCombatStateAsync`.
    *   Checks `combatEnded`:
        *   If **true**: Trigger Combat Resolution (Step 6).
        *   If **false**: Return the `userFacingText` for the current turn to the frontend.
*   **Hangfire (`HangfireJobsService`):**
    *   Needs a method like `ProcessCombatTurnAsync(userId, userInput)` which follows the standard pattern: Build Prompt -> Get Completion -> Handle Response (using `HandleCombatResponseAsync`).
    *   **API Endpoint:** A new, dedicated API endpoint (e.g., `/interaction/combat`) should be created in the backend. The UI will send combat action inputs to this endpoint. This endpoint will be responsible for creating the `PromptRequest` with `PromptType.Combat` and enqueuing the `ProcessCombatTurnAsync` job.

## 6. Combat Resolution & Summarization

*   **Detection:** `ResponseProcessingService.HandleCombatResponseAsync` detects `combatEnded == true`.
*   **Notification:** Call `GameNotificationService.NotifyCombatEndedAsync(userId, playerVictory)`.
*   **Cleanup:** Call `StorageService.DeleteCombatStateAsync(userId)` *after* summarization is complete.
*   **Summarization (`PromptType.SummarizeCombat`):**
    *   **Trigger:** After notifying the UI and *before* deleting the combat state, enqueue a new background job via `HangfireJobsService` for summarization.
    *   **Builder (`SummarizeCombatPromptBuilder`):**
        *   Loads the *final* `CombatState` (specifically the `CombatLog`).
        *   Creates a prompt asking the LLM to summarize the `CombatLog` into a concise narrative paragraph.
    *   **Templates (`PromptTemplates/SummarizeCombat/`):** Instruct the LLM on the desired summary format.
    *   **Processing:** The job receives the summary text from the LLM.
    *   **Logging:** Call `StorageService.AddDmMessageAsync` (or a similar method for logging significant events) to add the combat summary to the main game log/history visible to the DM prompt.

## 7. UI Integration (SignalR)

*   **`GameNotificationService`:** Add new methods:
    *   `NotifyCombatStartedAsync(string userId, CombatState initialState)`: Sends message "CombatStarted" with initial state (enemy name/desc, player conditions). Tells UI to switch to combat mode.
    *   `NotifyCombatEndedAsync(string userId, bool playerVictory)`: Sends message "CombatEnded" with outcome. Tells UI to switch back to normal mode and potentially display victory/defeat message.
    *   *(Optional)* `NotifyCombatTurnUpdateAsync(string userId, CombatState currentState)`: Could send updates after each turn if the UI needs to reflect changing conditions/enemy successes in real-time.
*   **`GameHub`:** Define the corresponding client-side methods (`CombatStarted`, `CombatEnded`, `CombatTurnUpdate`).

## Conclusion

This plan provides a roadmap for integrating combat. Key challenges include the robust handling of on-the-fly stat block creation and ensuring the `CombatState` is managed correctly across Hangfire jobs. Iterative development is recommended, perhaps starting with pre-defined stat blocks and the core combat loop before tackling on-the-fly generation and summarization.
