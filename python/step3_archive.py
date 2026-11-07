"""Крок 3. Скільки разів за рік змінювалась головна сторінка сайту (за даними архіву інтернету)
Запуск:  python step3_archive.py diit.edu.ua
Ключі:   --save зберегти відповідь архіву в data,  --offline взяти збережену
"""
import sys, json, math, collections, pathlib, requests

args = sys.argv[1:]
site = next((a for a in args if not a.startswith("--")), "diit.edu.ua")
file = pathlib.Path("data") / f"cdx_{site}.json"

if "--offline" in args:
    text = file.read_text(encoding="utf-8")
else:
    text = requests.get("https://web.archive.org/cdx/search/cdx", timeout=60,
                        headers={"User-Agent": "DOL-sherlock/1.0"},
                        params={"url": site, "output": "json", "fl": "timestamp",
                                "filter": "statuscode:200", "collapse": "digest"}).text
if "--save" in args:
    file.parent.mkdir(exist_ok=True); file.write_text(text, encoding="utf-8")

years = collections.Counter(row[0][:4] for row in json.loads(text)[1:])
for year, n in sorted(years.items()):
    print(f"{year}  {n:5d}  {'█' * (1 + int(math.log10(n) * 10))}")   # логарифмічна шкала
