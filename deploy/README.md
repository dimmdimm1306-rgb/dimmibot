# Deploy Dimbot Backend ke VPS Ubuntu

Paket ini deploy **AI proxy server** + **GDrive Reader** + **Cloudflare tunnels** ke VPS Ubuntu. Setelah deploy, bot akan jalan 24/7 tanpa perlu laptop.

## Struktur di VPS

```
/home/dimbot/
├── ai-server/           OpenClaw Node.js
│   ├── src/
│   ├── .env             (API keys)
│   └── node_modules/
├── drive-reader/        Python MCP + FastAPI
│   ├── mcp_gdrive_filter/
│   └── credentials/
│       └── gdrive-credentials.json
├── scripts/
│   ├── push-config.sh   Auto-push URL baru ke GitHub
│   └── health-check.sh
└── logs/
```

## Langkah Deploy (dari Laptop ke VPS)

### 1. SSH ke VPS & Setup User

```bash
# Di VPS, sebagai root atau user yang punya sudo
sudo adduser --disabled-password --gecos "" dimbot
sudo usermod -aG sudo dimbot
sudo mkdir -p /home/dimbot/{ai-server,drive-reader/credentials,scripts,logs}
sudo chown -R dimbot:dimbot /home/dimbot
```

### 2. Install Dependencies (di VPS)

```bash
# Node.js 20+ (kalau belum ada)
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt install -y nodejs

# Python 3 + pip
sudo apt install -y python3 python3-pip python3-venv

# cloudflared
curl -L https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb -o /tmp/cf.deb
sudo dpkg -i /tmp/cf.deb

# git (untuk auto-push config)
sudo apt install -y git

# Swap (kalau VPS kecil)
sudo fallocate -l 2G /swapfile && sudo chmod 600 /swapfile && sudo mkswap /swapfile && sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

### 3. Upload Code dari Laptop

Di laptop (dari folder project):

```powershell
# Upload AI server (OpenClaw)
scp -r OPENCLAW/src OPENCLAW/package.json OPENCLAW/package-lock.json OPENCLAW/.env dimbot@VPS_IP:/home/dimbot/ai-server/

# Upload GDrive reader
scp -r mcp_gdrive_filter dimbot@VPS_IP:/home/dimbot/drive-reader/

# Upload credentials
scp credentials/gdrive-credentials.json dimbot@VPS_IP:/home/dimbot/drive-reader/credentials/

# Upload systemd + scripts
scp deploy/systemd/*.service deploy/scripts/*.sh dimbot@VPS_IP:/tmp/
```

### 4. Install di VPS

```bash
# SSH ke VPS as dimbot
ssh dimbot@VPS_IP

# Install AI server deps
cd /home/dimbot/ai-server
npm install --production

# Install Drive reader deps
cd /home/dimbot/drive-reader
python3 -m venv venv
source venv/bin/activate
pip install -r mcp_gdrive_filter/requirements.txt
deactivate

# Set up bearer token (copy dari laptop-nya)
echo "GANTI_DENGAN_TOKEN_DARI_LAPTOP" > /home/dimbot/drive-reader/mcp_gdrive_filter/.token
chmod 600 /home/dimbot/drive-reader/mcp_gdrive_filter/.token

# Scripts
sudo mv /tmp/*.sh /home/dimbot/scripts/
sudo chown dimbot:dimbot /home/dimbot/scripts/*.sh
chmod +x /home/dimbot/scripts/*.sh

# Systemd services
sudo mv /tmp/*.service /etc/systemd/system/
sudo systemctl daemon-reload
```

### 5. Git Setup (untuk auto-push config)

```bash
cd /home/dimbot/scripts
git config --global user.email "dimbot@vps"
git config --global user.name "Dimbot VPS"

# Clone repo dimmibot buat auto-push
git clone https://github.com/dimmdimm1306-rgb/dimmibot.git /home/dimbot/dimmibot-config

# Buat PAT di github.com/settings/tokens (Fine-grained token, repo: dimmibot, permission: Contents R/W)
# Lalu:
cd /home/dimbot/dimmibot-config
git remote set-url origin https://GITHUB_USERNAME:GITHUB_PAT@github.com/dimmdimm1306-rgb/dimmibot.git
```

### 6. Enable & Start Services

```bash
sudo systemctl enable --now dimbot-ai dimbot-drive dimbot-tunnel-ai dimbot-tunnel-drive
sudo systemctl status dimbot-*
```

### 7. Verify

```bash
# Cek service sehat
curl http://localhost:8080/config
curl http://localhost:20129/health

# Cek tunnel URL (dari logs)
sudo journalctl -u dimbot-tunnel-ai -n 50 --no-pager | grep trycloudflare
sudo journalctl -u dimbot-tunnel-drive -n 50 --no-pager | grep trycloudflare
```

### 8. Auto-push URL ke GitHub

```bash
# Run sekali manual untuk test
/home/dimbot/scripts/push-config.sh

# Cronjob tiap 5 menit check & push kalau URL berubah
crontab -e
```

Tambah:
```
*/5 * * * * /home/dimbot/scripts/push-config.sh >> /home/dimbot/logs/push-config.log 2>&1
```

## Update Code di VPS

Kalau ada update:

```bash
# Dari laptop
scp file.js dimbot@VPS_IP:/home/dimbot/ai-server/src/

# Restart service
ssh dimbot@VPS_IP "sudo systemctl restart dimbot-ai"
```

## Troubleshoot

```bash
# Lihat log realtime
sudo journalctl -u dimbot-ai -f
sudo journalctl -u dimbot-drive -f
sudo journalctl -u dimbot-tunnel-ai -f

# Restart all
sudo systemctl restart dimbot-*

# Check firewall (kalau pakai ufw)
sudo ufw status
```

## Kenapa Quick Tunnel (bukan Named Tunnel)

Pakai quick tunnel (tanpa domain) simpel, tapi URL berubah tiap tunnel restart. Script `push-config.sh` otomatis push URL baru ke GitHub kalau berubah, jadi HP user tetap sync.

Kalau punya domain sendiri, bisa upgrade ke named tunnel dengan subdomain fixed (`ai.dimas.dev`, `drive.dimas.dev`) — tutorial di Cloudflare Zero Trust dashboard.
