"""Test koneksi ke Google Drive & enable APIs"""
import os
import sys
from google.oauth2 import service_account
from googleapiclient.discovery import build
from googleapiclient.errors import HttpError

CREDS_PATH = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    'credentials',
    'gdrive-credentials.json'
)

SCOPES = [
    'https://www.googleapis.com/auth/drive.readonly',
    'https://www.googleapis.com/auth/spreadsheets.readonly'
]

def test():
    print(f"Using credentials: {CREDS_PATH}")
    print(f"File exists: {os.path.exists(CREDS_PATH)}")
    
    credentials = service_account.Credentials.from_service_account_file(
        CREDS_PATH, scopes=SCOPES)
    
    print(f"Service account email: {credentials.service_account_email}")
    print(f"Project ID: {credentials.project_id}")
    print()
    
    # Test Drive API
    print("Testing Google Drive API...")
    try:
        service = build('drive', 'v3', credentials=credentials)
        results = service.files().list(
            pageSize=10,
            fields="files(id, name, mimeType)"
        ).execute()
        files = results.get('files', [])
        print(f"  [OK] Drive API connected")
        print(f"  Files shared with service account: {len(files)}")
        if files:
            for f in files[:5]:
                print(f"    - {f.get('name')} ({f.get('mimeType')})")
        else:
            print("  [!] No files shared yet")
            print(f"  Share files/folders with: {credentials.service_account_email}")
    except HttpError as e:
        print(f"  [ERROR] Drive API: {e}")
        if 'has not been used' in str(e) or 'is disabled' in str(e):
            print(f"  -> Enable Drive API at: https://console.cloud.google.com/apis/library/drive.googleapis.com?project={credentials.project_id}")
        return False
    
    print()
    # Test Sheets API
    print("Testing Google Sheets API...")
    try:
        sheets = build('sheets', 'v4', credentials=credentials)
        print(f"  [OK] Sheets API client created")
    except HttpError as e:
        print(f"  [ERROR] Sheets API: {e}")
    
    print()
    print("=" * 60)
    print("SUCCESS! MCP Server ready to use.")
    print("=" * 60)
    return True

if __name__ == "__main__":
    try:
        success = test()
        sys.exit(0 if success else 1)
    except Exception as e:
        print(f"[FAIL] {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
