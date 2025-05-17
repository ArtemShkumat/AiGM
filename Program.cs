using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Dashboard;
using AiGMBackEnd.Services;
using AiGMBackEnd.Services.Processors;
using AiGMBackEnd.Services.Storage;
using AiGMBackEnd.Services.Triggers;
using AiGMBackEnd.Hubs;
using Microsoft.Extensions.DependencyInjection;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add CORS policy for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // Assuming frontend runs on port 3000
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Required for SignalR
        });
});

// Configure SignalR
builder.Services.AddSignalR();

// Configure Hangfire
builder.Services.AddHangfire(config =>
{
    config.UseMemoryStorage();
    config.UseRecommendedSerializerSettings();
});

// Add the Hangfire server
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 4; // Number of concurrent background jobs
});

// Register application services - Storage layer
builder.Services.AddSingleton<LoggingService>();
builder.Services.AddSingleton<IBaseStorageService, BaseStorageService>();
builder.Services.AddSingleton<IEntityStorageService, EntityStorageService>();
builder.Services.AddSingleton<IInventoryStorageService, InventoryStorageService>();
builder.Services.AddSingleton<ITemplateService, TemplateService>();
builder.Services.AddSingleton<IValidationService, ValidationService>();
builder.Services.AddSingleton<IWorldSyncService, WorldSyncService>();
builder.Services.AddSingleton<IGameScenarioService, GameScenarioService>();
builder.Services.AddSingleton<IConversationLogService, ConversationLogService>();
builder.Services.AddSingleton<IRecentEventsService, RecentEventsService>();
builder.Services.AddSingleton<IEnemyStatBlockService, EnemyStatBlockService>();
builder.Services.AddSingleton<ICombatStateService, CombatStateService>();
builder.Services.AddSingleton<IEventStorageService, EventStorageService>();
builder.Services.AddSingleton<StorageService>();

// Register event trigger evaluators
builder.Services.AddSingleton<ITriggerEvaluator, TimeTriggerEvaluator>();
builder.Services.AddSingleton<ITriggerEvaluator, LocationChangeTriggerEvaluator>();
builder.Services.AddSingleton<ITriggerEvaluator, FirstLocationEntryTriggerEvaluator>();

// Register notification service
builder.Services.AddSingleton<GameNotificationService>();

// Register LLM response deserializer
builder.Services.AddSingleton<ILlmResponseDeserializer, LlmResponseDeserializer>();

// Register AI services
builder.Services.AddSingleton<AiGMBackEnd.Services.AIProviders.AIProviderFactory>();
builder.Services.AddSingleton<AiService>();
builder.Services.AddSingleton<PromptService>();

// Register new StatusTrackingService
builder.Services.AddSingleton<IStatusTrackingService, StatusTrackingService>();

// Register entity processors
builder.Services.AddSingleton<ILocationProcessor, LocationProcessor>();
builder.Services.AddSingleton<INPCProcessor, NPCProcessor>();
builder.Services.AddSingleton<IQuestProcessor, QuestProcessor>();
builder.Services.AddSingleton<IPlayerProcessor, PlayerProcessor>();
builder.Services.AddSingleton<IUpdateProcessor, UpdateProcessor>();
builder.Services.AddSingleton<ISummarizePromptProcessor, SummarizePromptProcessor>();
builder.Services.AddSingleton<IEnemyStatBlockProcessor, EnemyStatBlockProcessor>();
builder.Services.AddSingleton<ICombatResponseProcessor, CombatResponseProcessor>();
builder.Services.AddSingleton<IEventProcessor, EventProcessor>();

// Register scenario processor using a factory for lazy loading to break circular dependencies
// This needs to be registered before services that depend on it
builder.Services.AddSingleton<IScenarioProcessor, ScenarioProcessor>();

// Register CombatPromptBuilder (if not already there - assuming it exists)
// If CombatPromptBuilder is not part of a broader registration, add it explicitly
// We need to check if PromptService handles this registration implicitly
// Assuming explicit registration is needed for now:
builder.Services.AddSingleton<AiGMBackEnd.Services.PromptBuilders.CombatPromptBuilder>();

// Register processing services (dependent on other services)
builder.Services.AddSingleton<ResponseProcessingService>();
builder.Services.AddSingleton<HangfireJobsService>();
builder.Services.AddSingleton<PresenterService>();

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Add Hangfire Dashboard in development
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new AllowAllConnectionsFilter() }
    });
}

// Enable CORS
app.UseCors();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Map SignalR hubs
app.MapHub<GameHub>("/hubs/game");

app.Run();

// Allow all connections to the Hangfire Dashboard in development
public class AllowAllConnectionsFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
