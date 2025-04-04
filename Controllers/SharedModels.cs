using System.Collections.Generic;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;

namespace AiGMBackEnd.Controllers
{
    // Shared model classes needed by multiple controllers

    public class NewGameRequest
    {
        public string ScenarioId { get; set; }
        public GamePreferences Preferences { get; set; }
    }

    public class ScenarioInfo
    {
        public string ScenarioId { get; set; }
        public string Name { get; set; }
        public GameSetting GameSetting { get; set; }
        public GamePreferences GamePreferences { get; set; }
    }

    public class UserInputRequest
    {
        public string GameId { get; set; }
        public string UserInput { get; set; }
        public PromptType PromptType { get; set; } = PromptType.DM; // Default to DM prompt
        public string? NpcId { get; set; } // Optional field for NPC interactions
    }

    public class UserInputResponse
    {
        public string Response { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    public class GameInfo
    {
        public string GameId { get; set; }
        public string Name { get; set; }
        public string PlayerName { get; set; }
        public string PlayerLocation { get; set; }
    }

    public class CreateCharacterRequest
    {
        public string GameId { get; set; }
        public string CharacterDescription { get; set; }
    }

    public class SceneInfo
    {
        public string GameId { get; set; }
        public string CurrentLocationId { get; set; }
        public string LocationName { get; set; }
        public string LocationDescription { get; set; }
        public List<StorageService.NpcInfo> VisibleNpcs { get; set; } = new List<StorageService.NpcInfo>();
    }
} 