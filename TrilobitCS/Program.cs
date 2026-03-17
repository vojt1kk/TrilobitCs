using Microsoft.EntityFrameworkCore;
using TrilobitCS.Console;
using TrilobitCS.Data;
using TrilobitCS.Middleware;
using TrilobitCS.Services;

var builder = WebApplication.CreateBuilder(args);

// Laravel: config/database.php + .env DB_CONNECTION
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Laravel: AppServiceProvider::register()
builder.Services.AddHttpClient<SvitekScraper>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddScoped<ScrapeEagleFeathersCommand>();
builder.Services.AddControllers();

var app = builder.Build();

// Laravel: php artisan migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Laravel: php artisan app:scrape-eagle-feathers
// Usage: dotnet run -- scrape
if (args.Contains("scrape"))
{
    using var scope = app.Services.CreateScope();
    var command = scope.ServiceProvider.GetRequiredService<ScrapeEagleFeathersCommand>();
    await command.ExecuteAsync(System.Console.WriteLine);
    return;
}

// Laravel: app/Exceptions/Handler.php
app.UseMiddleware<ExceptionHandlerMiddleware>();

// Laravel: routes/api.php — controllers are auto-discovered via [Route] attributes
app.MapControllers();

app.Run();
