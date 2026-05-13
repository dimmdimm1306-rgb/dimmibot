"""Test readonly enforcement - read operations work, write attempts blocked"""
import os
import sys
import asyncio

CREDS_PATH = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    'credentials',
    'gdrive-credentials.json'
)
os.environ['GOOGLE_APPLICATION_CREDENTIALS'] = CREDS_PATH

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from mcp_gdrive_filter import server

async def main():
    print("=" * 60)
    print("READ-ONLY ENFORCEMENT TEST")
    print("=" * 60)
    print()
    
    # Test 1: Scopes are readonly
    print("[Test 1] Check scopes are read-only...")
    for s in server.SCOPES:
        if not s.endswith('.readonly'):
            print(f"  [FAIL] Non-readonly scope: {s}")
            return 1
        print(f"  [OK] {s}")
    print()
    
    # Test 2: Init services (should install guard)
    print("[Test 2] Initialize services with guard...")
    server.init_services()
    print(f"  [OK] Services initialized")
    print()
    
    # Test 3: Read operation should work
    print("[Test 3] Read operation (list files) should SUCCEED...")
    try:
        result = await server.call_tool("list_drive_files", {"page_size": 5})
        print(f"  [OK] List files works")
    except Exception as e:
        print(f"  [FAIL] Read broken: {e}")
        return 1
    print()
    
    # Test 4: Direct write attempt should BE BLOCKED
    print("[Test 4] Write attempt (files.create) should BE BLOCKED...")
    try:
        # Try to call a write API directly on the service
        server.service.files().create(body={'name': 'hacker.txt'}).execute()
        print(f"  [FAIL] Write was NOT blocked - SECURITY ISSUE!")
        return 1
    except PermissionError as e:
        print(f"  [OK] PermissionError raised by guard: {str(e)[:80]}...")
    except Exception as e:
        # Google itself will also reject because of scope, that's fine too
        msg = str(e).lower()
        if 'insufficient' in msg or 'permission' in msg or 'scope' in msg or 'read-only' in msg:
            print(f"  [OK] Blocked by: {type(e).__name__}: {str(e)[:80]}...")
        else:
            print(f"  [?] Unexpected error: {type(e).__name__}: {e}")
            return 1
    print()
    
    # Test 5: Delete attempt should BE BLOCKED
    print("[Test 5] Delete attempt (files.delete) should BE BLOCKED...")
    try:
        server.service.files().delete(fileId='fake_id').execute()
        print(f"  [FAIL] Delete was NOT blocked!")
        return 1
    except PermissionError as e:
        print(f"  [OK] PermissionError raised by guard")
    except Exception as e:
        msg = str(e).lower()
        if 'insufficient' in msg or 'permission' in msg or 'scope' in msg or 'read-only' in msg or '403' in msg:
            print(f"  [OK] Blocked: {type(e).__name__}: {str(e)[:80]}...")
        else:
            print(f"  [?] Unexpected: {e}")
            return 1
    print()
    
    # Test 6: Update attempt should BE BLOCKED
    print("[Test 6] Update attempt (files.update) should BE BLOCKED...")
    try:
        server.service.files().update(fileId='fake_id', body={'name': 'hacked'}).execute()
        print(f"  [FAIL] Update was NOT blocked!")
        return 1
    except PermissionError as e:
        print(f"  [OK] PermissionError raised by guard")
    except Exception as e:
        msg = str(e).lower()
        if 'insufficient' in msg or 'permission' in msg or 'scope' in msg or 'read-only' in msg or '403' in msg:
            print(f"  [OK] Blocked: {type(e).__name__}: {str(e)[:80]}...")
        else:
            print(f"  [?] Unexpected: {e}")
            return 1
    print()
    
    # Test 7: Sheets batchUpdate should BE BLOCKED
    print("[Test 7] Sheets batchUpdate should BE BLOCKED...")
    try:
        server.sheets_service.spreadsheets().batchUpdate(
            spreadsheetId='fake',
            body={'requests': []}
        ).execute()
        print(f"  [FAIL] batchUpdate was NOT blocked!")
        return 1
    except PermissionError as e:
        print(f"  [OK] PermissionError raised by guard")
    except Exception as e:
        msg = str(e).lower()
        if 'insufficient' in msg or 'permission' in msg or 'scope' in msg or 'read-only' in msg or '403' in msg:
            print(f"  [OK] Blocked: {type(e).__name__}: {str(e)[:80]}...")
        else:
            print(f"  [?] Unexpected: {e}")
            return 1
    print()
    
    print("=" * 60)
    print("ALL READ-ONLY TESTS PASSED!")
    print("Server is LOCKED to read-only mode.")
    print("=" * 60)
    return 0

if __name__ == "__main__":
    exit_code = asyncio.run(main())
    sys.exit(exit_code)
