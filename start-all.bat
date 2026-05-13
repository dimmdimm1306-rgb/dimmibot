@echo off
REM ── One-click launcher: Cloudflare Tunnel + npm run api + auto git commit/push ──
REM Double-click file ini.

title StokBarang Launcher
cd /d "%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0_launcher.ps1"

REM PowerShell sudah handle Read-Host di akhir, jadi tidak perlu pause tambahan.
exit /b %ERRORLEVEL%
