using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Dashboard;
using AiGMBackEnd.Services;
using AiGMBackEnd.Services.Processors;

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

// Register application services
builder.Services.AddSingleton<LoggingService>();
builder.Services.AddSingleton<StorageService>();
builder.Services.AddSingleton<AiGMBackEnd.Services.AIProviders.AIProviderFactory>();
builder.Services.AddSingleton<AiService>();
builder.Services.AddSingleton<PromptService>();

// Register new StatusTrackingService
builder.Services.AddSingleton<IStatusTrackingService, StatusTrackingService>();

// Register processors and other services in the correct dependency order to avoid circular dependencies
builder.Services.AddSingleton<IUpdateProcessor, UpdateProcessor>();
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
