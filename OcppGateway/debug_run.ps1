Write-Host "Starting OcppGateway in background..."
$process = Start-Process dotnet -ArgumentList "run --project OcppGateway.csproj --urls http://localhost:5168" -PassThru -NoNewWindow
$procId = $process.Id
Write-Host "Process started with ID: $procId"

Write-Host "Waiting 15 seconds for startup..."
Start-Sleep -Seconds 15

Write-Host "Verifying Port 5168..."
netstat -ano | findstr :5168

Write-Host "Checking Root URL..."
try {
    $res = Invoke-RestMethod -Uri http://localhost:5168 -Method Get
    Write-Host "Response: $res"
}
catch {
    Write-Host "Error reaching URL: $($_.Exception.Message)"
}

Write-Host "Checking recent logs..."
Get-Content logs/gateway-$(Get-Date -Format "yyyyMMdd").log -Tail 5

Write-Host "Process $procId is running."
