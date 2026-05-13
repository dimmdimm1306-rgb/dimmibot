@echo off
REM ========================================================
REM  ONE-CLICK LAUNCHER — START SEMUA SERVER
REM
REM  Script ini start:
REM    1. AI Server (OpenClaw Node.js, port 20128)
REM    2. GDrive Reader (Python FastAPI, port 20129)
REM    3. Cloudflare tunnel untuk AI
REM    4. Cloudflare tunnel untuk GDrive
REM    5. Update cloudflare-config.json
REM    6. Auto commit + push ke GitHub
REM
REM  HP user tinggal buka app, auto-fetch config baru.
REM ========================================================

cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0mcp_gdrive_filter\launcher_all.ps1"

pause
