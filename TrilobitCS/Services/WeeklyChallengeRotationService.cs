using MediatR;
using TrilobitCS.Features.Challenges;

namespace TrilobitCS.Services;

public class WeeklyChallengeRotationService(IServiceScopeFactory scopeFactory, ILogger<WeeklyChallengeRotationService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var id = await mediator.Send(new GenerateWeeklyChallengeCommand(DateTime.UtcNow), stoppingToken);

                if (id is not null)
                    logger.LogInformation("Generated weekly challenge {ChallengeId}", id);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Weekly challenge rotation failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
