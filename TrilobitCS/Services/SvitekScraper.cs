using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;

namespace TrilobitCS.Services;

public class SvitekScraper
{
    private const string BaseUrl = "https://orlipera.cz";

    private readonly HttpClient _httpClient;

    public SvitekScraper(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<(string Code, string Name)>> FetchSectionsAsync(int light)
    {
        var html = await FetchAsync($"/svitek_{light}svetlo");
        return ParseSections(html);
    }

    public async Task<List<ActivityData>> FetchActivitiesAsync(int light, string sectionCode)
    {
        var html = await FetchAsync($"/svitek_{light}svetlo/{sectionCode}");
        return ParseActivities(light, html);
    }

    public List<(string Code, string Name)> ParseSections(string html)
    {
        var doc = ParseHtml(html);
        var links = doc.DocumentNode.SelectNodes("//div[contains(@class,'op-sekce-list')]//a[contains(@href,'/svitek_')]");
        var sections = new List<(string Code, string Name)>();

        if (links == null) return sections;

        foreach (var link in links)
        {
            var strongNode = link.SelectSingleNode(".//strong");
            if (strongNode == null) continue;

            var code = WebUtility.HtmlDecode(strongNode.InnerText).Trim();
            var fullText = WebUtility.HtmlDecode(link.InnerText).Trim();
            var name = fullText.StartsWith(code) ? fullText[code.Length..].Trim() : fullText;

            if (code != "")
                sections.Add((code, name));
        }

        return sections;
    }

    public List<ActivityData> ParseActivities(int light, string html)
    {
        var doc = ParseHtml(html);
        var featherDivs = doc.DocumentNode.SelectNodes("//div[contains(@class,'op-list-prehled')]/div[contains(@class,'mt-4')]");
        var activities = new List<ActivityData>();

        if (featherDivs == null) return activities;

        foreach (var div in featherDivs)
        {
            var h2Link = div.SelectSingleNode(".//div[contains(@class,'mb-2')]//h2//a");
            if (h2Link == null) continue;

            var headingText = WebUtility.HtmlDecode(h2Link.InnerText).Trim();

            // Parse "1A1 Běh na 100 m" → sectionCode="1A", number=1, name="Běh na 100 m"
            var match = Regex.Match(headingText, @"^(\d+[A-Z]+)(\d+)\s+(.+)$");
            if (!match.Success) continue;

            var sectionCode = match.Groups[1].Value;
            var number = int.Parse(match.Groups[2].Value);
            var name = match.Groups[3].Value.Trim();
            var featherCode = $"{sectionCode}{number}";

            var contentHtml = ExtractContent(div);
            var (challenge, grandChallenge) = SplitChallenges(contentHtml);

            activities.Add(new ActivityData
            {
                Number = number,
                Name = name,
                Challenge = challenge,
                GrandChallenge = grandChallenge,
                SourceUrl = $"{BaseUrl}/{featherCode}"
            });
        }

        return activities;
    }

    private string ExtractContent(HtmlNode featherDiv)
    {
        var html = "";

        foreach (var child in featherDiv.ChildNodes)
        {
            if (child.NodeType != HtmlNodeType.Element) continue;

            var classes = child.GetAttributeValue("class", "");

            // Skip: toolbar, heading wrapper, footnote note, ML section, footnote definitions
            if (classes.Contains("op-list-tool")) continue;
            if (classes.Contains("mb-2") && child.SelectSingleNode(".//h2") != null) continue;
            if (classes.Contains("OpPoznamka")) continue;
            if (classes.Contains("OpML")) continue;
            if (child.Name == "dl" && classes.Contains("op-poznamky")) continue;

            html += child.OuterHtml;
        }

        return html.Trim();
    }

    private (string Challenge, string GrandChallenge) SplitChallenges(string html)
    {
        if (Regex.IsMatch(html, @"<ul[^>]*class=""OpPodminky""", RegexOptions.IgnoreCase))
            return SplitFromList(html);

        if (Regex.IsMatch(html, @"<table", RegexOptions.IgnoreCase))
            return SplitFromTable(html);

        return (html, html);
    }

    private (string Challenge, string GrandChallenge) SplitFromList(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var listNode = doc.DocumentNode.SelectSingleNode("//ul[contains(@class,'OpPodminky')]");
        var items = listNode?.SelectNodes("./li");
        var challengeItems = new List<string>();
        var grandChallengeItems = new List<string>();

        if (items != null)
        {
            foreach (var item in items)
            {
                var spans = item.SelectNodes(".//span");
                if (spans == null || spans.Count == 0) continue;

                var lastSpan = spans[^1];
                var label = lastSpan.InnerText.Trim().ToUpper();

                if (label.Contains("VELK") || label == "V. ČIN" || label == "V. CIN")
                    grandChallengeItems.Add(item.OuterHtml);
                else
                    challengeItems.Add(item.OuterHtml);
            }
        }

        var listClass = listNode?.GetAttributeValue("class", "OpPodminky") ?? "OpPodminky";

        var challengeList = challengeItems.Count > 0
            ? $"<ul class=\"{listClass}\">{string.Join("", challengeItems)}</ul>"
            : "";

        var grandChallengeList = grandChallengeItems.Count > 0
            ? $"<ul class=\"{listClass}\">{string.Join("", grandChallengeItems)}</ul>"
            : "";

        // Preserve content before <ul> (shared prefix text)
        var prefix = Regex.Replace(html, @"<ul[^>]*class=""OpPodminky""[^>]*>.*?</ul>", "", RegexOptions.Singleline).Trim();

        return (prefix + challengeList, prefix + grandChallengeList);
    }

    private (string Challenge, string GrandChallenge) SplitFromTable(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

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
                    grandChallengeRows.Add(row.OuterHtml);
                else
                    challengeRows.Add(row.OuterHtml);
            }
        }

        var theadNode = doc.DocumentNode.SelectSingleNode("//table//thead");
        var thead = theadNode != null ? CleanTableSection(theadNode) : "";

        var tableNode = doc.DocumentNode.SelectSingleNode("//table");
        var tableClass = tableNode?.GetAttributeValue("class", "") ?? "";
        var tableTag = tableClass != "" ? $"<table class=\"{tableClass}\">" : "<table>";

        var nonTableContent = Regex.Replace(html, @"<table.*?</table>", "", RegexOptions.Singleline).Trim();

        var challenge = nonTableContent + tableTag + thead
            + "<tbody>" + CleanRows(challengeRows) + "</tbody></table>";
        var grandChallenge = nonTableContent + tableTag + thead
            + "<tbody>" + CleanRows(grandChallengeRows) + "</tbody></table>";

        return (challenge, grandChallenge);
    }

    // Rebuild thead/tfoot HTML with cleaned cells (no div wrappers, no inline styles)
    // Drops the last column in each row if it's blank (it's the ČIN label header)
    private static string CleanTableSection(HtmlNode sectionNode)
    {
        var tag = sectionNode.Name; // "thead" or "tfoot"
        var rows = sectionNode.SelectNodes(".//tr");
        if (rows == null) return "";

        var sb = new System.Text.StringBuilder();
        sb.Append($"<{tag}>");
        foreach (var row in rows)
        {
            var cells = row.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Element).ToList();

            // Drop trailing empty header cell (ČIN label column header)
            if (cells.Count > 0 && string.IsNullOrWhiteSpace(WebUtility.HtmlDecode(cells[^1].InnerText).Trim()))
                cells = cells[..^1];

            sb.Append("<tr>");
            foreach (var cell in cells)
            {
                var cellTag = cell.Name;
                var colspan = cell.GetAttributeValue("colspan", 0);
                var rowspan = cell.GetAttributeValue("rowspan", 0);

                var attrs = "";
                if (colspan > 1) attrs += $" colspan=\"{colspan}\"";
                if (rowspan > 1) attrs += $" rowspan=\"{rowspan}\"";

                var content = WebUtility.HtmlDecode(cell.InnerText).Trim();
                sb.Append($"<{cellTag}{attrs}>{content}</{cellTag}>");
            }
            sb.Append("</tr>");
        }
        sb.Append($"</{tag}>");
        return sb.ToString();
    }

    // Clean data rows: unwrap div wrappers, decode entities, drop the ČIN label cell
    private static string CleanRows(List<string> rowHtmlList)
    {
        if (rowHtmlList.Count == 0) return "";

        var doc = new HtmlDocument();
        doc.LoadHtml("<table><tbody>" + string.Join("", rowHtmlList) + "</tbody></table>");

        var sb = new System.Text.StringBuilder();
        foreach (var row in doc.DocumentNode.SelectNodes("//tr") ?? Enumerable.Empty<HtmlNode>())
        {
            var cells = row.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Element).ToList();

            // Drop the last cell if it's a ČIN/V. ČIN label (redundant after split)
            if (cells.Count > 0)
            {
                var lastText = WebUtility.HtmlDecode(cells[^1].InnerText).Trim().ToUpper();
                if (lastText.Contains("ČIN") || lastText.Contains("CIN"))
                    cells = cells[..^1];
            }

            sb.Append("<tr>");
            foreach (var cell in cells)
            {
                var cellTag = cell.Name;
                var colspan = cell.GetAttributeValue("colspan", 0);
                var attrs = colspan > 1 ? $" colspan=\"{colspan}\"" : "";
                var content = WebUtility.HtmlDecode(cell.InnerText).Trim();
                sb.Append($"<{cellTag}{attrs}>{content}</{cellTag}>");
            }
            sb.Append("</tr>");
        }
        return sb.ToString();
    }

    public string StripHtml(string html)
    {
        // Remove ČIN/VELKÝ ČIN labels (Czech characters matched explicitly)
        html = Regex.Replace(html, "<span[^>]*>\\s*(?:VELKÝ\\s+)?ČIN\\s*</span>", "", RegexOptions.None);
        html = Regex.Replace(html, "<span[^>]*>\\s*(?:VELK[YÝ]\\s+)?[CČ]IN\\s*</span>", "", RegexOptions.None);
        html = Regex.Replace(html, "(?:VELK[YÝ]\\s+)?[CČ]IN\\s*$", "", RegexOptions.Multiline);

        // Extract, clean, and preserve tables
        var tables = new List<string>();
        html = Regex.Replace(html, @"<table.*?</table>", m =>
        {
            var placeholder = $"{{{{TABLE_{tables.Count}}}}}";
            tables.Add(RebuildCleanTable(m.Value));
            return placeholder;
        }, RegexOptions.Singleline);

        // Strip remaining HTML tags
        var text = Regex.Replace(html, @"<[^>]+>", "");
        text = WebUtility.HtmlDecode(text);

        // Normalize whitespace (collapse spaces and non-breaking spaces)
        text = text.Replace(" ", " ");
        text = Regex.Replace(text, @"\s+", " ");
        text = text.Trim();

        // Restore cleaned tables
        for (var i = 0; i < tables.Count; i++)
            text = text.Replace($"{{{{TABLE_{i}}}}}", tables[i]);

        return text;
    }

    // Rebuild table as clean HTML: no inline styles, no div wrappers in cells, decode entities
    private static string RebuildCleanTable(string tableHtml)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(tableHtml);

        var tableNode = doc.DocumentNode.SelectSingleNode("//table");
        if (tableNode == null) return tableHtml;

        var tableClass = tableNode.GetAttributeValue("class", "");
        var tableTag = tableClass != "" ? $"<table class=\"{tableClass}\">" : "<table>";

        var sb = new System.Text.StringBuilder();
        sb.Append(tableTag);

        foreach (var section in tableNode.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Element))
        {
            if (section.Name is not ("thead" or "tbody" or "tfoot")) continue;
            sb.Append(CleanTableSection(section));
        }

        sb.Append("</table>");
        return sb.ToString();
    }

    private async Task<string> FetchAsync(string path)
    {
        var response = await _httpClient.GetStringAsync($"{BaseUrl}{path}");
        return response;
    }

    private static HtmlDocument ParseHtml(string html)
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
