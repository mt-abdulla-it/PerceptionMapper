using PerceptionMapper.Application;
using PerceptionMapper.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var dbFile = Path.Combine(AppContext.BaseDirectory,
    builder.Configuration["Database:FileName"] ?? "perceptionmapper.db");

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(dbFile);
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Perception Mapper API", Version = "v1" });
});

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Perception Mapper v1"));

app.UseCors();
app.MapControllers();
app.MapGet("/", () => Results.Ok(new { status = "running", message = "Perception Mapper API (offline/local)." }));
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
