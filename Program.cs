using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Dashboard;
using AiGMBackEnd.Services;
using AiGMBackEnd.Services.Processors;
using AiGMBackEnd.Services.Storage;

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
                  .AllowAnyMethod();
        });
});

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
builder.Services.AddSingleton<ITemplateService, TemplateService>();
builder.Services.AddSingleton<IValidationService, ValidationService>();
builder.Services.AddSingleton<IWorldSyncService, WorldSyncService>();
builder.Services.AddSingleton<IGameScenarioService, GameScenarioService>();
builder.Services.AddSingleton<IConversationLogService, ConversationLogService>();
builder.Services.AddSingleton<IRecentEventsService, RecentEventsService>();
builder.Services.AddSingleton<StorageService>();

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

app.Run();

// Allow all connections to the Hangfire Dashboard in development
public class AllowAllConnectionsFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
