using Dapper;
using SemanticSearch.Api.Services;

DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddHttpClient<OllamaEmbeddingClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["OllamaBaseUrl"] ?? "http://localhost:11434");
    c.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddSingleton<SearchService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AjourFrontend", policy =>
        policy.WithOrigins(
                  "http://localhost:5173",
                  "https://localhost:5001",
                  "https://localhost:5002",
                  "https://develop.ajoursystem.tech")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("AjourFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();
