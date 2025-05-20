# Text-Based RPG with AI Dungeon Master

A dynamic text-based role-playing game powered by large language models that serve as an intelligent Dungeon Master, creating rich, responsive narratives based on player input.

## Overview

This project is a C# backend that processes natural language player inputs and leverages AI to generate narrative responses and content for an immersive text-based RPG experience. The system maintains persistent game state through JSON files and implements a modular architecture for flexibility and testability.

## Key Features

- **AI Dungeon Master**: Intelligent narrative generation that responds to player actions
- **Dynamic World**: Game world reacts to player choices with NPCs that remember interactions and locations that change
- **Time System**: In-game time progression with scheduled and random events
- **RPG Elements**: Quest system, inventory management, character progression via RPG Tags
- **Task Resolution**: Dice-based challenge system influenced by player skills and items
- **Turn-Based Combat**: Strategic combat encounters with enemies
- **Persistent State**: Automatic game state saving through JSON files

## System Requirements

- .NET 6.0+ runtime
- Configured AI provider API key (OpenAI, OpenRouter, etc.)
- Hangfire for background job processing

## Installation

1. Clone the repository
2. Configure your AI provider in `appsettings.json`
3. Set up your database for Hangfire job storage
4. Build and run the application

```powershell
# Example startup commands
dotnet restore
dotnet build
dotnet run
```

## Usage

Players interact with the game through natural language commands:

- Exploration: "look around", "go north", "enter the tavern"
- Interaction: "talk to the innkeeper", "examine the chest"
- Combat: "attack the goblin", "cast fireball"
- Inventory: "check inventory", "use health potion", "equip sword"

## Project Structure

```
/
|-- Controllers/            # API endpoints for frontend interaction
|-- Data/                   # Game data storage
|   |-- userData/           # Per-user game instances
|   |-- startingScenarios/  # Template data for new games
|-- Hubs/                   # SignalR hubs for real-time communication
|-- Models/                 # Data models for game entities
|-- PromptTemplates/        # Templates for AI interaction
|-- Services/
    |-- AIProviders/        # LLM API integration
    |-- PromptBuilders/     # Prompt construction for various scenarios
    |-- Processors/         # Process LLM responses
    |-- Storage/            # Persistence management
    |-- Triggers/           # Event trigger evaluation
```

## Technical Architecture

The system uses a modular service-oriented architecture with key components:

- **Job Queue**: Hangfire for background processing of all AI requests
- **Prompt Building**: Specialized builders that construct context-rich prompts
- **AI Integration**: Abstraction layer for different LLM providers
- **Response Processing**: JSON deserialization and data application
- **Persistence**: JSON-based storage with versioning
- **Real-time Updates**: SignalR for immediate game state notifications
