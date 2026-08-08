namespace TrilobitCS.Services;

// Runs the demo data seed immediately on startup and then once every 24 hours. Only registered
// when DemoSeeding:Enabled is true (see Program.cs) — meant for a demo/staging deployment (e.g.
// against Supabase) that should always have something in it, not for production.
public class DemoDataSeederService(IServiceScopeFactory scopeFactory, ILogger<DemoDataSeederService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
                await seeder.SeedAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Demo data seeding failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
