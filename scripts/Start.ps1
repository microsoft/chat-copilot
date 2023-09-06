<#
.SYNOPSIS
Initializes and runs both the backend and frontend for Chat Copilot.
#>

$BackendScript = Join-Path "$PSScriptRoot" 'Start-Backend.ps1'
$FrontendScript = Join-Path "$PSScriptRoot" 'Start-Frontend.ps1'

# Start backend (in new PS process)
Start-Process pwsh -ArgumentList "-command $BackendScript"
#check if the backend is running before proceeding
$backendRunning = $false
while ($backendRunning -eq $false) {
  $backendRunning = Test-NetConnection -ComputerName localhost -Port 40443 -InformationLevel Quiet
  Start-Sleep -Seconds 5
}

# Start frontend (in current PS process)
& $FrontendScript
