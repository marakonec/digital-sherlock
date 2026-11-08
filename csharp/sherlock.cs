#:property PublishAot=false
// sherlock.cs  Картка сайту з відкритих джерел: архів інтернету і сам сайт.
//
// Запуск:   dotnet run sherlock.cs -- ust.edu.ua
// Ключі:    --save     зберегти результат у data, щоб показати його без інтернету
//           --offline  показати збережений результат
//
// Програма лише читає те, що опубліковано. Вона нічого не підбирає і нікуди не входить.
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
Console.OutputEncoding = Encoding.UTF8;
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);   // старі сайти бувають у windows-1251

var site = (args.FirstOrDefault(a => !a.StartsWith("--")) ?? "ust.edu.ua")
    .Replace("https://", "").Replace("http://", "").TrimEnd('/');
var file = Path.Combine("data", $"sherlock_{site}.json");

Card card;
if (args.Contains("--offline"))
{
    card = JsonSerializer.Deserialize<Card>(File.ReadAllText(file))!;
    Console.WriteLine($"(збережена копія від {card.Saved})");
}
else
{
    card = await Sherlock.CollectAsync(site);
    if (args.Contains("--save"))
    {
        Directory.CreateDirectory("data");
        card = card with { Saved = DateTime.Now.ToString("dd.MM.yyyy HH:mm") };
        File.WriteAllText(file, JsonSerializer.Serialize(card));
    }
}
Sherlock.Show(card);

record Card(string Site, string? FirstYear, string? OldTitle, int? Versions, string? Title,
            int? Status, string Server, bool? Https, string? Saved = null);

static class Sherlock
{
    public static readonly HttpClient Http = CreateClient();

    static HttpClient CreateClient()
    {
        var handler = new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All };
        var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (DOL-sherlock/1.0)");
        return http;
    }

    public static async Task<Card> CollectAsync(string site)
    {
        var archiveTask = ArchiveAsync(site);          // запит до архіву і запит до сайту йдуть паралельно
        var siteTask = SiteAsync(site);

        var stamps = await archiveTask;
        var (status, server, https, title) = await siteTask;

        string? oldTitle = null;
        foreach (var stamp in (stamps ?? new List<string>()).Take(3))   // найстаріша копія, де є заголовок
        {
            oldTitle = await TitleAsync($"https://web.archive.org/web/{stamp}id_/http://{site}/");
            if (oldTitle is not null) break;
        }

        return new Card(site, stamps?.FirstOrDefault()?[..4], oldTitle, stamps?.Count, title, status, server, https);
    }

    // Список дат усіх копій головної сторінки, де вона змінювалась. null, якщо архів не відповів.
    static async Task<List<string>?> ArchiveAsync(string site)
    {
        try
        {
            var json = await Http.GetStringAsync(
                $"https://web.archive.org/cdx/search/cdx?url={site}&output=json&fl=timestamp&filter=statuscode:200&collapse=digest");
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateArray().Skip(1).Select(r => r[0].GetString()!).ToList();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    // Що сервер каже про себе і як сайт називає себе зараз. Спершу https, якщо не вийшло, то http.
    static async Task<(int?, string, bool?, string?)> SiteAsync(string site)
    {
        foreach (var scheme in new[] { "https", "http" })
        {
            try
            {
                using var r = await Http.GetAsync($"{scheme}://{site}");
                var server = r.Headers.Server.Count > 0 ? r.Headers.Server.ToString() : "не повідомляє";
                var https = r.RequestMessage?.RequestUri?.Scheme == "https";
                return ((int)r.StatusCode, server, https, ExtractTitle(await ReadTextAsync(r)));
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException) { }
        }
        return (null, "не відповідає", null, null);
    }

    static async Task<string?> TitleAsync(string url)
    {
        try
        {
            using var r = await Http.GetAsync(url);
            return r.IsSuccessStatusCode ? ExtractTitle(await ReadTextAsync(r)) : null;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException) { return null; }
    }

    // Читає сторінку у правильному кодуванні: з заголовка відповіді або з тегу meta charset.
    static async Task<string> ReadTextAsync(HttpResponseMessage r)
    {
        var bytes = await r.Content.ReadAsByteArrayAsync();
        var charset = r.Content.Headers.ContentType?.CharSet
            ?? Regex.Match(Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 3000)),
                           @"charset=[""']?([\w-]+)", RegexOptions.IgnoreCase).Groups[1].Value;
        try { return Encoding.GetEncoding(string.IsNullOrEmpty(charset) ? "utf-8" : charset.Trim('"')).GetString(bytes); }
        catch (ArgumentException) { return Encoding.UTF8.GetString(bytes); }
    }

    static string? ExtractTitle(string html)
    {
        var m = Regex.Match(html, @"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!m.Success) return null;
        var t = Regex.Replace(WebUtility.HtmlDecode(m.Groups[1].Value), @"\s+", " ").Trim();
        return t.Length == 0 ? null : t.Length > 60 ? t[..57] + "..." : t;
    }

    public static void Show(Card c)
    {
        string note =
            c.Status is null ? "сайт зараз не відповідає" :
            c.Status is 403 or 429 ? "сайт не пускає автоматичні запити" :
            c.Versions is null ? "архів не відповів, спробуйте пізніше" :
            c.Versions == 0 ? "в архіві немає жодної копії" : "немає";

        var rows = new (string, string)[]
        {
            ("сайт", c.Site),
            ("в архіві з", c.FirstYear ?? "немає даних"),
            ("назва тоді", c.OldTitle ?? "немає даних"),
            ("назва зараз", c.Title ?? "немає даних"),
            ("версій головної", c.Versions?.ToString() ?? "немає даних"),
            ("сервер", c.Server),
            ("https", c.Https switch { true => "так", false => "ні", _ => "невідомо" }),
            ("примітка", note),
        };
        var w = rows.Max(r => r.Item1.Length);
        var v = Math.Max(30, rows.Max(r => r.Item2.Length));
        Console.WriteLine("┌" + new string('─', w + 2) + "┬" + new string('─', v + 2) + "┐");
        foreach (var (k, val) in rows)
            Console.WriteLine($"│ {k.PadRight(w)} │ {val.PadRight(v)} │");
        Console.WriteLine("└" + new string('─', w + 2) + "┴" + new string('─', v + 2) + "┘");
    }
}
