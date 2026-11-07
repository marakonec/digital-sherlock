"""sherlock.py  Картка сайту з відкритих джерел: архів інтернету і сам сайт.

Запуск:   python sherlock.py ust.edu.ua
Ключі:    --save     зберегти результат у data, щоб показати його без інтернету
          --offline  показати збережений результат

Програма лише читає те, що опубліковано. Вона нічого не підбирає і нікуди не входить.
Потрібна бібліотека requests:  pip install requests
"""
import sys, json, re, html, pathlib, datetime
import requests

UA = {"User-Agent": "Mozilla/5.0 (DOL-sherlock/1.0)"}


def archive(site):
    """Дати всіх копій головної сторінки, де вона змінювалась. None, якщо архів не відповів."""
    try:
        r = requests.get("https://web.archive.org/cdx/search/cdx", headers=UA, timeout=60,
                         params={"url": site, "output": "json", "fl": "timestamp",
                                 "filter": "statuscode:200", "collapse": "digest"})
        r.raise_for_status()
        return [row[0] for row in r.json()[1:]] if r.text.strip() else []
    except (requests.RequestException, ValueError):
        return None


def title_of(response):
    """Заголовок сторінки з тегу title."""
    m = re.search(r"<title[^>]*>(.*?)</title>", response.text, re.I | re.S)
    if not m:
        return None
    t = re.sub(r"\s+", " ", html.unescape(m.group(1))).strip()
    return (t[:57] + "...") if len(t) > 60 else (t or None)


def fetch(url):
    r = requests.get(url, headers=UA, timeout=60)
    if r.encoding is None or r.encoding.lower() == "iso-8859-1":   # старі сайти бувають у windows-1251
        r.encoding = r.apparent_encoding
    return r


def site_info(site):
    """Що сервер каже про себе і як сайт називає себе зараз. Спершу https, якщо не вийшло, то http."""
    for scheme in ("https", "http"):
        try:
            r = fetch(f"{scheme}://{site}")
            return r.status_code, r.headers.get("Server", "не повідомляє"), r.url.startswith("https://"), title_of(r)
        except requests.RequestException:
            pass
    return None, "не відповідає", None, None


def collect(site):
    stamps = archive(site)
    status, server, https, title = site_info(site)
    old_title = None
    for stamp in (stamps or [])[:3]:   # найстаріша копія, де є заголовок
        try:
            r = fetch(f"https://web.archive.org/web/{stamp}id_/http://{site}/")
            old_title = title_of(r) if r.ok else None
        except requests.RequestException:
            old_title = None
        if old_title:
            break
    return {"site": site, "first_year": stamps[0][:4] if stamps else None, "old_title": old_title,
            "title": title, "versions": None if stamps is None else len(stamps),
            "status": status, "server": server, "https": https}


def show(c):
    note = ("сайт зараз не відповідає" if c["status"] is None else
            "сайт не пускає автоматичні запити" if c["status"] in (403, 429) else
            "архів не відповів, спробуйте пізніше" if c["versions"] is None else
            "в архіві немає жодної копії" if c["versions"] == 0 else "немає")
    rows = [("сайт", c["site"]),
            ("в архіві з", c["first_year"] or "немає даних"),
            ("назва тоді", c["old_title"] or "немає даних"),
            ("назва зараз", c["title"] or "немає даних"),
            ("версій головної", "немає даних" if c["versions"] is None else str(c["versions"])),
            ("сервер", c["server"]),
            ("https", {True: "так", False: "ні"}.get(c["https"], "невідомо")),
            ("примітка", note)]
    w = max(len(k) for k, _ in rows)
    v = max(30, max(len(x) for _, x in rows))
    print("┌" + "─" * (w + 2) + "┬" + "─" * (v + 2) + "┐")
    for k, x in rows:
        print(f"│ {k:<{w}} │ {x:<{v}} │")
    print("└" + "─" * (w + 2) + "┴" + "─" * (v + 2) + "┘")


if __name__ == "__main__":
    args = sys.argv[1:]
    site = next((a for a in args if not a.startswith("--")), "ust.edu.ua")
    site = site.replace("https://", "").replace("http://", "").rstrip("/")
    path = pathlib.Path("data") / f"sherlock_{site}.json"
    if "--offline" in args:
        card = json.loads(path.read_text(encoding="utf-8"))
        print(f"(збережена копія від {card.get('saved')})")
    else:
        card = collect(site)
        if "--save" in args:
            card["saved"] = datetime.datetime.now().strftime("%d.%m.%Y %H:%M")
            path.parent.mkdir(exist_ok=True)
            path.write_text(json.dumps(card, ensure_ascii=False, indent=1), encoding="utf-8")
    show(card)

    # ↓ Місце для того, що допишемо на занятті
