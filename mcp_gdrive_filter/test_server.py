"""Test MCP server bisa start dan list tools dengan benar"""
import os
import sys
import asyncio

# Set credentials path
CREDS_PATH = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    'credentials',
    'gdrive-credentials.json'
)
os.environ['GOOGLE_APPLICATION_CREDENTIALS'] = CREDS_PATH

# Import server module
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from mcp_gdrive_filter import server

async def main():
    print("Testing MCP server...")
    print(f"Credentials: {CREDS_PATH}")
    print()
    
    # Test list_tools
    tools = await server.list_tools()
    print(f"[OK] {len(tools)} tools registered:")
    for t in tools:
        print(f"  - {t.name}: {t.description[:60]}...")
    
    print()
    # Test init_services
    try:
        server.init_services()
        print(f"[OK] Google services initialized")
    except Exception as e:
        print(f"[FAIL] {e}")
        return 1
    
    print()
    # Test call_tool: list_drive_files
    print("Testing list_drive_files tool...")
    result = await server.call_tool("list_drive_files", {"file_type": "all", "page_size": 5})
    print("[OK] Tool call successful")
    print(f"Preview: {result[0].text[:200]}...")
    
    print()
    print("=" * 60)
    print("MCP SERVER READY!")
    print("=" * 60)
    return 0

if __name__ == "__main__":
    exit_code = asyncio.run(main())
    sys.exit(exit_code)
