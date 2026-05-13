# =====================================================================
# MASTER Launcher — start semuanya sekaligus:
#   1. AI server (OpenClaw Node.js, port 20128)
#   2. GDrive Reader (Python FastAPI, port 20129)
#   3. Cloudflare tunnel untuk AI (expose 20128)
#   4. Cloudflare tunnel untuk GDrive (expose 20129)
#   5. Update cloudflare-config.json dengan 2 URL tunnel baru
#   6. Auto commit + push ke GitHub
# =====================================================================

$ErrorActionPreference = "Continue"

# Paths
$ScriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$OpenclawDir = Join-Path $ProjectRoot "OPENCLAW"
$CredsPath   = Join-Path $ProjectRoot "credentials\gdrive-credentials.json"
$TokenFile   = Join-Path $ScriptDir ".token"
$ConfigFile  = Join-Path $ProjectRoot "cloudflare-config.json"

$AiPort    = 20128
$DrivePort = 20129

# Read actual AI port from OPENCLAW/.env (override default)
$envFile = Join-Path $OpenclawDir ".env"
if (Test-Path $envFile) {
    $envContent = Get-Content $envFile -Raw
    $portMatch = [regex]::Match($envContent, "(?m)^API_PORT=(\d+)")
    if ($portMatch.Success) {
        $AiPort = [int]$portMatch.Groups[1].Value
    }
}

$env:GOOGLE_APPLICATION_CREDENTIALS = $CredsPath
$env:PYTHONIOENCODING = "utf-8"

Clear-Host
Write-Host ""
Write-Host "  ===========================================================" -ForegroundColor Cyan
Write-Host "       FULL STACK LAUNCHER                                    " -ForegroundColor Cyan
Write-Host "       AI Bot + GDrive Reader + Cloudflare Tunnels            " -ForegroundColor Cyan
Write-Host "  ===========================================================" -ForegroundColor Cyan
Write-Host ""

# ----- Pre-flight -----
Write-Host "[1/7] Pre-flight checks..." -ForegroundColor Yellow

function Check-Cmd([string]$name) {
    $c = Get-Command $name -ErrorAction SilentlyContinue
    if (-not $c) { Write-Host "  [X] $name tidak ada di PATH" -ForegroundColor Red; return $false }
    Write-Host "  [OK] $name" -ForegroundColor Green
    return $true
}

$ok = $true
$ok = (Check-Cmd "python") -and $ok
$ok = (Check-Cmd "node") -and $ok
$ok = (Check-Cmd "cloudflared") -and $ok
$ok = (Check-Cmd "git") -and $ok
if (-not $ok) { Read-Host "Press Enter to exit"; exit 1 }

if (-not (Test-Path $CredsPath)) {
    Write-Host "  [X] Credentials tidak ditemukan: $CredsPath" -ForegroundColor Red
    Read-Host "Press Enter to exit"; exit 1
}
Write-Host "  [OK] Credentials: $(Split-Path $CredsPath -Leaf)" -ForegroundColor Green

if (-not (Test-Path $TokenFile)) {
    Write-Host "  [X] Drive token tidak ada: $TokenFile" -ForegroundColor Red
    Read-Host "Press Enter to exit"; exit 1
}
$Token = (Get-Content $TokenFile -Raw).Trim()
Write-Host "  [OK] Drive token loaded" -ForegroundColor Green

if (-not (Test-Path (Join-Path $OpenclawDir "src\api-server.js"))) {
    Write-Host "  [X] OPENCLAW\src\api-server.js tidak ada" -ForegroundColor Red
    Read-Host "Press Enter to exit"; exit 1
}
Write-Host "  [OK] OpenClaw api-server.js" -ForegroundColor Green

# ----- Cleanup old processes -----
Write-Host ""
Write-Host "[2/7] Cleanup old processes..." -ForegroundColor Yellow

foreach ($p in @($AiPort, $DrivePort)) {
    $conns = Get-NetTCPConnection -LocalPort $p -ErrorAction SilentlyContinue
    if ($conns) {
        $pids = $conns | Select-Object -ExpandProperty OwningProcess -Unique
        foreach ($procId in $pids) {
            try { Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue; Write-Host "  [OK] Killed PID $procId on port $p" -ForegroundColor Green } catch {}
        }
    }
}
Get-Process cloudflared -ErrorAction SilentlyContinue | ForEach-Object {
    try { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue; Write-Host "  [OK] Killed cloudflared PID $($_.Id)" -ForegroundColor Green } catch {}
}
Start-Sleep -Seconds 2

# ----- Start AI server -----
Write-Host ""
Write-Host "[3/7] Starting AI server (port $AiPort)..." -ForegroundColor Yellow

$aiLog = Join-Path $ScriptDir "_ai.log"
if (Test-Path $aiLog) { Remove-Item $aiLog -Force }
$aiProc = Start-Process -FilePath "node" `
    -ArgumentList "src\api-server.js" `
    -WorkingDirectory $OpenclawDir `
    -WindowStyle Minimized `
    -RedirectStandardOutput $aiLog `
    -RedirectStandardError (Join-Path $ScriptDir "_ai_err.log") `
    -PassThru
Write-Host "  [..] PID $($aiProc.Id) starting..." -ForegroundColor DarkGray

# Wait for AI health
$aiOk = $false
for ($i = 1; $i -le 15; $i++) {
    Start-Sleep -Seconds 1
    try {
        $null = Invoke-RestMethod -Uri "http://localhost:$AiPort/config" -TimeoutSec 3 -ErrorAction Stop
        $aiOk = $true; break
    } catch {}
}
if (-not $aiOk) {
    Write-Host "  [!] AI server belum siap setelah 15s, lanjut saja..." -ForegroundColor Yellow
    Write-Host "      Check log: $aiLog" -ForegroundColor DarkGray
} else {
    Write-Host "  [OK] AI server ready" -ForegroundColor Green
}

# ----- Start Drive server -----
Write-Host ""
Write-Host "[4/7] Starting GDrive Reader (port $DrivePort)..." -ForegroundColor Yellow

$driveProc = Start-Process -FilePath "python" `
    -ArgumentList "-m", "mcp_gdrive_filter.http_server" `
    -WorkingDirectory $ProjectRoot `
    -WindowStyle Minimized `
    -PassThru
Start-Sleep -Seconds 3

$driveOk = $false
for ($i = 1; $i -le 10; $i++) {
    try {
        $h = Invoke-RestMethod -Uri "http://localhost:$DrivePort/health" -TimeoutSec 3
        if ($h.status -eq "ok") { $driveOk = $true; break }
    } catch {}
    Start-Sleep -Seconds 1
}
if ($driveOk) {
    Write-Host "  [OK] Drive server ready" -ForegroundColor Green
} else {
    Write-Host "  [!] Drive server tidak respond" -ForegroundColor Red
    Read-Host "Press Enter to exit"; exit 1
}

# ----- Start tunnels -----
Write-Host ""
Write-Host "[5/7] Starting Cloudflare tunnels..." -ForegroundColor Yellow

$aiTunnelLog    = Join-Path $ScriptDir "_ai_tunnel.log"
$driveTunnelLog = Join-Path $ScriptDir "_drive_tunnel.log"
Remove-Item $aiTunnelLog, $driveTunnelLog -Force -ErrorAction SilentlyContinue

$aiTunnelProc = Start-Process -FilePath "cloudflared" `
    -ArgumentList "tunnel", "--url", "http://localhost:$AiPort" `
    -WorkingDirectory $ProjectRoot `
    -WindowStyle Minimized `
    -RedirectStandardError $aiTunnelLog `
    -PassThru

$driveTunnelProc = Start-Process -FilePath "cloudflared" `
    -ArgumentList "tunnel", "--url", "http://localhost:$DrivePort" `
    -WorkingDirectory $ProjectRoot `
    -WindowStyle Minimized `
    -RedirectStandardError $driveTunnelLog `
    -PassThru

function Wait-ForTunnelUrl([string]$logPath, [string]$label) {
    for ($i = 1; $i -le 40; $i++) {
        Start-Sleep -Seconds 1
        if (Test-Path $logPath) {
            $content = Get-Content $logPath -Raw -ErrorAction SilentlyContinue
            if ($content) {
                $match = [regex]::Match($content, "https://[a-zA-Z0-9-]+\.trycloudflare\.com")
                if ($match.Success) {
                    Write-Host "  [OK] $label : $($match.Value)" -ForegroundColor Green
                    return $match.Value
                }
            }
        }
        if ($i % 5 -eq 0) { Write-Host "  ... $label waiting ($i/40)" -ForegroundColor DarkGray }
    }
    return $null
}

$aiTunnelUrl    = Wait-ForTunnelUrl $aiTunnelLog    "AI tunnel   "
$driveTunnelUrl = Wait-ForTunnelUrl $driveTunnelLog "Drive tunnel"

if (-not $aiTunnelUrl -or -not $driveTunnelUrl) {
    Write-Host "  [X] Gagal dapat tunnel URL" -ForegroundColor Red
    Read-Host "Press Enter to exit"; exit 1
}

# ----- Update cloudflare-config.json -----
Write-Host ""
Write-Host "[6/7] Update config & push ke GitHub..." -ForegroundColor Yellow

$configChanged = $false
try {
    $json = Get-Content $ConfigFile -Raw | ConvertFrom-Json
    $aiChanged    = $json.baseUrl -ne "$aiTunnelUrl/v1"
    $driveChanged = $json.gdriveReaderUrl -ne $driveTunnelUrl

    if ($aiChanged) { $json.baseUrl = "$aiTunnelUrl/v1" }
    if ($json.PSObject.Properties.Name -contains 'tunnelUrl') { $json.tunnelUrl = $aiTunnelUrl }
    if ($driveChanged) { $json.gdriveReaderUrl = $driveTunnelUrl }

    if ($aiChanged -or $driveChanged) {
        if ($json.PSObject.Properties.Name -contains 'version') {
            try { $json.version = ([int]$json.version + 1).ToString() } catch { $json.version = "33" }
        }
        if ($json.PSObject.Properties.Name -contains 'lastTested') {
            $json.lastTested = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        }

        $jsonText = $json | ConvertTo-Json -Depth 10
        # Strip BOM (strict parsers reject UTF-8 BOM)
        [System.IO.File]::WriteAllText(
            (Resolve-Path $ConfigFile),
            $jsonText,
            (New-Object System.Text.UTF8Encoding $false)
        )
        $configChanged = $true
        Write-Host "  [OK] Config updated (version $($json.version))" -ForegroundColor Green
        if ($aiChanged)    { Write-Host "      AI baseUrl       -> $($json.baseUrl)" -ForegroundColor DarkGray }
        if ($driveChanged) { Write-Host "      Drive readerUrl  -> $($json.gdriveReaderUrl)" -ForegroundColor DarkGray }
    } else {
        Write-Host "  [OK] Config sudah pakai URL terbaru (skip)" -ForegroundColor Green
    }
} catch {
    Write-Host "  [!] Failed update config: $_" -ForegroundColor Yellow
}

# Git push
if ($configChanged) {
    try {
        Push-Location $ProjectRoot
        git add cloudflare-config.json 2>$null | Out-Null
        $commitMsg = "Auto: update AI + GDrive tunnel URLs ($(Get-Date -Format 'yyyy-MM-dd HH:mm'))"
        git commit -m $commitMsg 2>&1 | Out-Null
        $pushOut = git push origin HEAD 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  [OK] Pushed to GitHub - HP akan auto-fetch saat buka app" -ForegroundColor Green
        } else {
            Write-Host "  [!] Push gagal, coba manual: git push" -ForegroundColor Yellow
            Write-Host "      $pushOut" -ForegroundColor DarkGray
        }
        Pop-Location
    } catch {
        Write-Host "  [!] Git error: $_" -ForegroundColor Yellow
        Pop-Location -ErrorAction SilentlyContinue
    }
}

# ----- Summary -----
Write-Host ""
Write-Host "  ===========================================================" -ForegroundColor Green
Write-Host "                       SEMUA JALAN!                           " -ForegroundColor Green
Write-Host "  ===========================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  AI    (port $AiPort):    $aiTunnelUrl" -ForegroundColor Cyan
Write-Host "  Drive (port $DrivePort):    $driveTunnelUrl" -ForegroundColor Cyan
Write-Host ""
Write-Host "  AI PID         : $($aiProc.Id)" -ForegroundColor DarkGray
Write-Host "  Drive PID      : $($driveProc.Id)" -ForegroundColor DarkGray
Write-Host "  AI tunnel PID  : $($aiTunnelProc.Id)" -ForegroundColor DarkGray
Write-Host "  Drive tunnel   : $($driveTunnelProc.Id)" -ForegroundColor DarkGray
Write-Host ""

# Save status
@{
    ai_url = $aiTunnelUrl
    drive_url = $driveTunnelUrl
    ai_port = $AiPort
    drive_port = $DrivePort
    pids = @{
        ai = $aiProc.Id
        drive = $driveProc.Id
        ai_tunnel = $aiTunnelProc.Id
        drive_tunnel = $driveTunnelProc.Id
    }
    started_at = (Get-Date).ToString("o")
} | ConvertTo-Json | Set-Content (Join-Path $ScriptDir "_status.json")

try { Set-Clipboard -Value "AI: $aiTunnelUrl`nDrive: $driveTunnelUrl"; Write-Host "  [OK] Kedua URL sudah di-copy ke clipboard" -ForegroundColor Green } catch {}

Write-Host ""
Write-Host "  Tekan Ctrl+C atau close window ini untuk stop semua server." -ForegroundColor DarkYellow
Write-Host ""

# ----- Monitor loop -----
Write-Host "[7/7] Monitoring..." -ForegroundColor Yellow
try {
    while ($true) {
        Start-Sleep -Seconds 30
        $dead = @()
        foreach ($kv in @(@{N='AI'; P=$aiProc}, @{N='Drive'; P=$driveProc}, @{N='AI Tunnel'; P=$aiTunnelProc}, @{N='Drive Tunnel'; P=$driveTunnelProc})) {
            if ((Get-Process -Id $kv.P.Id -ErrorAction SilentlyContinue) -eq $null) {
                $dead += $kv.N
            }
        }
        if ($dead.Count -gt 0) {
            Write-Host "  [!] Process mati: $($dead -join ', ')" -ForegroundColor Red
            break
        }
    }
} finally {
    Write-Host ""
    Write-Host "Cleaning up..." -ForegroundColor Yellow
    foreach ($p in @($aiProc, $driveProc, $aiTunnelProc, $driveTunnelProc)) {
        try { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue } catch {}
    }
    Write-Host "Bye." -ForegroundColor Gray
    Start-Sleep -Seconds 2
}
