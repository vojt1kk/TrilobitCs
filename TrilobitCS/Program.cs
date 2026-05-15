using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
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

// Serilog must be configured before the builder starts.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddScoped<BcryptPasswordHasher>();
    builder.Services.AddScoped<JwtTokenService>();

    // Key is read inside the lambda: configuration is applied deferred (on first authenticate),
    // so WebApplicationFactory test overrides via ConfigureAppConfiguration take effect.
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var jwtKey = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key not configured.");

            // Legacy handler maps JWT claim 'sub' → ClaimTypes.NameIdentifier, which
            // ClaimsPrincipalExtensions.GetUserId() relies on. JsonWebTokenHandler does not
            // do this mapping — migration to it is planned as a separate step.
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

    builder.Services.AddHttpClient<SvitekScraper>();
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
    builder.Services.AddScoped<ScrapeEagleFeathersCommand>();
    builder.Services.AddControllers(options =>
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    });

    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    });

    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // Validation errors → 422. Empty body → generic message instead of confusing "request field is required".
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var isEmptyBody = context.ModelState.ContainsKey("") || context.ModelState.ContainsKey("request");
            var hasOnlyBodyErrors = context.ModelState.Keys.All(k => k is "" or "request");

            if (isEmptyBody && hasOnlyBodyErrors)
                return new UnprocessableEntityObjectResult(new { message = "errors.empty_body" });

            var errors = context.ModelState
                .Where(e => e.Key is not "" and not "request")
                .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(x => x.ErrorMessage));

            return new UnprocessableEntityObjectResult(errors);
        };
    });

    // var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    // builder.Services.AddCors(options =>
    // {
    //     options.AddPolicy("Frontend", policy =>
    //         policy.WithOrigins(allowedOrigins)
    //               .AllowAnyHeader()
    //               .AllowAnyMethod());
    // });

    // Rate limiting for auth endpoints — per-IP, 5 requests per minute, disabled in testing.
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

    builder.Services.AddHostedService<RefreshTokenCleanupService>();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    if (args.Contains("scrape"))
    {
        using var scope = app.Services.CreateScope();
        var command = scope.ServiceProvider.GetRequiredService<ScrapeEagleFeathersCommand>();
        await command.ExecuteAsync(System.Console.WriteLine);
        return;
    }

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    });

    app.UseResponseCompression();

    // Scalar UI: http://localhost:<port>/scalar/v1  |  OpenAPI JSON: http://localhost:<port>/openapi/v1.json
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("TrilobitCS API");
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    app.UseSerilogRequestLogging();

    // app.UseCors("Frontend");

    if (!app.Environment.IsEnvironment("Testing"))
        app.UseRateLimiter();

    app.UseMiddleware<ExceptionHandlerMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
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

// Exposes Program to the test project (WebApplicationFactory<Program>).
public partial class Program { }
