# World Building Rules & Guidelines

This document outlines the principles for structuring and generating the game world, focusing on locations and their relationships. The system is designed to be used with Large Language Models (LLMs) for content generation.

## Core Concepts

### 1. Unified Location Hierarchy

*   **Everything is a Location:** From vast realms down to individual rooms, every distinct place is represented by a `Location` object or one of its subclasses.
*   **Hierarchy via Parent:** Locations are organized hierarchically using the `ParentLocationId` property. Each location (except top-level ones like Realms or Planes) points to its containing location.
    *   *Example:* Room -> Floor -> Building -> District -> Settlement -> Region -> Country -> Continent -> World -> Realm/Plane.
*   **Implicit Adjacency:** Locations sharing the same `ParentLocationId` are considered adjacent or nearby within that parent context. Explicit connection lists (`ConnectedLocations`) are not used for general adjacency.
    *   *Example:* Two buildings with the same `District` as their parent are in the same district. A `Settlement` and `Wilds` with the same `Region` parent are neighboring areas within that region.
*   **Location Types:** A `LocationType` string property on the base `Location` class differentiates the kind of place (e.g., "Realm", "Region", "Settlement", "Building", "Room", "Wilderness", "Dungeon", "Floor", "District").

### 2. ID Management & Generation Flow

*   **Application Responsibility:** Unique IDs for all `Location` objects are **generated and managed solely by the backend application code**, *not* the LLM. This ensures consistency and uniqueness.
*   **LLM Role (Names, Not IDs):**
    *   When the LLM describes a location with nested parts (e.g., a Building with Floors/Rooms, a Settlement with Districts), it uses descriptive **names** for these parts in its response JSON (e.g., "Ground Floor", "Kitchen", "Market District").
    *   The LLM is explicitly prompted *not* to invent IDs for these nested structures.
*   **Application Role (ID Generation & Linking):**
    *   When requesting the creation of a *new top-level or standalone location* (e.g., a Settlement in a Region, a Building in a District), the application first generates a unique ID for this location.
    *   This application-generated ID (along with the `ParentLocationId`) is then provided to the LLM in the creation prompt.
    *   The `LocationProcessor` service parses the LLM's response JSON (which includes the application-provided ID for the main location and *names* for nested parts).
    *   For each named nested part (Floor, Room, District, etc.), the `LocationProcessor`:
        1.  Calls `GenerateNestedLocationId` (using the parent's ID, the type, and the sanitized name) to create a new, unique, hierarchical ID (e.g., `loc_bldg_tavern_floor_1_room_kitchen`).
        2.  Creates a `GenericLocation` object to represent this nested part.
        3.  Assigns the newly generated ID and the correct `ParentLocationId` to the `GenericLocation` object.
        4.  Saves the `GenericLocation` object via the `StorageService`.
        5.  Adds the newly generated ID to the appropriate list on the *parent* object (e.g., adds a Floor ID to `Building.FloorIds`, a District ID to `Settlement.DistrictIds`).
    *   Finally, the `LocationProcessor` saves the main parent object (e.g., the Building) with its updated list(s) of nested IDs.

*   **Example Flow: Creating an Inn**
    1.  **Player Input:** Player asks the DM, "Is there an inn here?"
    2.  **DM Response & Hook:** The LLM (as DM) responds narratively ("You see a sign for the 'Sleepy Hollow Inn'...") and includes a `CreationHook` in its JSON response (`newEntities` list) suggesting the creation of a `Building` named "Sleepy Hollow Inn".
    3.  **Application Generates Main ID:** The `ResponseProcessingService` detects the hook. Application logic generates a unique ID for the inn, e.g., `loc_bldg_sleepy_hollow_inn`.
    4.  **Creation Job Enqueued:** A background job is queued to create the location, passing the `userId`, the generated `Id` (`loc_bldg_sleepy_hollow_inn`), `entityType` ("Building"), the `parentLocationId` (the current town's ID), and the name.
    5.  **LLM Creates Content (using provided ID):** The `CreateLocationPromptBuilder` constructs a prompt telling the LLM to detail a `Building` with the ID `loc_bldg_sleepy_hollow_inn`. The LLM returns JSON describing the Inn, using this ID at the top level, but using *names* like "Ground Floor" and "Common Room" internally.
    6.  **Processor Generates Nested IDs:** The `LocationProcessor` receives this JSON. It saves the main `Building` object (`loc_bldg_sleepy_hollow_inn`). It then processes the nested parts:
        *   Sees "Ground Floor", generates ID `loc_bldg_sleepy_hollow_inn_floor_ground_floor`, saves the `GenericLocation` for the floor, and adds this ID to the Building's `FloorIds`.
        *   Sees "Common Room" (within Ground Floor), generates ID `loc_bldg_sleepy_hollow_inn_floor_ground_floor_room_common_room`, saves the `GenericLocation` for the room (parent = floor ID), etc.

### 3. NPC Location Tracking

*   **Single Source of Truth:** An NPC's location is tracked *only* via the `Npc.CurrentLocationId` property. This ID points to the specific `Location` object (be it a Room, Building, District, Wilds area, etc.) where the NPC currently is.
*   **No NPC Lists in Locations:** `Location` objects *do not* contain lists of specific NPCs currently present.

### 4. Typical Occupants

*   **Guidance for DM:** The `Location` object has a `TypicalOccupants` string property. This is a free-form description for the LLM acting as the Dungeon Master (DM) or game engine. It suggests the *types* of NPCs commonly found there (e.g., "Market stalls run by local artisans", "Rowdy patrons and a tired barkeep", "Goblins guarding the entrance"). The DM uses this guidance when populating the location dynamically during gameplay.

## Scenario Creation Workflow (General)

1.  **Top-Down:** Start defining the larger containers (e.g., Region).
2.  **Generate Major Locations:** Request Settlements, Wilds, Delves within the Region, providing the Region's ID as the `ParentLocationId`. The application generates unique IDs for these new locations.
3.  **Detail Settlements:** Request Districts within a Settlement, providing the Settlement's ID.
4.  **Detail Districts/Wilds:** Request Buildings within Districts, or Points of Interest/Sub-regions within Wilds, providing the parent's ID.
5.  **Detail Buildings/Delves:** Request Floors/Rooms for Buildings, or DelveRooms for Delves. The LLM uses names for these internal parts. The application processes the response to create unique IDs and link them.

## LLM Prompting Considerations

*   Prompts must clearly instruct the LLM to adhere to the ID naming convention (use names for nested parts).
*   Prompts must provide the necessary context (`ParentLocationId`, the `Id` for the main location being generated).
*   Prompts should ask for the `TypicalOccupants` description.
*   Prompts should *not* ask the LLM to populate specific NPC lists within the location structure.
