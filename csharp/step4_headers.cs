#:property PublishAot=false
// Крок 4. Що сервер повідомляє про себе в заголовках відповіді
// Запуск:  dotnet run step4_headers.cs -- ust.edu.ua
Console.OutputEncoding = System.Text.Encoding.UTF8;

var site = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "ust.edu.ua";
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (DOL-sherlock)");

using var r = await http.GetAsync($"https://{site}", HttpCompletionOption.ResponseHeadersRead);
Console.WriteLine($"Статус:       {(int)r.StatusCode}");
Console.WriteLine($"Server:       {(r.Headers.Server.Count > 0 ? r.Headers.Server.ToString() : "не повідомляє")}");
Console.WriteLine($"Content-Type: {r.Content.Headers.ContentType}");
