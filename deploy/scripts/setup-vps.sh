#!/bin/bash
# One-shot setup script. Run this AT THE VPS sebagai user dimbot setelah
# UPLOAD_TO_VPS.ps1 selesai. Butuh sudo untuk install systemd.
#
# Usage: bash /home/dimbot/scripts/setup-vps.sh

set -e

echo "=================================================="
echo "  DIMBOT VPS SETUP"
echo "=================================================="
echo ""

# 1. Install AI server deps
echo "[1/5] Installing AI server deps (npm install)..."
cd /home/dimbot/ai-server
npm install --production --silent 2>&1 | tail -5
echo "  [OK]"
echo ""

# 2. Install Drive reader deps
echo "[2/5] Setting up Python venv for Drive reader..."
cd /home/dimbot/drive-reader
if [ ! -d "venv" ]; then
    python3 -m venv venv
fi
source venv/bin/activate
pip install -q -r mcp_gdrive_filter/requirements.txt
deactivate
echo "  [OK]"
echo ""

# 3. Install systemd services
echo "[3/5] Installing systemd services..."
sudo cp /tmp/dimbot-deploy/*.service /etc/systemd/system/
sudo systemctl daemon-reload
echo "  [OK]"
echo ""

# 4. Enable & start
echo "[4/5] Enabling services..."
sudo systemctl enable --now dimbot-ai dimbot-drive dimbot-tunnel-ai dimbot-tunnel-drive
sleep 5
echo "  [OK]"
echo ""

# 5. Health check
echo "[5/5] Health check..."
sleep 10
bash /home/dimbot/scripts/health-check.sh
echo ""

echo "=================================================="
echo "  SETUP SELESAI"
echo "=================================================="
echo ""
echo "Next: setup cron untuk auto-push config ke GitHub"
echo ""
echo "  crontab -e"
echo ""
echo "Tambahkan baris:"
echo "  */5 * * * * /home/dimbot/scripts/push-config.sh >> /home/dimbot/logs/push-config.log 2>&1"
echo ""
echo "Dan jangan lupa setup git di /home/dimbot/dimmibot-config/ (lihat deploy/README.md step 5)"
echo ""
