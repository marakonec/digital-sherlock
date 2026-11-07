#:property PublishAot=false
// Крок 3. Скільки разів за рік змінювалась головна сторінка сайту (за даними архіву інтернету)
// Запуск:  dotnet run step3_archive.cs -- diit.edu.ua
// Ключі:   --save зберегти відповідь архіву в data,  --offline взяти збережену
using System.Text.Json;
Console.OutputEncoding = System.Text.Encoding.UTF8;

var site = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "diit.edu.ua";
var url = $"https://web.archive.org/cdx/search/cdx?url={site}&output=json&fl=timestamp&filter=statuscode:200&collapse=digest";
var file = Path.Combine("data", $"cdx_{site}.json");

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("DOL-sherlock/1.0");
var json = args.Contains("--offline") ? File.ReadAllText(file) : await http.GetStringAsync(url);
if (args.Contains("--save")) { Directory.CreateDirectory("data"); File.WriteAllText(file, json); }

var stamps = JsonDocument.Parse(json).RootElement.EnumerateArray().Skip(1).Select(r => r[0].GetString()!);
foreach (var year in stamps.GroupBy(s => s[..4]).OrderBy(g => g.Key))
{
    var n = year.Count();
    var bar = new string('█', 1 + (int)(Math.Log10(n) * 10));   // логарифмічна шкала, щоб було видно і 3, і 3000
    Console.WriteLine($"{year.Key}  {n,5}  {bar}");
}
