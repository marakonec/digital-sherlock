#:property PublishAot=false
#:package MetadataExtractor@*
// Крок 5б. Що фото знає про себе: камера, дата, координати
// Запуск:  dotnet run step6_photo.cs -- data/DSCN0025.jpg
using System.Globalization;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
Console.OutputEncoding = System.Text.Encoding.UTF8;

var path = args.FirstOrDefault() ?? Path.Combine("data", "DSCN0025.jpg");
var dirs = ImageMetadataReader.ReadMetadata(path);
var ifd0 = dirs.OfType<ExifIfd0Directory>().FirstOrDefault();
var sub = dirs.OfType<ExifSubIfdDirectory>().FirstOrDefault();
Console.WriteLine($"Камера: {ifd0?.GetDescription(ExifDirectoryBase.TagMake)} {ifd0?.GetDescription(ExifDirectoryBase.TagModel)}");
Console.WriteLine($"Знято:  {sub?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal) ?? "не вказано"}");

var gps = dirs.OfType<GpsDirectory>().FirstOrDefault();
var lat = gps?.GetRationalArray(GpsDirectory.TagLatitude);
var lon = gps?.GetRationalArray(GpsDirectory.TagLongitude);
if (gps is null || lat is null || lon is null) { Console.WriteLine("Координат у файлі немає"); return; }

double Deg(Rational[] v, string? side) =>
    (v[0].ToDouble() + v[1].ToDouble() / 60 + v[2].ToDouble() / 3600) * (side is "S" or "W" ? -1 : 1);
var point = string.Create(CultureInfo.InvariantCulture,
    $"{Deg(lat, gps.GetString(GpsDirectory.TagLatitudeRef)):F5},{Deg(lon, gps.GetString(GpsDirectory.TagLongitudeRef)):F5}");
Console.WriteLine($"Координати: {point}");
Console.WriteLine($"Карта: https://www.google.com/maps?q={point}");
