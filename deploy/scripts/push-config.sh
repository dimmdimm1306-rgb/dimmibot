#!/bin/bash
# Auto-push cloudflare-config.json ke GitHub kalau tunnel URL berubah.
# Run by cron every 5 min.

set -e

CONFIG_DIR="/home/dimbot/dimmibot-config"
CONFIG_FILE="$CONFIG_DIR/cloudflare-config.json"
AI_LOG="/home/dimbot/logs/tunnel-ai.log"
DRIVE_LOG="/home/dimbot/logs/tunnel-drive.log"

# Extract latest tunnel URLs from cloudflared logs
get_tunnel_url() {
    local log=$1
    # Grep the most recent URL mentioned, keep last occurrence
    grep -oE 'https://[a-zA-Z0-9-]+\.trycloudflare\.com' "$log" 2>/dev/null | tail -n 1
}

AI_URL=$(get_tunnel_url "$AI_LOG")
DRIVE_URL=$(get_tunnel_url "$DRIVE_LOG")

if [ -z "$AI_URL" ] || [ -z "$DRIVE_URL" ]; then
    echo "$(date): tunnel URLs not found yet, skipping"
    exit 0
fi

# Pull latest config
cd "$CONFIG_DIR"
git pull --quiet origin master || true

# Read current URLs from config
CUR_AI=$(python3 -c "import json; print(json.load(open('$CONFIG_FILE')).get('baseUrl',''))" | sed 's|/v1$||')
CUR_DRIVE=$(python3 -c "import json; print(json.load(open('$CONFIG_FILE')).get('gdriveReaderUrl',''))")

# Check if changed
CHANGED=false
if [ "$CUR_AI" != "$AI_URL" ] || [ "$CUR_DRIVE" != "$DRIVE_URL" ]; then
    CHANGED=true
fi

if ! $CHANGED; then
    echo "$(date): no change (AI=$AI_URL Drive=$DRIVE_URL)"
    exit 0
fi

echo "$(date): URLs changed - AI: $CUR_AI -> $AI_URL | Drive: $CUR_DRIVE -> $DRIVE_URL"

# Update config via Python (handles JSON properly, no BOM)
python3 << PYEOF
import json, datetime
with open('$CONFIG_FILE','r',encoding='utf-8') as f: c = json.load(f)
c['baseUrl'] = '$AI_URL/v1'
c['gdriveReaderUrl'] = '$DRIVE_URL'
c['tunnelUrl'] = '$AI_URL'
try:
    c['version'] = str(int(c.get('version','0')) + 1)
except:
    c['version'] = '100'
c['lastTested'] = datetime.datetime.utcnow().strftime('%Y-%m-%dT%H:%M:%SZ')
c['notes'] = 'Auto-updated from VPS at ' + c['lastTested']
with open('$CONFIG_FILE','w',encoding='utf-8',newline='\n') as f:
    json.dump(c, f, indent=4, ensure_ascii=False)
PYEOF

# Commit & push
git add cloudflare-config.json
git commit -m "Auto: VPS tunnel URLs updated $(date -u +%Y-%m-%dT%H:%M:%SZ)" --quiet
git push --quiet origin master && echo "$(date): pushed to GitHub" || echo "$(date): push failed"
