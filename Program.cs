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

// Register application services
builder.Services.AddSingleton<AiGMBackEnd.Services.LoggingService>();
builder.Services.AddSingleton<AiGMBackEnd.Services.StorageService>();
builder.Services.AddSingleton<AiGMBackEnd.Services.AIProviders.AIProviderFactory>();
builder.Services.AddSingleton<AiGMBackEnd.Services.AiService>();
builder.Services.AddSingleton<AiGMBackEnd.Services.PromptService>();

// Register services with circular dependency
builder.Services.AddSingleton<AiGMBackEnd.Services.BackgroundJobService>();
builder.Services.AddSingleton<AiGMBackEnd.Services.ResponseProcessingService>();
builder.Services.AddSingleton<AiGMBackEnd.Services.PresenterService>();

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Resolve and configure services with circular dependencies
var backgroundJobService = app.Services.GetRequiredService<AiGMBackEnd.Services.BackgroundJobService>();
backgroundJobService.SetResponseProcessingServiceFactory(() => 
    app.Services.GetRequiredService<AiGMBackEnd.Services.ResponseProcessingService>());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS
app.UseCors();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
