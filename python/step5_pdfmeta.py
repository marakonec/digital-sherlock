"""Крок 5а. Що документ PDF знає про себе
Запуск:  python step5_pdfmeta.py data/opp.pdf      (потрібно: pip install pypdf)
"""
import sys, re
from pypdf import PdfReader


def pdf_date(raw):
    """Дата в PDF записана як D:РРРРММДДггххсс, переводимо у звичний вигляд."""
    m = re.match(r"D:(\d{4})(\d{2})(\d{2})(\d{2})?(\d{2})?", str(raw or ""))
    return f"{m[3]}.{m[2]}.{m[1]} {m[4]}:{m[5]}" if m else (raw or "не вказано")


m = PdfReader(sys.argv[1] if len(sys.argv) > 1 else "data/opp.pdf").metadata or {}
print("Створено в: ", m.get("/Creator", "не вказано"))
print("Збережено:  ", m.get("/Producer", "не вказано"))
print("Створено:   ", pdf_date(m.get("/CreationDate")))
print("Змінено:    ", pdf_date(m.get("/ModDate")))
print("Автор:      ", m.get("/Author", "не вказано"))
