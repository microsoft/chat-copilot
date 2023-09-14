<#
.SYNOPSIS
Initializes and runs both the backend and frontend for Chat Copilot.
#>

$BackendScript = Join-Path "$PSScriptRoot" 'Start-Backend.ps1'
$FrontendScript = Join-Path "$PSScriptRoot" 'Start-Frontend.ps1'

# Start backend (in new PS process)
Start-Process pwsh -ArgumentList "-command $BackendScript"
# check if the backend is running before proceeding
$backendRunning = $false

# get the port from the REACT_APP_BACKEND_URI env variable
$envFilePath = Join-Path $PSScriptRoot '..\webapp\.env'
$envContent = Get-Content -Path $envFilePath
$port = [regex]::Match($envContent, ':(\d+)/').Groups[1].Value

$maxRetries = 5
$retryCount = 0
$retryWait = 5 # set the number of seconds to wait before retrying

# check if the backend is running and check if the retry count is less than the max retries
while ($backendRunning -eq $false -and $retryCount -lt $maxRetries) {
  $retryCount++
  $backendRunning = Test-NetConnection -ComputerName localhost -Port $port -InformationLevel Quiet
  Start-Sleep -Seconds $retryWait
}

# if the backend is running, start the frontend
if ($backendRunning -eq $true) {
  # Start frontend (in current PS process)
  & $FrontendScript
} else { 
  # otherwise, write to the console that the backend is not running and we have exceeded the number of retries and we are exiting
  Write-Host "*************************************************"
  Write-Host "Backend is not running and we have exceeded "
  Write-Host "the maximum number of retries."
  Write-Host ""
  Write-Host "Therefore, we are exiting."
  Write-Host "*************************************************"
}
