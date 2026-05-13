using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using TrilobitCS.Auth;
using TrilobitCS.Console;
using TrilobitCS.Data;
using TrilobitCS.Middleware;
using TrilobitCS.OpenApi;
using TrilobitCS.Services;

// Serilog musí být nakonfigurován dříve, než se spustí builder
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog — načte konfiguraci z appsettings.json (sekce Serilog)
    builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

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
            // se spoléhá ClaimsPrincipalExtensions.GetUserId(). Nový JsonWebTokenHandler toto
            // mapování nedělá — migrace plánována v samostatném kroku.
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

    // OpenAPI (Microsoft.AspNetCore.OpenApi) + Scalar UI
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    });

    // Laravel: FormRequest::rules()
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // Validation errors → 422 místo výchozích 400
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
            new UnprocessableEntityObjectResult(context.ModelState);
    });

    // CORS — allowed origins z appsettings.json
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    });

    // Rate limiting pro auth endpointy — per-IP, 5 requestů / 1 minuta (vypnuto v testovacím prostředí)
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy("auth", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
    }

    // Denní cleanup expired/revoked refresh tokenů
    builder.Services.AddHostedService<RefreshTokenCleanupService>();

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

    // Scalar UI: http://localhost:<port>/scalar/v1
    // OpenAPI JSON: http://localhost:<port>/openapi/v1.json
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("TrilobitCS API");
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    // Serilog request logging
    app.UseSerilogRequestLogging();

    app.UseCors("Frontend");

    if (!app.Environment.IsEnvironment("Testing"))
        app.UseRateLimiter();

    // Laravel: app/Exceptions/Handler.php
    app.UseMiddleware<ExceptionHandlerMiddleware>();

    // Laravel: \Illuminate\Auth\Middleware\Authenticate
    app.UseAuthentication();
    app.UseAuthorization();

    // Laravel: routes/api.php
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.CloseAndFlush();
}

// Zpřístupní třídu Program testovacímu projektu (WebApplicationFactory<Program>)
public partial class Program { }
