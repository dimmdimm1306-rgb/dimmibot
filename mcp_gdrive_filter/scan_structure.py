"""
Scan semua sheet di spreadsheet ter-share dan tebak struktur header-nya.
Heuristic: baris yang pertama kali punya >=3 non-empty cells TANPA kolom bergabung
kemungkinan besar adalah baris header.
"""
import os
import sys
import json
import requests

TOKEN = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), '.token'), 'r').read().strip()
BASE = os.getenv('GDRIVE_BASE', 'http://localhost:20129')
HEADERS = {"Authorization": f"Bearer {TOKEN}", "Content-Type": "application/json"}

def post(path, body):
    r = requests.post(f"{BASE}{path}", json=body, headers=HEADERS, timeout=60)
    r.raise_for_status()
    return r.json()

def get(path):
    r = requests.get(f"{BASE}{path}", headers=HEADERS, timeout=60)
    r.raise_for_status()
    return r.json()


def detect_header_start(rows):
    """
    Heuristic: cari baris pertama yang kelihatan seperti header.
    Kriteria:
      - >=3 non-empty cells
      - distinct values (bukan semua sama)
      - value mostly text (bukan angka)
      - kalau baris berikutnya juga mostly angka → baris ini header
    Return (row_start_1indexed, header_rows).
    """
    def nonempty(row):
        return [str(c).strip() for c in row if str(c).strip()]

    def mostly_text(row):
        cells = nonempty(row)
        if not cells:
            return False
        non_numeric = 0
        for c in cells:
            try:
                float(str(c).replace(',', ''))
            except ValueError:
                non_numeric += 1
        return non_numeric >= max(2, int(len(cells) * 0.5))

    def distinct(row):
        cells = [str(c).strip().lower() for c in row if str(c).strip()]
        return len(set(cells)) == len(cells)

    for i, row in enumerate(rows):
        cells = nonempty(row)
        if len(cells) < 3:
            continue
        if not mostly_text(row):
            continue
        # Cek baris berikutnya: kalau juga text, mungkin multi-row header
        header_rows = 1
        if i + 1 < len(rows):
            next_cells = nonempty(rows[i + 1])
            # Multi-row header kalau baris ini lebih sedikit isinya (merged) dan baris bawah penuh
            if len(next_cells) > len(cells) * 1.3 and mostly_text(rows[i + 1]):
                header_rows = 2

        # Verify: baris setelah header (header_rows ke bawah) harusnya punya angka atau data
        data_start = i + header_rows
        if data_start < len(rows):
            data_row = nonempty(rows[data_start])
            if len(data_row) >= 2:
                return i + 1, header_rows  # 1-indexed

        return i + 1, header_rows

    return 1, 1  # default


def main():
    # Get all shared files yang tipe spreadsheet
    files = post('/files/list', {"file_type": "spreadsheet", "page_size": 100})
    spreadsheets = files.get('files', [])

    print(f"=== Found {len(spreadsheets)} spreadsheet file(s) ===\n")

    for f in spreadsheets:
        print(f"📊 {f['name']}  (id: {f['id']})  type: {f.get('type')}")
        try:
            tabs_resp = post('/sheet/tabs', {"file_id": f['id']})
            tabs = tabs_resp.get('sheets', [])
            for tab in tabs:
                try:
                    preview = post('/sheet/preview', {
                        "file_id": f['id'],
                        "sheet_name": tab,
                        "max_rows": 8,
                    })
                    rows = preview.get('rows', [])
                    start, hrows = detect_header_start(rows)
                    # Ambil sample header
                    if start <= len(rows):
                        header_row = rows[start - 1]
                        header_preview = [str(c) for c in header_row if str(c).strip()][:8]
                    else:
                        header_preview = []
                    print(f"  └─ 📑 {tab!r}")
                    print(f"       header_row_start={start}, header_rows={hrows}")
                    print(f"       columns preview: {header_preview}")
                except Exception as e:
                    print(f"  └─ 📑 {tab!r}  ERROR preview: {e}")
        except Exception as e:
            print(f"  ERROR tabs: {e}")
        print()

if __name__ == "__main__":
    main()
