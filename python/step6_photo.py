"""Крок 5б. Що фото знає про себе: камера, дата, координати
Запуск:  python step6_photo.py data/DSCN0025.jpg      (потрібно: pip install Pillow)
"""
import sys
from PIL import Image
from PIL.ExifTags import GPSTAGS

exif = Image.open(sys.argv[1] if len(sys.argv) > 1 else "data/DSCN0025.jpg").getexif()
print("Камера:", exif.get(0x010F, ""), exif.get(0x0110, ""))
print("Знято: ", exif.get_ifd(0x8769).get(0x9003, "не вказано"))

gps = {GPSTAGS.get(k, k): v for k, v in exif.get_ifd(0x8825).items()}
if "GPSLatitude" not in gps:
    sys.exit("Координат у файлі немає")
deg = lambda v, side: (float(v[0]) + float(v[1]) / 60 + float(v[2]) / 3600) * (-1 if side in "SW" else 1)
point = f"{deg(gps['GPSLatitude'], gps['GPSLatitudeRef']):.5f},{deg(gps['GPSLongitude'], gps['GPSLongitudeRef']):.5f}"
print("Координати:", point)
print("Карта: https://www.google.com/maps?q=" + point)
