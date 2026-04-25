using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TrilobitCS.Auth;
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

// Laravel: Hash::make() a Hash::check()
builder.Services.AddScoped<BcryptPasswordHasher>();

// Laravel: php artisan make:auth → JWT service pro generování tokenů
builder.Services.AddScoped<JwtTokenService>();

// Laravel: config/auth.php — 'driver' => 'jwt'
// Čtení klíče uvnitř lambdy: konfigurace se aplikuje deferred (při prvním authenticate),
// takže WebApplicationFactory test override přes ConfigureAppConfiguration se projeví.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key not configured.");

        // Legacy handler mapuje JWT claim 'sub' → ClaimTypes.NameIdentifier, na který
        // se spoléhá UsersController. Nový JsonWebTokenHandler toto mapování nedělá.
        options.TokenHandlers.Clear();
        options.TokenHandlers.Add(new JwtSecurityTokenHandler());

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        {
            KeyId = JwtSigningKey.KeyId,
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = signingKey,
        };
    });

// Laravel: AppServiceProvider::register()
builder.Services.AddHttpClient<SvitekScraper>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddScoped<ScrapeEagleFeathersCommand>();
builder.Services.AddControllers();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

// Laravel ekvivalent: L5-Swagger / Scribe — generování API dokumentace
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "TrilobitCS API", Version = "v1" });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Přidá tlačítko "Authorize" pro JWT Bearer token
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Laravel: FormRequest::rules()
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Propustí FluentValidation pravidla do Swagger schématu (max length, required, ...)
builder.Services.AddFluentValidationRulesToSwagger();

// Validation errors → 422 místo výchozích 400
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
        new UnprocessableEntityObjectResult(context.ModelState);
});

var app = builder.Build();

// Laravel: php artisan migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Laravel: php artisan app:scrape-eagle-feathers
if (args.Contains("scrape"))
{
    using var scope = app.Services.CreateScope();
    var command = scope.ServiceProvider.GetRequiredService<ScrapeEagleFeathersCommand>();
    await command.ExecuteAsync(System.Console.WriteLine);
    return;
}

app.UseResponseCompression();

// Swagger UI 5.17.14 (embedded in Swashbuckle 6.x) only accepts openapi: "3.0.[0-3]"
// via regex /^3\.0\.([0123])(?:-rc[012])?$/. Microsoft.OpenApi 1.6+ generates "3.0.4",
// so we normalize the version string before it reaches the browser.
app.Use(async (ctx, next) =>
{
    if (!ctx.Request.Path.StartsWithSegments("/swagger/v1/swagger.json"))
    {
        await next();
        return;
    }
    var original = ctx.Response.Body;
    using var buffer = new MemoryStream();
    ctx.Response.Body = buffer;
    await next();
    buffer.Seek(0, SeekOrigin.Begin);
    var json = await new StreamReader(buffer).ReadToEndAsync();
    json = json.Replace("\"openapi\": \"3.0.4\"", "\"openapi\": \"3.0.1\"");
    ctx.Response.Body = original;
    ctx.Response.ContentLength = System.Text.Encoding.UTF8.GetByteCount(json);
    await ctx.Response.WriteAsync(json);
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TrilobitCS v1");
});

// Laravel: app/Exceptions/Handler.php
app.UseMiddleware<ExceptionHandlerMiddleware>();

// Laravel: \Illuminate\Auth\Middleware\Authenticate
app.UseAuthentication();
app.UseAuthorization();

// Laravel: routes/api.php
app.MapControllers();

app.Run();

// Zpřístupní třídu Program testovacímu projektu (WebApplicationFactory<Program>)
public partial class Program { }
