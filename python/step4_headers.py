"""Крок 4. Що сервер повідомляє про себе в заголовках відповіді
Запуск:  python step4_headers.py ust.edu.ua
"""
import sys, requests

site = sys.argv[1] if len(sys.argv) > 1 else "ust.edu.ua"
r = requests.get(f"https://{site}", timeout=20, stream=True,
                 headers={"User-Agent": "Mozilla/5.0 (DOL-sherlock)"})
print("Статус:      ", r.status_code)
print("Server:      ", r.headers.get("Server", "не повідомляє"))
print("Content-Type:", r.headers.get("Content-Type", "не вказано"))
