# One-time setup SSH key based auth to VPS.
# Usage: .\deploy\_setup_ssh.ps1 -Password "your_vps_password"

param(
    [Parameter(Mandatory=$true)]
    [string]$Password,
    [string]$VpsHost = "43.157.226.109",
    [string]$VpsUser = "ubuntu"
)

$pubKeyPath = "$env:USERPROFILE\.ssh\id_ed25519.pub"
if (-not (Test-Path $pubKeyPath)) {
    Write-Host "[X] SSH key tidak ada di $pubKeyPath" -ForegroundColor Red
    exit 1
}
$pubKey = (Get-Content $pubKeyPath -Raw).Trim()

Write-Host "Installing SSH key to $VpsUser@$VpsHost..." -ForegroundColor Cyan

# Use Tencent Lighthouse approach: install sshpass via scoop/chocolatey, or use plink
# Fallback: use Expect-like approach with plink (PuTTY tools)

# Try plink first (comes with PuTTY, commonly installed)
$plink = Get-Command plink -ErrorAction SilentlyContinue
if (-not $plink) {
    # Try install Windows OpenSSH with sshpass-like behavior using .NET SSH
    Write-Host "  Plink not found. Using PowerShell native..." -ForegroundColor Yellow
}

# Encode command to run on VPS
$cmd = "mkdir -p ~/.ssh && chmod 700 ~/.ssh && echo '$pubKey' >> ~/.ssh/authorized_keys && chmod 600 ~/.ssh/authorized_keys && echo INSTALLED_OK"

if ($plink) {
    # Use plink -pw (PuTTY's password option)
    $output = & plink -batch -pw $Password -ssh "$VpsUser@$VpsHost" $cmd 2>&1
    if ($output -match "INSTALLED_OK") {
        Write-Host "[OK] SSH key installed successfully via plink" -ForegroundColor Green
    } else {
        Write-Host "[X] plink output: $output" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "[!] Plink tidak ada. Install PuTTY atau lakukan manual:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. Login ke web console Tencent Lighthouse (klik 'Log In' di dashboard)" -ForegroundColor White
    Write-Host "2. Paste command ini di terminal VPS:" -ForegroundColor White
    Write-Host ""
    Write-Host "mkdir -p ~/.ssh && chmod 700 ~/.ssh" -ForegroundColor Cyan
    Write-Host "echo '$pubKey' >> ~/.ssh/authorized_keys" -ForegroundColor Cyan
    Write-Host "chmod 600 ~/.ssh/authorized_keys" -ForegroundColor Cyan
    Write-Host ""
    exit 2
}

# Test SSH key-based login
Write-Host ""
Write-Host "Testing SSH key login..." -ForegroundColor Cyan
$testCmd = "whoami && uname -a && echo SSH_KEY_WORKS"
$result = & ssh -o StrictHostKeyChecking=no -o BatchMode=yes -o ConnectTimeout=5 "$VpsUser@$VpsHost" $testCmd 2>&1
if ($result -match "SSH_KEY_WORKS") {
    Write-Host "[OK] SSH key auth works!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Dari sekarang kamu bisa SSH tanpa password:" -ForegroundColor Green
    Write-Host "  ssh $VpsUser@$VpsHost" -ForegroundColor Cyan
    Write-Host ""
    $result | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
} else {
    Write-Host "[X] SSH key test failed:" -ForegroundColor Red
    $result | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
}
