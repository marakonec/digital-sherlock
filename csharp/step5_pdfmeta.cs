#:package PdfPig@*
#:property PublishAot=false
// Крок 5а. Що документ PDF знає про себе
// Запуск:  dotnet run step5_pdfmeta.cs -- data/opp.pdf
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
Console.OutputEncoding = System.Text.Encoding.UTF8;

var path = args.FirstOrDefault() ?? Path.Combine("data", "opp.pdf");
using var pdf = PdfDocument.Open(path);
var info = pdf.Information;

Console.WriteLine($"Створено в:  {info.Creator ?? "не вказано"}");
Console.WriteLine($"Збережено:   {info.Producer ?? "не вказано"}");
Console.WriteLine($"Створено:    {PdfDate(info.CreationDate)}");
Console.WriteLine($"Змінено:     {PdfDate(info.ModifiedDate)}");
Console.WriteLine($"Автор:       {info.Author ?? "не вказано"}");

// Дата в PDF записана як D:РРРРММДДггххсс, переводимо у звичний вигляд
static string PdfDate(string? raw)
{
    var m = Regex.Match(raw ?? "", @"D:(\d{4})(\d{2})(\d{2})(\d{2})?(\d{2})?");
    return m.Success ? $"{m.Groups[3]}.{m.Groups[2]}.{m.Groups[1]} {m.Groups[4]}:{m.Groups[5]}" : raw ?? "не вказано";
}
