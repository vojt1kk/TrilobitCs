using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Middleware;
using TrilobitCS.Services;

var builder = WebApplication.CreateBuilder(args);

// Laravel: config/database.php — registrace DB spojení
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=trilobit.db"));

// Laravel: AppServiceProvider::register() — registrace služeb do DI kontejneru
builder.Services.AddHttpClient<SvitekScraper>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddControllers();

var app = builder.Build();

// Laravel: php artisan migrate — automatické vytvoření DB při startu
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Laravel: app/Exceptions/Handler.php — převod výjimek na HTTP odpovědi
app.UseMiddleware<ExceptionHandlerMiddleware>();

// Laravel: routes/api.php — Route::apiResource(...)
// Tady se jen řekne "použij controllery", samotné routes jsou v atributech na controllerech
app.MapControllers();

app.Run();
