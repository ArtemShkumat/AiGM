# Importing Scenarios from Text

This document outlines the technical approach for generating a structured Scenario Template from a large text input and subsequently instantiating a new game world from that template.

## Feature Overview

1.  **Template Generation:** An administrator provides a large block of text (e.g., content from an adventure module PDF, story outline) and a name for the template. A background process analyzes the text using an LLM to extract key game elements (world settings, NPCs, locations, quests) and saves them as a structured JSON `ScenarioTemplate` file in a global storage location.
2.  **Game Instantiation:** A user selects a pre-generated `ScenarioTemplate` and provides a name for their new game. A background process reads the template and kicks off a series of jobs to create the corresponding game world, including the core game files (`world.json`, `gameSetting.json`) and the individual entities (NPCs, Locations, Quests) based on the information stored in the template stubs.

## Phase 1: Generating the Scenario Template

### 1.1. Goal

Analyze large text input and produce a structured, reusable `ScenarioTemplate` JSON file.

### 1.2. Models (`/Models/`)

*   **New File:** `Models/ScenarioTemplate.cs`
    *   Defines `public class ScenarioTemplate`
        *   `string TemplateId { get; set; }` (GUID, acts as primary key)
        *   `string TemplateName { get; set; }` (User-provided name)
        *   `GameSetting GameSettings { get; set; }` (Reuses the existing `Models.GameSetting` class)
        *   `List<NpcStub> Npcs { get; set; }`
        *   `List<LocationStub> Locations { get; set; }`
        *   `List<QuestStub> Quests { get; set; }`
    *   Includes definitions for helper classes:
        *   `public class NpcStub { string Name; string Description; }`
        *   `public class LocationStub { string Name; string Description; }`
        *   `public class QuestStub { string Name; string Description; }`

### 1.3. API Endpoint (`/Controllers/`)

*   **Modify:** `Controllers/GameAdminController.cs`
    *   Add new endpoint: `POST /api/admin/generate-template-from-text`
    *   Request Body Interface:
      ```json
      {
        "templateName": "string",
        "largeTextInput": "string"
      }
      ```
    *   Action:
        1.  Generate `templateId = Guid.NewGuid().ToString();`
        2.  Inject `IBackgroundJobClient`.
        3.  Enqueue job: `_backgroundJobClient.Enqueue<HangfireJobsService>(x => x.GenerateScenarioTemplateAsync(largeTextInput, templateId, templateName, null));`
        4.  Return `Accepted(new { TemplateId = templateId });`

### 1.4. Storage (`/Services/Storage/`)

*   **New Interface:** `Services/Storage/Interfaces/IScenarioTemplateStorageService.cs`
    *   `Task SaveTemplateAsync(ScenarioTemplate template);`
    *   `Task<ScenarioTemplate?> LoadTemplateAsync(string templateId);`
    *   `string GetTemplateFilePath(string templateId);`
*   **New Class:** `Services/Storage/ScenarioTemplateStorageService.cs`
    *   Implements `IScenarioTemplateStorageService`.
    *   Injects `IBaseStorageService` and `ILogger`.
    *   `GetTemplateFilePath` returns `Path.Combine("scenarioTemplates", templateId, "template.json")`.
    *   Uses `_baseStorageService.SaveAsync` and `_baseStorageService.LoadAsync<ScenarioTemplate>`.
    *   **Storage Path:** `/Data/scenarioTemplates/{templateId}/template.json` (relative to project root).

### 1.5. Prompt Templates (`/PromptTemplates/`)

*   **New Directory:** `/PromptTemplates/System/GenerateScenarioTemplate/`
*   **New Files** within the directory:
    *   `system.txt`: Core instructions for the LLM. Must detail the task (analyze text, extract entities matching the output structure), constraints (JSON only, adhere to structure), and define the purpose of each field within the structure.
    *   `outputStructure.json`: Defines the target JSON format. This file will contain a JSON object matching the structure of the `ScenarioTemplate` class, including the nested `GameSetting` object and examples of the stub list items.
    *   `exampleResponse.json`: Contains the complete, expected `ScenarioTemplate` JSON output for a hypothetical input, serving as a one-shot example for the LLM to follow.

### 1.6. Prompt Builder (`/Services/PromptBuilders/`)

*   **New Class:** `Services/PromptBuilders/GenerateScenarioTemplatePromptBuilder.cs`
    *   Implements `IPromptBuilder`.
    *   Injects `ITemplateService` and `ILogger`.
    *   Registered via DI keyed by `PromptType.GenerateScenarioTemplate`.
    *   `BuildPromptAsync(PromptRequest request)`:
        1.  Validates `request.PromptType == PromptType.GenerateScenarioTemplate`.
        2.  Validates `request.Context` (the `largeTextInput`) is not empty.
        3.  Uses `_templateService` to load the content of `system.txt`, `outputStructure.json`, and `exampleResponse.json` from `/PromptTemplates/System/GenerateScenarioTemplate/`.
        4.  Constructs the final prompt string using a `StringBuilder`, combining the loaded instructions, structure definition, example response, and the actual `largeTextInput` from `request.Context`.
        5.  Returns `Task.FromResult(new Prompt(promptString))`. (Async loading of templates might require `async Task`).

### 1.7. Hangfire Job (`/Services/`)

*   **Modify:** `Services/HangfireJobsService.cs`
    *   Add new public method:
      ```csharp
      [JobDisplayName("Generate Scenario Template: {2} (ID: {1})")]
      public async Task GenerateScenarioTemplateAsync(string largeTextInput, string templateId, string templateName, PerformContext context /* Injected */)
      {
          var promptRequest = new PromptRequest
          {
              PromptType = PromptType.GenerateScenarioTemplate,
              Context = largeTextInput
          };
          
          var prompt = await _promptService.BuildPromptAsync(promptRequest);
          var llmResponse = await _aiService.GetCompletionAsync(prompt);
          await _responseProcessingService.HandleScenarioTemplateResponseAsync(llmResponse, templateId, templateName);
      }
      ```
    *   Inject `IPromptService`, `IAiService`, `IResponseProcessingService`.

### 1.8. Response Processing (`/Services/`)

*   **Modify:** `Services/ResponseProcessingService.cs`
    *   Add new public method:
      ```csharp
      public async Task HandleScenarioTemplateResponseAsync(string llmResponse, string templateId, string templateName)
      {
          var template = System.Text.Json.JsonSerializer.Deserialize<ScenarioTemplate>(llmResponse);
          template.TemplateId = templateId;
          template.TemplateName = templateName;
          await _scenarioTemplateStorageService.SaveTemplateAsync(template);
      }
      ```
    *   Inject `IScenarioTemplateStorageService`, `ILogger`.

## Phase 2: Instantiating a Game from the Scenario Template

### 2.1. Goal

Create a new game instance for a user, populated with entities based on a selected `ScenarioTemplate`.

### 2.2. API Endpoint (`/Controllers/`)

*   **Modify:** `Controllers/GameAdminController.cs`
    *   Add new endpoint: `POST /api/admin/create-game-from-template`
    *   Request Body Interface:
      ```json
      {
        "userId": "string",
        "templateId": "string",
        "newGameName": "string"
      }
      ```
    *   Action:
        1.  Generate `newGameId = Guid.NewGuid().ToString();`.
        2.  Enqueue job: `_backgroundJobClient.Enqueue<HangfireJobsService>(x => x.InstantiateScenarioFromTemplateAsync(userId, templateId, newGameId, newGameName, null));`
        3.  Return `Ok(new { GameId = newGameId });`

### 2.3. Hangfire Job (`/Services/`)

*   **Modify:** `Services/HangfireJobsService.cs`
    *   Add new public method:
      ```csharp
      [JobDisplayName("Instantiate Scenario: {3} (GameID: {2}) from Template: {1})")]
      public async Task InstantiateScenarioFromTemplateAsync(string userId, string templateId, string newGameId, string newGameName, PerformContext context)
      {
          // Load template
          var template = await _scenarioTemplateStorageService.LoadTemplateAsync(templateId);
          
          // Create core game files
          // 1. Create user directory
          // 2. Save core files (GameSettings, World, etc.)
          
          // Process entity creation in batches
          var locationBatch = BatchJob.StartNew();
          foreach (var locationStub in template.Locations)
          {
              var locationId = Guid.NewGuid().ToString();
              locationBatch.Enqueue<HangfireJobsService>(x => 
                  x.CreateLocationAsync(userId, locationId, locationStub.Name, locationStub.Description, newGameId));
          }
          
          // Process NPCs after locations are created
          var npcBatch = BatchJob.ContinueBatchWith(locationBatch.BatchId);
          foreach (var npcStub in template.Npcs)
          {
              var npcId = Guid.NewGuid().ToString();
              npcBatch.Enqueue<HangfireJobsService>(x => 
                  x.CreateNpcAsync(userId, npcId, npcStub.Name, npcStub.Description, newGameId));
          }
          
          // Process Quests after NPCs are created
          var questBatch = BatchJob.ContinueBatchWith(npcBatch.BatchId);
          foreach (var questStub in template.Quests)
          {
              var questId = Guid.NewGuid().ToString();
              questBatch.Enqueue<HangfireJobsService>(x => 
                  x.CreateQuestAsync(userId, questId, questStub.Name, questStub.Description, newGameId));
          }
      }
      ```
    *   Inject `IScenarioTemplateStorageService`.
    *   Note: Simplified to focus on core job functionality. Implementation details for creating entities would leverage existing entity creation methods.


