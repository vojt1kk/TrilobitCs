using System.Net;
using System.Reflection;
using FluentAssertions;
using TrilobitCS.Services;
using Xunit;

namespace TrilobitCS.Tests.EagleFeathers;

public class SvitekScraperTests
{
    private static readonly SvitekScraper Scraper = new(new HttpClient());

    private static string LoadFixture(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Fixture '{fileName}' not found in assembly resources.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    // =====================
    // ParseSections
    // =====================

    [Fact]
    public void ParseSections_Light1_ReturnsAllSections()
    {
        var html = LoadFixture("svetlo1.html");

        var sections = Scraper.ParseSections(html);

        sections.Should().HaveCount(3);
        sections.Should().Contain(("1A", "Atletika - běhy"));
        sections.Should().Contain(("1B", "Atletika - chůze"));
        sections.Should().Contain(("1C", "Atletika - skoky"));
    }

    [Fact]
    public void ParseSections_ExtractsCodeAndNameCorrectly()
    {
        var html = LoadFixture("svetlo1.html");

        var sections = Scraper.ParseSections(html);

        var first = sections[0];
        first.Code.Should().Be("1A");
        first.Name.Should().Be("Atletika - běhy");
        first.Name.Should().NotStartWith("1A");
    }

    // =====================
    // ParseActivities — table format (1A)
    // =====================

    [Fact]
    public void ParseActivities_Section1A_ReturnsBothFeathers()
    {
        var html = LoadFixture("section_1A.html");

        var activities = Scraper.ParseActivities(1, html);

        activities.Should().HaveCount(2);
    }

    [Fact]
    public void ParseActivities_Section1A_ParsesNameAndNumber()
    {
        var html = LoadFixture("section_1A.html");

        var activities = Scraper.ParseActivities(1, html);

        activities[0].Number.Should().Be(1);
        activities[0].Name.Should().Be("Běh na 100 m");
        activities[1].Number.Should().Be(2);
        activities[1].Name.Should().Be("Běh na 1500 m");
    }

    [Fact]
    public void ParseActivities_Section1A_SetsPermalinkSourceUrl()
    {
        var html = LoadFixture("section_1A.html");

        var activities = Scraper.ParseActivities(1, html);

        activities[0].SourceUrl.Should().Be("https://orlipera.cz/1A1");
        activities[1].SourceUrl.Should().Be("https://orlipera.cz/1A2");
    }

    [Fact]
    public void ParseActivities_Section1A_SplitsTableChallengeAndGrandChallenge()
    {
        var html = LoadFixture("section_1A.html");

        var activities = Scraper.ParseActivities(1, html);
        var feather = activities[0];

        feather.Challenge.Should().NotBeNullOrWhiteSpace();
        feather.GrandChallenge.Should().NotBeNullOrWhiteSpace();
        feather.Challenge.Should().NotBe(feather.GrandChallenge);
        feather.Challenge.Should().Contain("16,1");
        feather.GrandChallenge.Should().Contain("15,1");
    }

    [Fact]
    public void ParseActivities_Section1A_StripsOpMLAndOpPoznamka()
    {
        var html = LoadFixture("section_1A.html");

        var activities = Scraper.ParseActivities(1, html);

        foreach (var activity in activities)
        {
            activity.Challenge.Should().NotContain("OpML");
            activity.Challenge.Should().NotContain("OpPoznamka");
            activity.GrandChallenge.Should().NotContain("OpML");
            activity.GrandChallenge.Should().NotContain("OpPoznamka");
        }
    }

    // =====================
    // ParseActivities — list format (2A)
    // =====================

    [Fact]
    public void ParseActivities_Section2A_ReturnsAllFeathers()
    {
        var html = LoadFixture("section_2A.html");

        var activities = Scraper.ParseActivities(2, html);

        activities.Should().HaveCount(3);
    }

    [Fact]
    public void ParseActivities_Section2A_SplitsListChallengeAndGrandChallenge()
    {
        var html = LoadFixture("section_2A.html");

        var activities = Scraper.ParseActivities(2, html);
        var feather = activities[0]; // 2A1

        feather.Challenge.Should().NotBe(feather.GrandChallenge);
        feather.Challenge.Should().Contain("30 minut");
        feather.GrandChallenge.Should().Contain("20. stolet");
    }

    [Fact]
    public void ParseActivities_Section2A_PreservesPrefixTextOnBothVariants()
    {
        var html = LoadFixture("section_2A.html");

        var activities = Scraper.ParseActivities(2, html);
        var feather2A2 = activities.First(a => a.Name == "Hvězdy a vesmírné objekty");

        feather2A2.Challenge.Should().Contain("Ukaž na obloze a pojmenuj");
        feather2A2.GrandChallenge.Should().Contain("Ukaž na obloze a pojmenuj");
    }

    [Fact]
    public void ParseActivities_Section2A_StripsOpMLFromContent()
    {
        var html = LoadFixture("section_2A.html");

        var activities = Scraper.ParseActivities(2, html);

        foreach (var activity in activities)
        {
            activity.Challenge.Should().NotContain("OpML");
            activity.Challenge.Should().NotContain("ML:");
        }
    }

    [Fact]
    public void ParseActivities_Section2A_StripsFootnoteDl()
    {
        var html = LoadFixture("section_2A.html");

        var activities = Scraper.ParseActivities(2, html);
        var feather = activities[0]; // 2A1 has <dl class="op-poznamky">

        feather.Challenge.Should().NotContain("op-poznamky");
        feather.Challenge.Should().NotContain("Prokaž, že znáš (umíš)");
    }

    // =====================
    // StripHtml
    // =====================

    [Fact]
    public void StripHtml_RemovesCinLabels()
    {
        var html = "<ul class=\"OpPodminky\"><li><span>Splň úkol</span><span>ČIN</span></li></ul>";

        var result = Scraper.StripHtml(html);

        result.Should().Contain("Splň úkol");
        result.Should().NotContain("ČIN");
    }

    [Fact]
    public void StripHtml_PreservesTableHtml()
    {
        var html = "<div>Úvod</div><table class=\"oramovani\"><tbody><tr><td>16,1</td><td>ČIN</td></tr></tbody></table>";

        var result = Scraper.StripHtml(html);

        result.Should().Contain("<table");
        result.Should().Contain("16,1");
    }

    [Fact]
    public void StripHtml_DecodesHtmlEntities()
    {
        var html = "<div>Vypr&aacute;věj o &uacute;spěchu</div>";

        var result = Scraper.StripHtml(html);

        result.Should().Contain("Vyprávěj o úspěchu");
    }
}
