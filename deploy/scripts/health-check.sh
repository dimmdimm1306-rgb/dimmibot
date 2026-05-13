#!/bin/bash
# Quick health check of all dimbot services.

echo "=== Dimbot Health Check ==="
echo ""

# Service status
for svc in dimbot-ai dimbot-drive dimbot-tunnel; do
    status=$(systemctl is-active "$svc" 2>/dev/null || echo "unknown")
    if [ "$status" = "active" ]; then
        echo "  [OK]   $svc: $status"
    else
        echo "  [FAIL] $svc: $status"
    fi
done

echo ""
echo "=== Local endpoints ==="

# AI server
if curl -sf --max-time 5 http://localhost:8080/config > /dev/null; then
    echo "  [OK]   http://localhost:8080 (AI)"
else
    echo "  [FAIL] http://localhost:8080 (AI)"
fi

# Drive reader
if curl -sf --max-time 5 http://localhost:20129/health > /dev/null; then
    echo "  [OK]   http://localhost:20129 (Drive)"
else
    echo "  [FAIL] http://localhost:20129 (Drive)"
fi

echo ""
echo "=== Public tunnel endpoints ==="
if curl -sf --max-time 8 https://ai.dimmi.online/health > /dev/null; then
    echo "  [OK]   https://ai.dimmi.online"
else
    echo "  [FAIL] https://ai.dimmi.online"
fi
if curl -sf --max-time 8 https://drive.dimmi.online/health > /dev/null; then
    echo "  [OK]   https://drive.dimmi.online"
else
    echo "  [FAIL] https://drive.dimmi.online"
fi

echo ""
echo "=== GitHub config version ==="
ver=$(curl -sf https://raw.githubusercontent.com/dimmdimm1306-rgb/dimmibot/master/cloudflare-config.json 2>/dev/null | python3 -c "import sys,json; print(json.load(sys.stdin).get('version','?'))" 2>/dev/null)
echo "  version: ${ver:-unavailable}"

echo ""
echo "=== Memory ==="
free -h | awk '/^Mem:/ {printf "  Total: %s  Used: %s  Free: %s\n", $2, $3, $4}'

echo ""
echo "=== Disk ==="
df -h / | awk 'NR==2 {printf "  Used: %s / %s (%s)\n", $3, $2, $5}'
