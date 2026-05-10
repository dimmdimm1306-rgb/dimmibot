Write-Host "🚀 Starting OpenClaw API Server..." -ForegroundColor Cyan
Write-Host "📍 Location: OPENCLAW directory" -ForegroundColor Yellow
Write-Host "⚡ Press Ctrl+C to stop the server" -ForegroundColor Yellow
Write-Host ""

Set-Location -Path "d:\!FTTH\Program\UPLOAD DOKUMEN\StokBarangMAUI\OPENCLAW"
npm run api
