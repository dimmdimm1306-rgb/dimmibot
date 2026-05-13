#!/bin/bash
# Test setup di VPS
set -e

echo "=== Test AI server (Node.js) ==="
cd /home/dimbot/ai-server
node -e "const pkg = require('./package.json'); console.log('Package:', pkg.name, pkg.version);" 2>&1 || echo "Node test failed"
ls src/api-server.js > /dev/null && echo "  api-server.js: OK"

echo ""
echo "=== Test Drive server (Python) ==="
cd /home/dimbot/drive-reader
export GOOGLE_APPLICATION_CREDENTIALS=/home/dimbot/drive-reader/credentials/gdrive-credentials.json
venv/bin/python -c "
from mcp_gdrive_filter import server
server.init_services()
print('  Drive init:', 'OK' if server.service else 'FAIL')
print('  Sheets init:', 'OK' if server.sheets_service else 'FAIL')
"

echo ""
echo "=== Test Drive can list files ==="
venv/bin/python -c "
import os
os.environ['GOOGLE_APPLICATION_CREDENTIALS']='/home/dimbot/drive-reader/credentials/gdrive-credentials.json'
from mcp_gdrive_filter import server
server.init_services()
r = server.service.files().list(pageSize=3, fields='files(name, id)').execute()
print('  Shared files:', len(r.get('files', [])))
for f in r.get('files', [])[:3]:
    print('   -', f['name'])
"

echo ""
echo "DONE"
