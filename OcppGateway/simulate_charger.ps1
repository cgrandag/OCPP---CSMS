$ErrorActionPreference = "Stop"

$chargePointId = "123456"
if ($args.Count -gt 0) { $chargePointId = $args[0] }

$url = "ws://localhost:5168/$chargePointId"
$uri = [System.Uri]$url

[Console]::WriteLine("CONNECTING to $url ...")

try {
    $ws = New-Object System.Net.WebSockets.ClientWebSocket
    $ws.Options.AddSubProtocol("ocpp1.6j")
    $connectCts = New-Object System.Threading.CancellationTokenSource(10000)
    $ws.ConnectAsync($uri, $connectCts.Token).Wait()
    
    [Console]::WriteLine("CONNECTED to $url successfully!")

    $loopToken = [System.Threading.CancellationToken]::None

    function Send-Message([int]$type, [string]$action, [hashtable]$payload, [string]$msgId = "") {
        if ([string]::IsNullOrEmpty($msgId)) { $msgId = [Guid]::NewGuid().ToString() }
        
        $msg = @()
        if ($type -eq 2) { $msg = @(2, $msgId, $action, $payload) }
        elseif ($type -eq 3) { $msg = @(3, $msgId, $payload) }
        
        $json = $msg | ConvertTo-Json -Depth 5 -Compress
        $buffer = [System.Text.Encoding]::UTF8.GetBytes($json)
        $segment = [ArraySegment[byte]]::new($buffer)
        $ws.SendAsync($segment, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $loopToken).Wait()
        Write-Output "-> $json"
    }

    Send-Message 2 "BootNotification" @{ chargePointVendor = "SimulatedVendor"; chargePointModel = "SimulatedModel" }
    Start-Sleep -Seconds 1
    
    Send-Message 2 "StatusNotification" @{ connectorId = 1; errorCode = "NoError"; status = "Available" }
    
    $rcvBuffer = [byte[]]::new(4096)
    $rcvSegment = [ArraySegment[byte]]::new($rcvBuffer)
    $receiveTask = $null
    
    $isCharging = $false
    $txId = 0
    $lastHeartbeat = [DateTime]::UtcNow
    $lastMeter = [DateTime]::UtcNow
    $meterStart = 0

    Write-Output "Monitoring... Press Ctrl+C to stop."

    while ($ws.State -eq 'Open') {
        if (([DateTime]::UtcNow - $lastHeartbeat).TotalSeconds -ge 60) {
            Send-Message 2 "Heartbeat" @{}
            $lastHeartbeat = [DateTime]::UtcNow
        }

        if ($isCharging -and ([DateTime]::UtcNow - $lastMeter).TotalSeconds -ge 2) {
            $meterStart += 100
            
            $sampledValue = @(
                @{ measurand = "Energy.Active.Import.Register"; value = $meterStart.ToString(); unit = "Wh" },
                @{ measurand = "Power.Active.Import"; value = "7.4"; unit = "kW" },
                @{ measurand = "Current.Import"; value = "32.0"; unit = "A" }
            )
            
            $meterPayload = @{
                connectorId   = 1
                transactionId = $txId
                meterValue    = @(
                    @{
                        timestamp    = [DateTime]::UtcNow.ToString("o")
                        sampledValue = $sampledValue
                    }
                )
            }
            Send-Message 2 "MeterValues" $meterPayload
            $lastMeter = [DateTime]::UtcNow
        }

        if ($null -eq $receiveTask) {
            $receiveTask = $ws.ReceiveAsync($rcvSegment, $loopToken)
        }

        $timeoutTask = [System.Threading.Tasks.Task]::Delay(1000)
        $completedTask = [System.Threading.Tasks.Task]::WhenAny($receiveTask, $timeoutTask).Result
        
        if ($completedTask -eq $receiveTask) {
            $result = $receiveTask.Result
            $receiveTask = $null
            
            if ($result.MessageType -eq 'Close') {
                break;
            }
            
            $responseJson = [System.Text.Encoding]::UTF8.GetString($rcvBuffer, 0, $result.Count)
            Write-Output "<- $responseJson"
            
            try {
                $parsed = $responseJson | ConvertFrom-Json
                if ($parsed.Count -ge 4 -and $parsed[0] -eq 2) {
                    $reqId = $parsed[1]
                    $action = $parsed[2]
                    $payload = $parsed[3]
                    
                    if ($action -eq "RemoteStartTransaction") {
                        Write-Output "Received RemoteStartTransaction!"
                        Send-Message 3 "" @{ status = "Accepted" } $reqId
                        
                        $txId = [Math]::Floor([int](Get-Random -Minimum 1000 -Maximum 9999))
                        Send-Message 2 "StartTransaction" @{ connectorId = 1; idTag = "SimUser"; timestamp = [DateTime]::UtcNow.ToString("o"); meterStart = $meterStart }
                        
                        $isCharging = $true
                        Start-Sleep -Seconds 1
                        Send-Message 2 "StatusNotification" @{ connectorId = 1; errorCode = "NoError"; status = "Charging" }
                    }
                    elseif ($action -eq "RemoteStopTransaction") {
                        Write-Output "Received RemoteStopTransaction!"
                        Send-Message 3 "" @{ status = "Accepted" } $reqId
                        
                        $isCharging = $false
                        Send-Message 2 "StopTransaction" @{ transactionId = $txId; timestamp = [DateTime]::UtcNow.ToString("o"); meterStop = $meterStart }
                        
                        Send-Message 2 "StatusNotification" @{ connectorId = 1; errorCode = "NoError"; status = "Finishing" }
                        Start-Sleep -Seconds 2
                        Send-Message 2 "StatusNotification" @{ connectorId = 1; errorCode = "NoError"; status = "Available" }
                    }
                    elseif ($action -eq "Reset") {
                        Write-Output "Received Reset!"
                        Send-Message 3 "" @{ status = "Accepted" } $reqId
                        break;
                    }
                }
            }
            catch {
            }
        }
    }
}
catch {
    Write-Output "ERROR: $_"
}
finally {
    if ($ws -and $ws.State -eq 'Open') {
        $ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, "Done", $loopToken).Wait()
    }
    if ($ws) { $ws.Dispose() }
}
