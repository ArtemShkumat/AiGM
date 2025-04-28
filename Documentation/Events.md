# Event System

## Overview

This document outlines the design and implementation plan for a system to manage dynamic, triggerable events within the game. These events can be created by the LLM (acting as the DM) in response to gameplay or pre-defined as part of a starting scenario. The goal is to add persistence and reactivity to the game world beyond immediate player actions.

**Core Concepts:**

1.  **Event Entity:** An `Event` object represents a future occurrence with specific trigger conditions and context.
2.  **Triggers:** Events are activated based on defined conditions, such as game time passing or the player entering a specific location.
3.  **LLM Creation:** The DM can create new events dynamically using a new `EventCreationHook` within the `newEntities` array of its response.
4.  **Scenario Definition:** Starting scenarios can include pre-defined events that become active when a new game is created.
5.  **Evaluation:** Before generating a DM response, the system checks all active events against the current game state and context.
6.  **Injection:** If an event's trigger conditions are met, its summary is injected into the system prompt for the LLM, instructing it to incorporate the event's outcome into the narrative.
7.  **Persistence:** Events are stored persistently (likely as individual JSON files) and their status is tracked (`Active`, `Completed`, `Error`).

**Example Use Cases:**

*   **Time-Based:** An NPC is sent on a journey. The DM creates an event: `TriggerType = Time`, `TriggerValue = CurrentTime + 2 days`, `Summary = "NPC Arrives. Update location and act on message."`. When 2 days pass, the event triggers.
*   **Location-Based:** A scenario defines an event: `TriggerType = FirstLocationEntry`, `TriggerValue = "loc_market_square"`, `Summary = "Player enters market square for the first time, sees NPC argument."`. When the player first moves to `loc_market_square`, the event triggers.

## Implementation Plan

**1. Define Core Models (`/Models/`)**

*   **`Event.cs`**:
    *   `string Id`
    *   `string Summary` (LLM directive)
    *   `EventType TriggerType` (Enum)
    *   `object TriggerValue` (e.g., `DateTimeOffset`, `string LocationId`)
    *   `Dictionary<string, object> Context` (Optional additional data)
    *   `EventStatus Status` (Enum: `Active`, `Completed`, `Error`)
    *   `DateTimeOffset? CompletionTime`
*   **`EventType.cs` (Enum)**: `Time`, `LocationChange`, `FirstLocationEntry`, etc.
*   **`EventStatus.cs` (Enum)**: `Active`, `Completed`, `Error`.
*   **`Hooks/EventCreationHook.cs`**: Implement `ICreationHook` mirroring `Event` properties (excluding `Id`, `Status`, `CompletionTime`).
*   **`Triggers/TriggerContext.cs`**: Contains data for evaluation (`UserId`, `CurrentTime`, `CurrentLocationId`, `PreviousLocationId`, `UserInput`, `World`, `Player`).

**2. Implement Trigger Evaluation Logic (`/Services/Triggers/`)**

*   **`ITriggerEvaluator.cs` (Interface)**:
    *   `EventType HandledTriggerType`
    *   `bool ShouldTrigger(Event gameEvent, TriggerContext context)`
*   **Concrete Implementations**: `TimeTriggerEvaluator.cs`, `LocationChangeTriggerEvaluator.cs`, etc.
*   **Register** evaluators in DI.

**3. Implement Storage (`/Services/Storage/`)**

*   **`Interfaces/IEventStorageService.cs`**: Define methods for CRUD operations on events (`GetActiveEventsAsync`, `SaveEventAsync`, `UpdateEventStatusAsync`, etc.).
*   **`EventStorageService.cs`**: Implement using `BaseStorageService`, storing events in `/Data/userData/<UserId>/events/{eventId}.json`.
*   **Register** the service in DI.

**4. Implement Event Creation Processing (`/Services/Processors/`)**

*   **`EventProcessor.cs`**: Implement `IEntityProcessor<EventCreationHook>`. Validates hook, creates `Event`, assigns `Id` and `Active` status, saves via `IEventStorageService`.
*   **Register** the processor in DI.
*   **Update `ResponseProcessingService`**: Check for `EventCreationHook` in `newEntities` and delegate to `EventProcessor`.

**5. Update DM Prompt Builder (`/Services/PromptBuilders/DMPromptBuilder.cs`)**

*   Inject `IEventStorageService` and `IEnumerable<ITriggerEvaluator>`.
*   **Address `PreviousLocationId`:** Modify `PromptRequest` to include `PreviousLocationId`. Ensure upstream services fetch and pass this.
*   **Evaluation Logic:**
    *   Fetch active events.
    *   Create `TriggerContext`.
    *   Iterate events, find evaluator, call `ShouldTrigger`.
    *   Collect triggered event summaries.
*   **Prompt Injection:** If events triggered, add a `### Triggered Events ###` section to the system prompt with instructions for the LLM.
*   **Status Update:** After building prompt, update status of triggered events to `Completed` via `IEventStorageService`.

**6. Update Prompt Templates (`/PromptTemplates/DmPrompt/`)**

*   **`OutputStructure.json`**: Add `EventCreationHook` to `newEntities` schema.
*   **`System.txt`**: Add instructions for creating events and explain the `### Triggered Events ###` section.
*   **`ExampleResponses.txt`**: Add examples demonstrating event creation and triggered event handling.

**7. Update Scenario Loading (`/Services/Storage/GameScenarioService.cs`)**

*   Modify to check for `/Data/startingScenarios/<ScenarioName>/events/` or `events.json`.
*   Load event definitions, create `Event` objects with new `Id`s, set `Active`, and save using `IEventStorageService`. Handle relative time triggers.

**8. Refinements & Considerations**

*   **Passing Previous Location:** Ensure this mechanism is solid.
*   **Error Handling:** Define behavior for event processing/storage failures.
*   **Trigger Complexity:** Evaluate if `object TriggerValue` is sufficient long-term.
*   **UI:** Determine if any UI awareness is needed (admin/debug).
*   **Cleanup:** Plan for eventual cleanup of old events.
