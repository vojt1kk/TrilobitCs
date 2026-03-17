using MediatR;
using TrilobitCS.Features.EagleFeathers;
using TrilobitCS.Services;

namespace TrilobitCS.Console;

public class ScrapeEagleFeathersCommand
{
    private readonly SvitekScraper _scraper;
    private readonly IMediator _mediator;

    public ScrapeEagleFeathersCommand(SvitekScraper scraper, IMediator mediator)
    {
        _scraper = scraper;
        _mediator = mediator;
    }

    public async Task<int> ExecuteAsync(Action<string> log)
    {
        log("Starting eagle feathers scrape...");

        var totalSaved = 0;

        for (var light = 1; light <= 4; light++)
        {
            log($"Fetching light {light} sections...");
            var sections = await _scraper.FetchSectionsAsync(light);
            log($"  Found {sections.Count} sections.");

            foreach (var (sectionCode, sectionName) in sections)
            {
                log($"  Scraping {sectionName}...");
                var activities = await _scraper.FetchActivitiesAsync(light, sectionCode);

                foreach (var activity in activities)
                {
                    await _mediator.Send(new UpdateOrCreateEagleFeatherCommand(
                        (byte)light, sectionName, (short)activity.Number,
                        activity.Name,
                        _scraper.StripHtml(activity.Challenge),
                        _scraper.StripHtml(activity.GrandChallenge),
                        activity.SourceUrl));

                    totalSaved++;
                }

                log($"    Saved {activities.Count} activities.");
            }
        }

        log($"Done! Total activities saved: {totalSaved}");
        return totalSaved;
    }
}
