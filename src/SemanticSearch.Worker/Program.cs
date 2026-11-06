using SemanticSearch.Worker;
using SemanticSearch.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient<OllamaEmbeddingClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["OllamaBaseUrl"] ?? "http://localhost:11434");
    c.Timeout = TimeSpan.FromSeconds(120);
});

builder.Services.AddSingleton<ContentExtractor>();
builder.Services.AddSingleton<TextChunker>();
builder.Services.AddSingleton<IndexingPipeline>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
