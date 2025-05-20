# Event System

## Overview

This document outlines the design and implementation of the system that manages dynamic, triggerable events within the game. These events can be created by the LLM (acting as the DM) in response to gameplay or pre-defined as part of a starting scenario. The goal is to add persistence and reactivity to the game world beyond immediate player actions.

**Core Concepts:**

1.  **Event Entity:** An `Event` object represents a future occurrence with specific trigger conditions and context.
2.  **Triggers:** Events are activated based on defined conditions, such as game time passing or the player entering a specific location.
3.  **LLM Creation:** The DM can create new events dynamically using a new `EventCreationHook` within the `newEntities` array of its response.
4.  **Scenario Definition:** Starting scenarios can include pre-defined events that become active when a new game is created.
5.  **Evaluation:** Before generating a DM response, the system checks all active events against the current game state and context.
6.  **Injection:** If an event's trigger conditions are met, its summary is injected into the system prompt for the LLM, instructing it to incorporate the event's outcome into the narrative.
7.  **Persistence:** Events are stored persistently (as individual JSON files) and their status is tracked (`Active`, `Completed`, `Error`).
8.  **Prioritization:** The system limits the number of triggered events to 3 per response to avoid overwhelming the narrative, prioritizing them based on type and creation time.
9.  **Random Events:** In addition to pre-defined events, the system includes a chance-based random event system that can introduce spontaneous occurrences.

**Example Use Cases:**

*   **Time-Based:** An NPC is sent on a journey. The DM creates an event: `TriggerType = Time`, `TriggerValue = CurrentTime + 2 days`, `Summary = "NPC Arrives. Update location and act on message."`. When 2 days pass, the event triggers.
*   **Location-Based:** A scenario defines an event: `TriggerType = FirstLocationEntry`, `TriggerValue = "loc_market_square"`, `Summary = "Player enters market square for the first time, sees NPC argument."`. When the player first moves to `loc_market_square`, the event triggers.

## Implementation Details

**1. Core Models (`/Models/`)**

*   **`Event.cs`**:
    *   `string Id`
    *   `string Summary` (LLM directive)
    *   `EventType TriggerType` (Enum)
    *   `TriggerValue TriggerValue` (Type-safe base class with derived implementations)
    *   `Dictionary<string, object> Context` (Optional additional data)
    *   `EventStatus Status` (Enum: `Active`, `Completed`, `Error`)
    *   `DateTimeOffset CreationTime`
    *   `DateTimeOffset? CompletionTime`
*   **`EventType.cs` (Enum)**: `Time`, `LocationChange`, `FirstLocationEntry`, etc.
*   **`EventStatus.cs` (Enum)**: `Active`, `Completed`, `Error`.
*   **`TriggerValue.cs` (Abstract Base Class)**:
    *   Common properties/methods for all trigger value types
*   **Derived Trigger Value Classes**:
    *   `TimeTriggerValue.cs`: `DateTimeOffset TriggerTime`
    *   `LocationTriggerValue.cs`: `string LocationId`, `bool MustBeFirstVisit`
    *   Additional derived classes as needed for other trigger types
*   **`TriggerValueConverter.cs`**: A JsonConverter for proper serialization/deserialization
*   **`Hooks/EventCreationHook.cs`**: Implements `ICreationHook` mirroring `Event` properties (excluding `Id`, `Status`, `CompletionTime`).

**2. Trigger Evaluation Logic (`/Services/Triggers/`)**

*   **`ITriggerEvaluator.cs` (Interface)**:
    *   `EventType HandledTriggerType`
    *   `bool ShouldTrigger(Event gameEvent, TriggerContext context)`
*   **`TriggerContext.cs`**:
    *   `string UserId`
    *   `DateTimeOffset CurrentTime`
    *   `string CurrentLocationId`
    *   `string PreviousLocationId`
    *   `string UserInput`
    *   `World World`
    *   `Player Player`
*   **Concrete Implementations**: `TimeTriggerEvaluator.cs`, `LocationChangeTriggerEvaluator.cs`, etc.

**3. Storage (`/Services/Storage/`)**

*   **`IEventStorageService.cs`**: Defines methods for CRUD operations on events:
    *   `Task<List<Event>> GetActiveEventsAsync(string userId)`
    *   `Task<List<Event>> GetAllEventsAsync(string userId)`
    *   `Task<Event> GetEventAsync(string userId, string eventId)`
    *   `Task SaveEventAsync(string userId, Event gameEvent)`
    *   `Task UpdateEventStatusAsync(string userId, string eventId, EventStatus status)`
    *   `Task<bool> DeleteEventAsync(string userId, string eventId)`
    *   `Task<bool> UpdateEventAsync(string userId, string eventId, Action<Event> updateAction)`
    *   `Task PurgeOldCompletedEventsAsync(string userId, TimeSpan retentionPeriod)`
*   **`EventStorageService.cs`**: Implementation using `BaseStorageService`, storing events in `/Data/userData/<UserId>/events/{eventId}.json`.

**4. Event Processing in the DM Prompt Builder (`/Services/PromptBuilders/DMPromptBuilder.cs`)**

*   **Injection Dependencies**:
    *   `IEventStorageService`
    *   `IEnumerable<ITriggerEvaluator>`
*   **Event Checking**:
    *   `CheckForTriggeredEventsAsync` fetches active events and evaluates them against the current context.
    *   Uses the appropriate `ITriggerEvaluator` for each event's trigger type.
*   **Event Prioritization**:
    *   `PrioritizeTriggeredEvents` limits triggered events to 3 per response.
    *   Prioritizes by event type and creation time:
        1. Time-based events (older first)
        2. FirstLocationEntry events (older first)
        3. LocationChange events (older first)
*   **Prompt Injection**:
    *   Adds a "### Triggered Events ###" section in the system prompt.
    *   Lists each triggered event's summary.
    *   Instructs the LLM to incorporate these events into the narrative.
*   **Status Update**:
    *   Updates triggered events to `Completed` status after building the prompt.

**5. Random Event System**

*   **`AddRandomEvent` in DMPromptBuilder**:
    *   Checks if a cooldown period has passed since the last random event (default: 24 hours).
    *   If cooldown has passed, rolls a random number to determine if an event occurs (default: 10% chance).
    *   If triggered, updates `World.LastRandomEventTime` and adds random event directive to the prompt.
    *   Uses a template file (`random_event_directive.txt`) for random event instructions.

**6. Event Creation Processing (`/Services/Processors/`)**

*   **`EventProcessor.cs`**: Implements `IEntityProcessor<EventCreationHook>`:
    *   `ValidationResult ValidateEventCreationHook(EventCreationHook hook)`: Validates hook data.
    *   `Task ProcessAsync(List<EventCreationHook> hooks, string userId)`: Creates and saves new events.

**7. Integration with Game Scenarios**

*   Game scenarios can include pre-defined events that are loaded and activated when a new game is created.
*   Events defined in scenario templates are processed to set correct status and creation times before being saved.

**8. Handling of Previous Location ID**

*   The `PromptRequest` class includes a `PreviousLocationId` property that is passed to the `TriggerContext` for location-based event evaluation.
*   This enables detection of location changes and first-time entries.

## Future Considerations

*   **Event Retention**: Implementing a configurable retention period for completed events.
*   **UI Awareness**: Potentially adding UI elements to show active or upcoming events for debug purposes.
*   **Combat Integration**: Adding a flag to indicate if events can trigger during combat when combat system integration is needed.
*   **Additional Trigger Types**: Expanding the system with more types of triggers (e.g., inventory changes, quest status changes).
