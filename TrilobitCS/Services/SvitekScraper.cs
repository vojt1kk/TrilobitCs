using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;

namespace TrilobitCS.Services;

public class SvitekScraper
{
    private const string BaseUrl = "https://www.woodcraft.cz/files/web/svitek/index.php";

    private readonly HttpClient _httpClient;

    public SvitekScraper(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<(string Code, string Name)>> FetchSectionsAsync(int light)
    {
        var html = await FetchAsync(new Dictionary<string, string>
        {
            ["right"] = $"svetlo{light}",
            ["lan"] = "cs"
        });

        var doc = ParseHtml(html);
        var links = doc.DocumentNode.SelectNodes("//div[@id='rightitem']//a[contains(@href, 'section=')]");
        var sections = new List<(string Code, string Name)>();

        if (links == null) return sections;

        foreach (var link in links)
        {
            var text = link.InnerText.Trim();
            var href = link.GetAttributeValue("href", "");
            var query = HttpUtility.ParseQueryString(new Uri(BaseUrl + "?" + href.Split('?').LastOrDefault()).Query);
            var sectionCode = query["section"] ?? "";

            if (sectionCode != "")
            {
                sections.Add((sectionCode, text));
            }
        }

        return sections;
    }

    public async Task<List<ActivityData>> FetchActivitiesAsync(int light, string sectionCode)
    {
        var parameters = new Dictionary<string, string>
        {
            ["right"] = $"svetlo{light}",
            ["lan"] = "cs",
            ["section"] = sectionCode
        };

        var html = await FetchAsync(parameters);
        var doc = ParseHtml(html);
        var h2s = doc.DocumentNode.SelectNodes("//div[@id='rightitem']//h2[@data-name]");
        var activities = new List<ActivityData>();

        if (h2s == null) return activities;

        for (var index = 0; index < h2s.Count; index++)
        {
            var h2 = h2s[index];
            var activityId = h2.GetAttributeValue("id", "");
            var name = h2.GetAttributeValue("data-name", "");
            var number = index + 1;

            var contentHtml = ExtractActivityContent(doc, activityId);

            if (IsEmptyContent(contentHtml))
            {
                contentHtml = FindSharedContent(h2);
            }

            var (challenge, grandChallenge) = SplitChallenges(contentHtml);

            var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            var sourceUrl = $"{BaseUrl}?{queryString}#{activityId}";

            activities.Add(new ActivityData
            {
                Number = number,
                Name = name,
                Challenge = challenge,
                GrandChallenge = grandChallenge,
                SourceUrl = sourceUrl
            });
        }

        return activities;
    }

    private string ExtractActivityContent(HtmlDocument doc, string activityId)
    {
        var h2 = doc.DocumentNode.SelectSingleNode($"//h2[@id='{activityId}']");
        if (h2 == null) return "";

        var html = "";
        var sibling = h2.NextSibling;

        while (sibling != null)
        {
            if (sibling.Name == "h2" || sibling.Name == "h1")
                break;

            html += sibling.OuterHtml;
            sibling = sibling.NextSibling;
        }

        return CleanContent(html);
    }

    private string CleanContent(string html)
    {
        html = Regex.Replace(html, @"<div class=""OpNastroje"">.*?(?=<h[12]|$)", "", RegexOptions.Singleline);
        html = Regex.Replace(html, @"<div class=""OpPoznamka"">.*?</div>", "", RegexOptions.Singleline);
        html = Regex.Replace(html, @"<div class=""OpPojmy"">.*?(?=<div class=""OpNastroje""|<h[12]|$)", "", RegexOptions.Singleline);
        html = Regex.Replace(html, @"<div class=""OpML"">.*?</div>", "", RegexOptions.Singleline);

        return html.Trim();
    }

    private (string Challenge, string GrandChallenge) SplitChallenges(string html)
    {
        if (Regex.IsMatch(html, @"<ul class=""OpPodminky"">", RegexOptions.IgnoreCase))
        {
            return SplitFromList(html);
        }

        if (Regex.IsMatch(html, @"<table", RegexOptions.IgnoreCase))
        {
            return SplitFromTable(html);
        }

        return (html, html);
    }

    private (string Challenge, string GrandChallenge) SplitFromList(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<meta charset=\"utf-8\">" + html);

        var items = doc.DocumentNode.SelectNodes("//ul[@class='OpPodminky']/li");
        var challenge = "";
        var grandChallenge = "";

        if (items != null)
        {
            foreach (var item in items)
            {
                var spans = item.SelectNodes(".//span");
                if (spans == null || spans.Count == 0) continue;

                var lastSpan = spans[^1];
                var label = lastSpan.InnerText.Trim().ToUpper();
                var itemHtml = item.OuterHtml;

                if (label.Contains("VELK") || label == "V. ČIN")
                {
                    grandChallenge += itemHtml;
                }
                else
                {
                    challenge += itemHtml;
                }
            }
        }

        if (challenge != "")
            challenge = "<ul class=\"OpPodminky\">" + challenge + "</ul>";

        if (grandChallenge != "")
            grandChallenge = "<ul class=\"OpPodminky\">" + grandChallenge + "</ul>";

        var prefix = Regex.Replace(html, @"<ul class=""OpPodminky"">.*</ul>", "", RegexOptions.Singleline).Trim();

        if (prefix != "")
        {
            challenge = prefix + challenge;
            grandChallenge = prefix + grandChallenge;
        }

        return (challenge, grandChallenge);
    }

    private (string Challenge, string GrandChallenge) SplitFromTable(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<meta charset=\"utf-8\">" + html);

        var rows = doc.DocumentNode.SelectNodes("//table//tbody//tr");
        var challengeRows = new List<string>();
        var grandChallengeRows = new List<string>();

        if (rows != null)
        {
            foreach (var row in rows)
            {
                var cells = row.SelectNodes(".//td");
                if (cells == null || cells.Count == 0) continue;

                var lastCell = cells[^1];
                var label = lastCell.InnerText.Trim().ToUpper();

                if (label.Contains("V.") || label.Contains("VELK"))
                {
                    grandChallengeRows.Add(row.OuterHtml);
                }
                else
                {
                    challengeRows.Add(row.OuterHtml);
                }
            }
        }

        var theadNode = doc.DocumentNode.SelectSingleNode("//table//thead");
        var thead = theadNode?.OuterHtml ?? "";

        var tableNode = doc.DocumentNode.SelectSingleNode("//table");
        var tableAttrs = "";
        if (tableNode != null)
        {
            tableAttrs = string.Join("", tableNode.Attributes.Select(a => $" {a.Name}=\"{a.Value}\""));
        }

        var nonTableContent = Regex.Replace(html, @"<table.*?</table>", "", RegexOptions.Singleline).Trim();

        var challenge = $"{nonTableContent}<table{tableAttrs}>{thead}<tbody>{string.Join("", challengeRows)}</tbody></table>";
        var grandChallenge = $"{nonTableContent}<table{tableAttrs}>{thead}<tbody>{string.Join("", grandChallengeRows)}</tbody></table>";

        return (challenge, grandChallenge);
    }

    public string StripHtml(string html)
    {
        // Remove ČIN/VELKÝ ČIN labels
        html = Regex.Replace(html, @"<span>\s*(?:VELK[ÝY]\s+)?[ČC]IN\s*</span>", "", RegexOptions.None);
        html = Regex.Replace(html, @"(?:VELK[ÝY]\s+)?[ČC]IN\s*$", "", RegexOptions.Multiline);

        // Extract tables
        var tables = new List<string>();
        html = Regex.Replace(html, @"<table.*?</table>", m =>
        {
            var placeholder = $"{{{{TABLE_{tables.Count}}}}}";
            tables.Add(m.Value);
            return placeholder;
        }, RegexOptions.Singleline);

        // Strip HTML tags
        var text = Regex.Replace(html, @"<[^>]+>", "");
        text = WebUtility.HtmlDecode(text);

        // Normalize whitespace
        text = text.Replace("\u00A0", " ");
        text = Regex.Replace(text, @"\s+", " ");
        text = text.Trim();

        // Restore tables
        for (var i = 0; i < tables.Count; i++)
        {
            text = text.Replace($"{{{{TABLE_{i}}}}}", tables[i]);
        }

        return text;
    }

    private bool IsEmptyContent(string html)
    {
        var stripped = Regex.Replace(html.Trim(), @"<[^>]+>", "");
        return stripped == "";
    }

    private string FindSharedContent(HtmlNode h2)
    {
        var prev = h2.PreviousSibling;

        while (prev != null)
        {
            if (prev.NodeType == HtmlNodeType.Element)
            {
                var innerHtml = prev.OuterHtml;

                if (innerHtml.Contains("OpPodminky"))
                {
                    return CleanContent(innerHtml);
                }

                if (prev.Name == "h1")
                    break;
            }

            prev = prev.PreviousSibling;
        }

        return "";
    }

    private async Task<string> FetchAsync(Dictionary<string, string> parameters)
    {
        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        var url = $"{BaseUrl}?{queryString}";

        var response = await _httpClient.GetStringAsync(url);
        return response;
    }

    private HtmlDocument ParseHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }
}

public class ActivityData
{
    public int Number { get; set; }
    public string Name { get; set; } = "";
    public string Challenge { get; set; } = "";
    public string GrandChallenge { get; set; } = "";
    public string SourceUrl { get; set; } = "";
}
