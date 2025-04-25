# Storage Service Migration to SQL Database

This document outlines the step-by-step plan for migrating our file-based storage system to a SQL database.

## 1. Prerequisites & Setup

- Local SQL database (confirmed setup)
- Install required Entity Framework Core packages:
  ```
  Microsoft.EntityFrameworkCore.SqlServer (or appropriate provider)
  Microsoft.EntityFrameworkCore.Design
  Microsoft.EntityFrameworkCore.Tools
  ```
- Configure database connection string in `appsettings.json`:
  ```json
  {
    "ConnectionStrings": {
      "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AiGMDatabase;Trusted_Connection=True;"
    }
  }
  ```

## 2. Data Modeling & Schema Design

### Core Entity Tables
- **Games**: `GameId` (PK), `CreationTimestamp`, `Name`, `Description`
- **Players**: `Id` (PK), `GameId` (FK), `Name`, `Race`, `Class`, `Level`, `Stats`, `Inventory`, `QuestIds`
- **Worlds**: `Id` (PK), `GameId` (FK), `CurrentPlayer`, `CurrentLocation`, `Settings`, `Description`
- **Locations**: `Id` (PK), `GameId` (FK), `Name`, `Description`, `Type`,  `NPCIds`, `ItemIds`
- **Npcs**: `Id` (PK), `GameId` (FK), `Name`, `Description`, `CurrentLocationId` (FK), `VisibleToPlayer`, `Stats`
- **Quests**: `Id` (PK), `GameId` (FK), `Title`, `Description`, `Status`, `Steps`, `Rewards`, `RelatedNPCIds`
- **GameSettings**: `Id` (PK), `GameId` (FK), `Difficulty`, `Theme`, `ContentSettings`
- **GamePreferences**: `Id` (PK), `GameId` (FK), `AIModel`, `ResponseLength`, `StylePreferences`

### Conversation Log Tables (Normalized Approach)
- **ConversationLogs**: `Id` (PK), `GameId` (FK), `EntityType` (e.g., "World", "NPC"), `EntityId`
- **Messages**: `Id` (PK), `ConversationLogId` (FK), `Timestamp`, `Sender`, `Content`

### Starting Scenario Tables
- **StartingScenarios**: `ScenarioId` (PK), `Name`, `Description`
- **ScenarioData**: `Id` (PK), `ScenarioId` (FK), `EntityType`, `EntityId`, `JsonData`

### Prompt Template Tables
- **PromptTemplates**: `Id` (PK), `TemplatePath`, `TemplateType`, `Content`

## 3. Data Access Layer (DAL) Implementation

### Database Context
```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<Game> Games { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<World> Worlds { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Npc> Npcs { get; set; }
    public DbSet<Quest> Quests { get; set; }
    public DbSet<GameSetting> GameSettings { get; set; }
    public DbSet<GamePreference> GamePreferences { get; set; }
    public DbSet<ConversationLog> ConversationLogs { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<StartingScenario> StartingScenarios { get; set; }
    public DbSet<ScenarioData> ScenarioData { get; set; }
    public DbSet<PromptTemplate> PromptTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationships and constraints
        modelBuilder.Entity<Player>()
            .HasOne()
            .WithMany()
            .HasForeignKey(p => p.GameId);

        modelBuilder.Entity<World>()
            .HasOne()
            .WithMany()
            .HasForeignKey(w => w.GameId);

        // Additional relationships...

        // Configure one-to-many relationship between ConversationLogs and Messages
        modelBuilder.Entity<Message>()
            .HasOne(m => m.ConversationLog)
            .WithMany(cl => cl.Messages)
            .HasForeignKey(m => m.ConversationLogId);
    }
}
```

### Repository Interfaces
Create interfaces for each entity type:

```csharp
public interface IGameRepository
{
    Task<Game> GetByIdAsync(string gameId);
    Task<IEnumerable<Game>> GetAllAsync();
    Task<string> CreateAsync(Game game);
    Task UpdateAsync(Game game);
    Task DeleteAsync(string gameId);
}

public interface IPlayerRepository
{
    Task<Player> GetByIdAsync(string gameId);
    Task SaveAsync(Player player);
    // Additional methods...
}

// Similar interfaces for World, Location, Npc, Quest, etc.

public interface IConversationLogRepository
{
    Task<ConversationLog> GetByEntityAsync(string gameId, string entityType, string entityId);
    Task<Message> AddMessageAsync(string gameId, string entityType, string entityId, Message message);
    // Additional methods...
}

public interface IScenarioRepository
{
    Task<IEnumerable<StartingScenario>> GetAllScenariosAsync();
    Task<ScenarioData> GetScenarioDataAsync(string scenarioId, string entityType, string entityId);
    // Additional methods...
}

public interface ITemplateRepository
{
    Task<string> GetTemplateContentAsync(string templatePath);
    // Additional methods...
}
```

### Repository Implementations
Implement each repository interface:

```csharp
public class GameRepository : IGameRepository
{
    private readonly ApplicationDbContext _context;

    public GameRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Game> GetByIdAsync(string gameId)
    {
        return await _context.Games.FindAsync(gameId);
    }

    public async Task<IEnumerable<Game>> GetAllAsync()
    {
        return await _context.Games.ToListAsync();
    }

    public async Task<string> CreateAsync(Game game)
    {
        _context.Games.Add(game);
        await _context.SaveChangesAsync();
        return game.GameId;
    }

    public async Task UpdateAsync(Game game)
    {
        _context.Entry(game).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string gameId)
    {
        var game = await _context.Games.FindAsync(gameId);
        if (game != null)
        {
            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
        }
    }
}

// Similar implementations for other repositories...
```

## 4. Database Schema Creation

Use Entity Framework Core migrations:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## 5. Data Seeding

### Seeding Service

```csharp
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly string _promptTemplatesPath;
    private readonly string _startingScenariosPath;

    public DatabaseSeeder(
        ApplicationDbContext context, 
        ILogger<DatabaseSeeder> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        
        string rootDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
        _promptTemplatesPath = Path.Combine(rootDirectory, "PromptTemplates");
        _startingScenariosPath = Path.Combine(rootDirectory, "Data", "startingScenarios");
    }

    public async Task SeedAsync()
    {
        await SeedPromptTemplatesAsync();
        await SeedStartingScenariosAsync();
    }

    private async Task SeedPromptTemplatesAsync()
    {
        // Read all template files and insert into PromptTemplates table
        
        // Example:
        // foreach (var file in Directory.GetFiles(_promptTemplatesPath, "*.txt", SearchOption.AllDirectories))
        // {
        //     var content = await File.ReadAllTextAsync(file);
        //     var templatePath = file.Replace(_promptTemplatesPath, "").TrimStart('\\', '/');
        //     
        //     var template = new PromptTemplate
        //     {
        //         TemplatePath = templatePath,
        //         TemplateType = DetermineTemplateType(templatePath),
        //         Content = content
        //     };
        //     
        //     _context.PromptTemplates.Add(template);
        // }
        // await _context.SaveChangesAsync();
    }

    private async Task SeedStartingScenariosAsync()
    {
        // Read all scenario directories and insert into StartingScenarios and ScenarioData tables
        
        // Example:
        // foreach (var scenarioDir in Directory.GetDirectories(_startingScenariosPath))
        // {
        //     var scenarioId = Path.GetFileName(scenarioDir);
        //     var scenario = new StartingScenario { ScenarioId = scenarioId, Name = scenarioId };
        //     _context.StartingScenarios.Add(scenario);
        //     
        //     // Process each file in the scenario directory
        //     foreach (var file in Directory.GetFiles(scenarioDir, "*.json", SearchOption.AllDirectories))
        //     {
        //         var content = await File.ReadAllTextAsync(file);
        //         var relativePath = file.Replace(scenarioDir, "").TrimStart('\\', '/');
        //         var entityType = DetermineEntityType(relativePath);
        //         var entityId = Path.GetFileNameWithoutExtension(file);
        //         
        //         var scenarioData = new ScenarioData
        //         {
        //             ScenarioId = scenarioId,
        //             EntityType = entityType,
        //             EntityId = entityId,
        //             JsonData = content
        //         };
        //         
        //         _context.ScenarioData.Add(scenarioData);
        //     }
        // }
        // await _context.SaveChangesAsync();
    }

    private string DetermineTemplateType(string templatePath)
    {
        // Logic to determine template type based on path
        if (templatePath.StartsWith("DmPrompt")) return "DM";
        if (templatePath.StartsWith("NPCPrompt")) return "NPC";
        // etc.
        return "Other";
    }

    private string DetermineEntityType(string relativePath)
    {
        // Logic to determine entity type based on relative path
        if (relativePath.StartsWith("npcs")) return "NPC";
        if (relativePath.StartsWith("locations")) return "Location";
        if (relativePath.StartsWith("quests")) return "Quest";
        if (relativePath == "player.json") return "Player";
        if (relativePath == "world.json") return "World";
        // etc.
        return "Other";
    }
}
```

## 6. Refactor StorageService

### New StorageService Organization

```csharp
public class StorageService
{
    private readonly IGameRepository _gameRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IWorldRepository _worldRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly INpcRepository _npcRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IConversationLogRepository _conversationLogRepository;
    private readonly IScenarioRepository _scenarioRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly LoggingService _loggingService;

    public StorageService(
        IGameRepository gameRepository,
        IPlayerRepository playerRepository,
        IWorldRepository worldRepository,
        ILocationRepository locationRepository,
        INpcRepository npcRepository,
        IQuestRepository questRepository,
        IConversationLogRepository conversationLogRepository,
        IScenarioRepository scenarioRepository,
        ITemplateRepository templateRepository,
        LoggingService loggingService)
    {
        _gameRepository = gameRepository;
        _playerRepository = playerRepository;
        _worldRepository = worldRepository;
        _locationRepository = locationRepository;
        _npcRepository = npcRepository;
        _questRepository = questRepository;
        _conversationLogRepository = conversationLogRepository;
        _scenarioRepository = scenarioRepository;
        _templateRepository = templateRepository;
        _loggingService = loggingService;
    }

    // Entity-specific accessor methods (refactored)
    public async Task<Player> GetPlayerAsync(string gameId)
    {
        return await _playerRepository.GetByIdAsync(gameId);
    }

    public async Task<World> GetWorldAsync(string gameId)
    {
        return await _worldRepository.GetByIdAsync(gameId);
    }

    // Additional entity accessors...

    // Template access methods (refactored)
    public async Task<string> GetTemplateAsync(string templatePath)
    {
        var template = await _templateRepository.GetTemplateContentAsync(templatePath);
        if (string.IsNullOrEmpty(template))
        {
            _loggingService.LogWarning($"Template not found: {templatePath}. Using empty template.");
            return string.Empty;
        }
        return template;
    }

    // Specific template accessors...

    // Game creation methods (refactored)
    public async Task<string> CreateGameFromScenarioAsync(string scenarioId, GamePreferences preferences = null)
    {
        try
        {
            // 1. Create new game
            var game = new Game
            {
                GameId = Guid.NewGuid().ToString(),
                CreationTimestamp = DateTime.UtcNow
            };
            await _gameRepository.CreateAsync(game);

            // 2. Get scenario data for all entities
            var scenarioDataList = await _scenarioRepository.GetAllScenarioDataAsync(scenarioId);

            // 3. Process each entity in the scenario
            foreach (var scenarioData in scenarioDataList)
            {
                switch (scenarioData.EntityType)
                {
                    case "Player":
                        var player = JsonSerializer.Deserialize<Player>(scenarioData.JsonData);
                        player.Id = game.GameId; // Update ID
                        await _playerRepository.SaveAsync(player);
                        break;
                    case "World":
                        var world = JsonSerializer.Deserialize<World>(scenarioData.JsonData);
                        world.CurrentPlayer = game.GameId; // Update player reference
                        world.GameId = game.GameId; // Set game ID
                        await _worldRepository.SaveAsync(world);
                        break;
                    // Handle other entity types...
                }
            }

            // 4. Save game preferences if provided
            if (preferences != null)
            {
                preferences.GameId = game.GameId;
                await _gamePreferencesRepository.SaveAsync(preferences);
            }

            _loggingService.LogInfo($"Created new game with ID: {game.GameId} based on scenario: {scenarioId}");
            return game.GameId;
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error creating game from scenario: {ex.Message}");
            throw;
        }
    }

    // Conversation log methods (refactored)
    public async Task AddUserMessageAsync(string gameId, string content)
    {
        var message = new Message
        {
            Sender = "user",
            Content = content,
            Timestamp = DateTime.UtcNow
        };
        
        await _conversationLogRepository.AddMessageAsync(gameId, "World", "main", message);
    }

    public async Task AddDmMessageAsync(string gameId, string content)
    {
        var message = new Message
        {
            Sender = "dm",
            Content = content,
            Timestamp = DateTime.UtcNow
        };
        
        await _conversationLogRepository.AddMessageAsync(gameId, "World", "main", message);
    }

    // NPC conversation methods (refactored)
    public async Task AddUserMessageToNpcLogAsync(string gameId, string npcId, string content)
    {
        var message = new Message
        {
            Sender = "user",
            Content = content,
            Timestamp = DateTime.UtcNow
        };
        
        await _conversationLogRepository.AddMessageAsync(gameId, "NPC", npcId, message);
    }

    // Additional methods...
}
```

## 7. Dependency Injection Setup

In `Program.cs` or appropriate startup configuration:

```csharp
// Register DbContext
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
services.AddScoped<IGameRepository, GameRepository>();
services.AddScoped<IPlayerRepository, PlayerRepository>();
services.AddScoped<IWorldRepository, WorldRepository>();
services.AddScoped<ILocationRepository, LocationRepository>();
services.AddScoped<INpcRepository, NpcRepository>();
services.AddScoped<IQuestRepository, QuestRepository>();
services.AddScoped<IConversationLogRepository, ConversationLogRepository>();
services.AddScoped<IScenarioRepository, ScenarioRepository>();
services.AddScoped<ITemplateRepository, TemplateRepository>();

// Register services
services.AddScoped<StorageService>();
services.AddScoped<DatabaseSeeder>();

// Optional: Run seeder on startup
// var serviceProvider = services.BuildServiceProvider();
// var seeder = serviceProvider.GetRequiredService<DatabaseSeeder>();
// seeder.SeedAsync().Wait();
```

## 8. Model Class Updates

Update model classes to be compatible with EF Core:

```csharp
public class Player
{
    public string Id { get; set; }
    public string GameId { get; set; }
    // Other properties...
}

public class ConversationLog
{
    public int Id { get; set; }
    public string GameId { get; set; }
    public string EntityType { get; set; }
    public string EntityId { get; set; }
    public List<Message> Messages { get; set; } = new List<Message>();
}

public class Message
{
    public int Id { get; set; }
    public int ConversationLogId { get; set; }
    public ConversationLog ConversationLog { get; set; }
    public DateTime Timestamp { get; set; }
    public string Sender { get; set; }
    public string Content { get; set; }
}

// Update other model classes similarly
```

## 9. Implementation Strategy

1. **Phased Approach**:
   - Implement database schema and repositories
   - Create data seeding code
   - Update `StorageService` to use repositories
   - Test with a simple scenario

2. **Data Migration**:
   - Write a utility to copy existing file data to the database
   - Run migration for test data
   - Verify data integrity

3. **Dual Operation (Optional)**:
   - Support both file and database storage for a transition period
   - Implement feature flag to switch between implementations

## 10. Cleanup

Once confident in the database implementation:
- Remove file I/O code from `StorageService`
- Update all references to `userId` to `gameId` where appropriate
- Remove any unused dependencies
