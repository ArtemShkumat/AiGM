var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register application services
builder.Services.AddSingleton<AiGMBackEnd.Services.PresenterService>();
builder.Services.AddSingleton<AiGMBackEnd.Services.PromptService>();
builder.Services.AddSingleton<AiGMBackEnd.Services.AiService>();
builder.Services.AddSingleton<AiGMBackEnd.Services.ResponseProcessingService>();
builder.Services.AddSingleton<AiGMBackEnd.Services.StorageService>();
builder.Services.AddSingleton<AiGMBackEnd.Services.LoggingService>();
builder.Services.AddSingleton<AiGMBackEnd.Services.BackgroundJobService>();

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
