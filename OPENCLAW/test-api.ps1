$url = "https://ceo-lotus-races-review.trycloudflare.com/v1/chat/completions"

$body = @{
    messages = @(
        @{
            role = "user"
            content = "Hello, this is a test message. Please respond briefly."
        }
    )
    model = "openrouter/owl-alpha"
    max_tokens = 50
} | ConvertTo-Json

$headers = @{
    "Content-Type" = "application/json"
}

Write-Host "Testing API endpoint: $url" -ForegroundColor Cyan
Write-Host "Sending request..." -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri $url -Method Post -Body $body -Headers $headers -TimeoutSec 30
    Write-Host "`nSuccess! Response:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 10
} catch {
    Write-Host "`nError occurred:" -ForegroundColor Red
    Write-Host $_.Exception.Message
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.BaseStream.Position = 0
        $reader.DiscardBufferedData()
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response body: $responseBody"
    }
}
