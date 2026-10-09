using System.Text.Json.Serialization;
using Eurocar.Api.Features.Zones;
using Eurocar.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDir);

builder.Services.AddDbContext<AppDb>(o => o.UseSqlite(
    builder.Configuration.GetConnectionString("Db") ?? $"Data Source={Path.Combine(dataDir, "eurocar.db")}"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<ZoneService>();

var app = builder.Build();
app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
    DbBootstrap.Init(scope.ServiceProvider.GetRequiredService<AppDb>(), app.Configuration, TimeProvider.System);

var api = app.MapGroup("/api/v1");
api.MapZones();

app.Run();

public partial class Program;
