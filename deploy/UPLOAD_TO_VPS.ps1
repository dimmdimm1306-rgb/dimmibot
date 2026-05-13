# =====================================================================
# Upload semua file yang diperlukan ke VPS Ubuntu.
# Pastikan VPS sudah create user 'dimbot' & folder struktur sudah ready.
# Lihat deploy/README.md untuk setup awal VPS.
# =====================================================================

param(
    [Parameter(Mandatory=$true)]
    [string]$VpsHost,      # e.g. 123.45.67.89 atau mydomain.com

    [string]$VpsUser = "dimbot",

    [int]$VpsPort = 22
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  UPLOAD DIMBOT KE VPS" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Target : $VpsUser@$VpsHost`:$VpsPort"
Write-Host "  Source : $ProjectRoot"
Write-Host ""

function Send-File($src, $dst) {
    Write-Host "  -> $dst" -ForegroundColor Yellow
    & scp.exe -P $VpsPort -r -q $src "${VpsUser}@${VpsHost}:${dst}"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [FAIL] scp error" -ForegroundColor Red
        throw "scp failed"
    }
}

function Invoke-Remote($cmd) {
    & ssh.exe -p $VpsPort "${VpsUser}@${VpsHost}" $cmd
}

# 1. OpenClaw AI server
Write-Host "[1/5] Upload OpenClaw (AI server)..." -ForegroundColor Yellow
Push-Location (Join-Path $ProjectRoot "OPENCLAW")
Invoke-Remote "mkdir -p /home/dimbot/ai-server/src"
Send-File "src" "/home/dimbot/ai-server/"
Send-File "package.json" "/home/dimbot/ai-server/"
Send-File "package-lock.json" "/home/dimbot/ai-server/"
if (Test-Path ".env") { Send-File ".env" "/home/dimbot/ai-server/" } else { Write-Host "  [!] .env not found - bot won't have API keys" -ForegroundColor Yellow }
Pop-Location

# 2. GDrive Reader
Write-Host ""
Write-Host "[2/5] Upload GDrive Reader..." -ForegroundColor Yellow
Invoke-Remote "mkdir -p /home/dimbot/drive-reader/mcp_gdrive_filter /home/dimbot/drive-reader/credentials"
Send-File (Join-Path $ProjectRoot "mcp_gdrive_filter\http_server.py") "/home/dimbot/drive-reader/mcp_gdrive_filter/"
Send-File (Join-Path $ProjectRoot "mcp_gdrive_filter\server.py") "/home/dimbot/drive-reader/mcp_gdrive_filter/"
Send-File (Join-Path $ProjectRoot "mcp_gdrive_filter\__init__.py") "/home/dimbot/drive-reader/mcp_gdrive_filter/"
Send-File (Join-Path $ProjectRoot "mcp_gdrive_filter\requirements.txt") "/home/dimbot/drive-reader/mcp_gdrive_filter/"

# Token file
if (Test-Path (Join-Path $ProjectRoot "mcp_gdrive_filter\.token")) {
    Send-File (Join-Path $ProjectRoot "mcp_gdrive_filter\.token") "/home/dimbot/drive-reader/mcp_gdrive_filter/"
    Invoke-Remote "chmod 600 /home/dimbot/drive-reader/mcp_gdrive_filter/.token"
}

# 3. Credentials
Write-Host ""
Write-Host "[3/5] Upload credentials..." -ForegroundColor Yellow
$credsPath = Join-Path $ProjectRoot "credentials\gdrive-credentials.json"
if (Test-Path $credsPath) {
    Send-File $credsPath "/home/dimbot/drive-reader/credentials/"
    Invoke-Remote "chmod 600 /home/dimbot/drive-reader/credentials/gdrive-credentials.json"
} else {
    Write-Host "  [FAIL] credentials/gdrive-credentials.json tidak ada!" -ForegroundColor Red
    throw "Missing credentials"
}

# 4. Systemd services
Write-Host ""
Write-Host "[4/5] Upload systemd services..." -ForegroundColor Yellow
Invoke-Remote "mkdir -p /tmp/dimbot-deploy"
$systemdPath = Join-Path $PSScriptRoot "systemd"
Get-ChildItem $systemdPath -Filter "*.service" | ForEach-Object {
    Send-File $_.FullName "/tmp/dimbot-deploy/"
}

# 5. Scripts
Write-Host ""
Write-Host "[5/5] Upload scripts..." -ForegroundColor Yellow
Invoke-Remote "mkdir -p /home/dimbot/scripts"
$scriptsPath = Join-Path $PSScriptRoot "scripts"
Get-ChildItem $scriptsPath -Filter "*.sh" | ForEach-Object {
    Send-File $_.FullName "/home/dimbot/scripts/"
}
Invoke-Remote "chmod +x /home/dimbot/scripts/*.sh"

Write-Host ""
Write-Host "==================================================" -ForegroundColor Green
Write-Host "  UPLOAD SELESAI" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Langkah selanjutnya di VPS (SSH dulu):" -ForegroundColor Yellow
Write-Host ""
Write-Host "  # 1. Install AI server deps"
Write-Host "  cd /home/dimbot/ai-server && npm install --production"
Write-Host ""
Write-Host "  # 2. Install Drive reader deps"
Write-Host "  cd /home/dimbot/drive-reader"
Write-Host "  python3 -m venv venv"
Write-Host "  source venv/bin/activate"
Write-Host "  pip install -r mcp_gdrive_filter/requirements.txt"
Write-Host "  deactivate"
Write-Host ""
Write-Host "  # 3. Install systemd services (butuh sudo)"
Write-Host "  sudo cp /tmp/dimbot-deploy/*.service /etc/systemd/system/"
Write-Host "  sudo systemctl daemon-reload"
Write-Host "  sudo systemctl enable --now dimbot-ai dimbot-drive dimbot-tunnel-ai dimbot-tunnel-drive"
Write-Host ""
Write-Host "  # 4. Verify"
Write-Host "  /home/dimbot/scripts/health-check.sh"
Write-Host ""
Write-Host "Detail lengkap: lihat deploy/README.md"
Write-Host ""
