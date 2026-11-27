# ==============================================================================
# SOLVIS Auto-Deploy Script for Ubuntu Server (192.168.77.5)
# ==============================================================================

param(
    [string]$ServerIp = "192.168.77.5",
    [string]$User = "solvis",
    [string]$RemoteDir = "/var/www/solvis",
    [string]$ServiceName = "solvis",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$sshTarget = "${User}@${ServerIp}"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   SOLVIS - Automated Deployment to $ServerIp" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Check Server Reachability
Write-Host "[1/5] Checking connectivity to $ServerIp..." -ForegroundColor Yellow
$ping = Test-Connection -ComputerName $ServerIp -Count 1 -Quiet -ErrorAction SilentlyContinue
if (-not $ping) {
    Write-Host "[ERROR] Cannot reach $ServerIp. Ensure your network is connected." -ForegroundColor Red
    exit 1
}
Write-Host "      Server is online." -ForegroundColor Green

# 2. Build and Publish
$publishDir = Join-Path $PSScriptRoot "publish_linux"
if (-not $SkipBuild) {
    Write-Host "[2/5] Building & Publishing Release package for Linux..." -ForegroundColor Yellow
    & dotnet publish -c Release -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] dotnet publish failed with code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "      Build succeeded: $publishDir" -ForegroundColor Green
} else {
    Write-Host "[2/5] Skipping build (using existing $publishDir)..." -ForegroundColor Gray
}

# 3. Transfer files via SCP to staging directory on server
$remoteStaging = "~/stockportal_build"
Write-Host "[3/5] Uploading files to ${sshTarget}:${remoteStaging}/..." -ForegroundColor Yellow

# Ensure remote staging directory exists
& ssh $sshTarget "mkdir -p $remoteStaging"

# Transfer files
& scp -r "${publishDir}\*" "${sshTarget}:${remoteStaging}/"
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] File transfer failed. Check SSH credentials." -ForegroundColor Red
    exit $LASTEXITCODE
}
Write-Host "      Files uploaded successfully." -ForegroundColor Green

# 4. Deploy to RemoteDir and Restart Service
Write-Host "[4/5] Updating $RemoteDir and restarting service..." -ForegroundColor Yellow

$remoteCmd = @"
if [ -d "$RemoteDir" ]; then
    [ -f "$RemoteDir/appsettings.json" ] && cp "$RemoteDir/appsettings.json" /tmp/appsettings.json.bak
    sudo cp -r $remoteStaging/* "$RemoteDir/"
    [ -f /tmp/appsettings.json.bak ] && sudo cp /tmp/appsettings.json.bak "$RemoteDir/appsettings.json"
    sudo chown -R www-data:www-data "$RemoteDir" 2>/dev/null || sudo chown -R ${User}:${User} "$RemoteDir"
    sudo chmod -R 755 "$RemoteDir"
else
    sudo mkdir -p "$RemoteDir"
    sudo cp -r $remoteStaging/* "$RemoteDir/"
    sudo chown -R www-data:www-data "$RemoteDir" 2>/dev/null || sudo chown -R ${User}:${User} "$RemoteDir"
fi

sudo systemctl restart $ServiceName 2>/dev/null || sudo systemctl restart stockportal 2>/dev/null || sudo systemctl restart kestrel-solvis 2>/dev/null || true
sudo systemctl reload nginx 2>/dev/null || true
"@

& ssh -t $sshTarget $remoteCmd
Write-Host "      Remote deployment executed." -ForegroundColor Green

# 5. Health Check
Write-Host "[5/5] Running health check against http://$ServerIp..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
try {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $response = Invoke-WebRequest -Uri "http://$ServerIp" -Method Head -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
    $sw.Stop()
    Write-Host "[SUCCESS] Portal responded with HTTP $($response.StatusCode) in $($sw.ElapsedMilliseconds)ms!" -ForegroundColor Green
    Write-Host "          SOLVIS is live at: http://$ServerIp or http://solvis.local" -ForegroundColor Cyan
} catch {
    Write-Host "[INFO] HTTP probe note: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "Check server status via: ssh $sshTarget 'sudo systemctl status $ServiceName'" -ForegroundColor Gray
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Deployment Finished Successfully!" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan
