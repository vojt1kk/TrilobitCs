using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using TrilobitCS.Data;
using Xunit;

namespace TrilobitCS.Tests;

// Laravel ekvivalent: RefreshDatabase trait — každá testovací třída dostane čistou DB
public class TrilobitWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super-secret-test-key-that-is-at-least-32-chars!!",
                ["Jwt:Issuer"] = "trilobit-test",
                ["Jwt:Audience"] = "trilobit-test",
                ["Jwt:AccessTokenExpiresInMinutes"] = "15",
                ["Jwt:RefreshTokenExpiresInDays"] = "180",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Odstraní DbContext registrovaný v Program.cs (s dev connection stringem)
            // a nahradí ho testovacím PostgreSQL kontejnerem (Testcontainers)
            // Laravel ekvivalent: výměna DB spojení přes RefreshDatabase trait
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }
}
