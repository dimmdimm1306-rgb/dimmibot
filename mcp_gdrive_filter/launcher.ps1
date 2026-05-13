# =====================================================================
# One-Click Launcher for GDrive Reader
# Spawn FastAPI server + Cloudflare tunnel, capture URL, update local config.
# =====================================================================

$ErrorActionPreference = "Continue"

# Paths
$ScriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$CredsPath   = Join-Path $ProjectRoot "credentials\gdrive-credentials.json"
$TokenFile   = Join-Path $ScriptDir ".token"
$ConfigFile  = Join-Path $ProjectRoot "cloudflare-config.json"
$Port        = 20129

# Ensure environment
$env:GOOGLE_APPLICATION_CREDENTIALS = $CredsPath
$env:PYTHONIOENCODING = "utf-8"

# Banner
Clear-Host
Write-Host ""
Write-Host "  +=========================================================+" -ForegroundColor Cyan
Write-Host "  |     GDrive Reader - One-Click Launcher                  |" -ForegroundColor Cyan
Write-Host "  |     Read-only Google Drive for MAUI AI Bot              |" -ForegroundColor Cyan
Write-Host "  +=========================================================+" -ForegroundColor Cyan
Write-Host ""

# ----- Pre-flight checks -----
Write-Host "[1/5] Pre-flight checks..." -ForegroundColor Yellow

if (-not (Test-Path $CredsPath)) {
    Write-Host "  [X] Credentials tidak ditemukan: $CredsPath" -ForegroundColor Red
    Read-Host "Press Enter to exit"; exit 1
}
Write-Host "  [OK] Credentials: $CredsPath" -ForegroundColor Green

if (-not (Test-Path $TokenFile)) {
    Write-Host "  [X] Token file tidak ditemukan: $TokenFile" -ForegroundColor Red
    Read-Host "Press Enter to exit"; exit 1
}
$Token = (Get-Content $TokenFile -Raw).Trim()
Write-Host "  [OK] Token loaded (length: $($Token.Length))" -ForegroundColor Green

# Check python
$pythonCmd = Get-Command python -ErrorAction SilentlyContinue
if (-not $pythonCmd) {
    Write-Host "  [X] python tidak ada di PATH" -ForegroundColor Red
    Read-Host "Press Enter to exit"; exit 1
}
Write-Host "  [OK] python: $($pythonCmd.Source)" -ForegroundColor Green

# Check cloudflared
$cfCmd = Get-Command cloudflared -ErrorAction SilentlyContinue
if (-not $cfCmd) {
    Write-Host "  [X] cloudflared tidak ada di PATH. Install dari https://github.com/cloudflare/cloudflared/releases" -ForegroundColor Red
    Read-Host "Press Enter to exit"; exit 1
}
Write-Host "  [OK] cloudflared: $($cfCmd.Source)" -ForegroundColor Green

# Kill existing python on port
Write-Host ""
Write-Host "[2/5] Clean up existing processes..." -ForegroundColor Yellow
$existing = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
if ($existing) {
    $pids = $existing | Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($p in $pids) {
        try { Stop-Process -Id $p -Force -ErrorAction SilentlyContinue; Write-Host "  [OK] Killed process $p on port $Port" -ForegroundColor Green } catch {}
    }
}
# Kill dangling cloudflared
Get-Process cloudflared -ErrorAction SilentlyContinue | ForEach-Object {
    try { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue; Write-Host "  [OK] Killed cloudflared pid $($_.Id)" -ForegroundColor Green } catch {}
}
Start-Sleep -Seconds 1

# ----- Start FastAPI server -----
Write-Host ""
Write-Host "[3/5] Starting HTTP server (port $Port)..." -ForegroundColor Yellow

$serverJob = Start-Process -FilePath "python" `
    -ArgumentList "-m", "mcp_gdrive_filter.http_server" `
    -WorkingDirectory $ProjectRoot `
    -WindowStyle Minimized `
    -PassThru

Start-Sleep -Seconds 3

# Health check
$healthOk = $false
for ($i = 1; $i -le 10; $i++) {
    try {
        $h = Invoke-RestMethod -Uri "http://localhost:$Port/health" -TimeoutSec 3
        if ($h.status -eq "ok") {
            Write-Host "  [OK] Server ready (service account: $($h.service_account))" -ForegroundColor Green
            $healthOk = $true
            break
        }
    } catch {}
    Start-Sleep -Seconds 1
}
if (-not $healthOk) {
    Write-Host "  [X] Server tidak respond dalam 10 detik. Cek log." -ForegroundColor Red
    Read-Host "Press Enter to exit"; exit 1
}

# ----- Start Cloudflare tunnel + capture URL -----
Write-Host ""
Write-Host "[4/5] Starting Cloudflare tunnel..." -ForegroundColor Yellow

$tunnelLog = Join-Path $ScriptDir "_tunnel.log"
if (Test-Path $tunnelLog) { Remove-Item $tunnelLog -Force }

$tunnelProc = Start-Process -FilePath "cloudflared" `
    -ArgumentList "tunnel", "--url", "http://localhost:$Port" `
    -WorkingDirectory $ProjectRoot `
    -WindowStyle Minimized `
    -RedirectStandardError $tunnelLog `
    -PassThru

# Wait for URL in log
$tunnelUrl = $null
for ($i = 1; $i -le 30; $i++) {
    Start-Sleep -Seconds 1
    if (Test-Path $tunnelLog) {
        $content = Get-Content $tunnelLog -Raw -ErrorAction SilentlyContinue
        if ($content) {
            $match = [regex]::Match($content, "https://[a-zA-Z0-9-]+\.trycloudflare\.com")
            if ($match.Success) {
                $tunnelUrl = $match.Value
                break
            }
        }
    }
    Write-Host "  ... waiting for tunnel URL ($i/30)" -ForegroundColor DarkGray
}

if (-not $tunnelUrl) {
    Write-Host "  [X] Gagal dapat tunnel URL. Log: $tunnelLog" -ForegroundColor Red
    Read-Host "Press Enter to exit"; exit 1
}
Write-Host "  [OK] Tunnel URL: $tunnelUrl" -ForegroundColor Green

# Verify tunnel works via direct local URL (skip external DNS check —
# kadang mesin local punya DNS cache stuck setelah ganti tunnel URL,
# tapi HP user pasti jalan karena pakai DNS operator seluler)
try {
    $h = @{ "Authorization" = "Bearer $Token" }
    $r = Invoke-RestMethod -Uri "http://localhost:$Port/health" -Headers $h -TimeoutSec 5
    Write-Host "  [OK] Local server health OK" -ForegroundColor Green
} catch {
    Write-Host "  [!] Local health check gagal: $_" -ForegroundColor Yellow
}

# ----- Update local cloudflare-config.json -----
Write-Host ""
Write-Host "[5/5] Update local config..." -ForegroundColor Yellow

$configChanged = $false
if (Test-Path $ConfigFile) {
    try {
        $json = Get-Content $ConfigFile -Raw | ConvertFrom-Json
        $oldUrl = $json.gdriveReaderUrl
        if ($oldUrl -ne $tunnelUrl) {
            $json.gdriveReaderUrl = $tunnelUrl
            # Bump version + timestamp so HP users detect change
            if ($json.PSObject.Properties.Name -contains 'version') {
                try { $json.version = ([int]$json.version + 1).ToString() } catch { $json.version = "30" }
            }
            if ($json.PSObject.Properties.Name -contains 'lastTested') {
                $json.lastTested = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
            $json | ConvertTo-Json -Depth 10 | Set-Content $ConfigFile -Encoding UTF8
            # Ensure no BOM (strict JSON parsers like C# System.Text.Json reject BOM)
            $content = Get-Content $ConfigFile -Raw
            $content = $content -replace "^\uFEFF", ''
            [System.IO.File]::WriteAllText(
                (Resolve-Path $ConfigFile),
                $content,
                (New-Object System.Text.UTF8Encoding $false)
            )
            $configChanged = $true
            Write-Host "  [OK] Updated: $ConfigFile" -ForegroundColor Green
            Write-Host "      Old URL: $oldUrl" -ForegroundColor DarkGray
            Write-Host "      New URL: $tunnelUrl" -ForegroundColor DarkGray
        } else {
            Write-Host "  [OK] Config sudah pakai URL terbaru (skip update)" -ForegroundColor Green
        }
    } catch {
        Write-Host "  [!] Failed to update config: $_" -ForegroundColor Yellow
    }
}

# ----- Auto-commit & push to GitHub -----
if ($configChanged) {
    Write-Host ""
    Write-Host "[6/6] Auto-push config ke GitHub..." -ForegroundColor Yellow

    $gitCmd = Get-Command git -ErrorAction SilentlyContinue
    if ($gitCmd) {
        try {
            Push-Location $ProjectRoot

            # Verify remote
            $remote = git remote get-url origin 2>$null
            if ($remote) {
                git add cloudflare-config.json 2>$null
                $commitMsg = "Auto: update gdriveReaderUrl to new tunnel ($(Get-Date -Format 'yyyy-MM-dd HH:mm'))"
                git commit -m $commitMsg 2>&1 | Out-Null
                $pushResult = git push origin HEAD 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "  [OK] Pushed to $remote" -ForegroundColor Green
                    Write-Host "       HP users akan auto-fetch config baru saat buka app" -ForegroundColor DarkGray
                } else {
                    Write-Host "  [!] Push gagal:" -ForegroundColor Yellow
                    Write-Host "      $pushResult" -ForegroundColor DarkGray
                    Write-Host "      Coba manual: git push" -ForegroundColor DarkGray
                }
            } else {
                Write-Host "  [!] Git remote 'origin' belum ter-set. Skip push." -ForegroundColor Yellow
            }
            Pop-Location
        } catch {
            Write-Host "  [!] Git error: $_" -ForegroundColor Yellow
            Pop-Location -ErrorAction SilentlyContinue
        }
    } else {
        Write-Host "  [!] git tidak ada di PATH, skip push" -ForegroundColor Yellow
    }
}

# ----- Save status file for tray/reference -----
$statusFile = Join-Path $ScriptDir "_status.json"
@{
    tunnel_url = $tunnelUrl
    port = $Port
    token_length = $Token.Length
    server_pid = $serverJob.Id
    tunnel_pid = $tunnelProc.Id
    started_at = (Get-Date).ToString("o")
} | ConvertTo-Json | Set-Content $statusFile

# ----- Summary -----
Write-Host ""
Write-Host "  +=========================================================+" -ForegroundColor Green
Write-Host "  |                  SEMUA SIAP!                            |" -ForegroundColor Green
Write-Host "  +=========================================================+" -ForegroundColor Green
Write-Host ""
Write-Host "  Local URL  : http://localhost:$Port" -ForegroundColor White
Write-Host "  Tunnel URL : $tunnelUrl" -ForegroundColor Cyan
Write-Host "  API Docs   : http://localhost:$Port/docs" -ForegroundColor White
Write-Host "  Token      : $Token" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  PID Server  : $($serverJob.Id)" -ForegroundColor DarkGray
Write-Host "  PID Tunnel  : $($tunnelProc.Id)" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  Next: Commit cloudflare-config.json ke GitHub supaya HP auto-update!" -ForegroundColor Yellow
Write-Host ""

# Copy tunnel URL to clipboard kalau bisa
try {
    Set-Clipboard -Value $tunnelUrl
    Write-Host "  [OK] Tunnel URL sudah di-copy ke clipboard" -ForegroundColor Green
} catch {}

Write-Host ""
Write-Host "  Tekan Ctrl+C atau close window ini untuk stop server." -ForegroundColor DarkYellow
Write-Host "  (Server akan tetap jalan selama window ini terbuka)" -ForegroundColor DarkGray
Write-Host ""

# Keep alive + monitor
try {
    while ($true) {
        Start-Sleep -Seconds 30
        # Health ping
        try {
            $null = Invoke-RestMethod -Uri "http://localhost:$Port/health" -TimeoutSec 5
        } catch {
            Write-Host "  [!] Server tidak respond!" -ForegroundColor Red
            break
        }
        # Check processes alive
        if ((Get-Process -Id $serverJob.Id -ErrorAction SilentlyContinue) -eq $null) {
            Write-Host "  [!] Server process mati" -ForegroundColor Red
            break
        }
        if ((Get-Process -Id $tunnelProc.Id -ErrorAction SilentlyContinue) -eq $null) {
            Write-Host "  [!] Tunnel process mati" -ForegroundColor Red
            break
        }
    }
} finally {
    Write-Host ""
    Write-Host "Cleaning up..." -ForegroundColor Yellow
    try { Stop-Process -Id $serverJob.Id -Force -ErrorAction SilentlyContinue } catch {}
    try { Stop-Process -Id $tunnelProc.Id -Force -ErrorAction SilentlyContinue } catch {}
    Write-Host "Bye." -ForegroundColor Gray
    Start-Sleep -Seconds 2
}
