# SOLVIS Local Domain & DNS Configuration Script
$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "       SOLVIS Local Domain & DNS Setup (solvis.local)     " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Update Windows hosts file on this PC
$hostsPath = "$env:SystemRoot\System32\drivers\etc\hosts"
$hostsContent = Get-Content $hostsPath -Raw

if ($hostsContent -match "solvis\.local") {
    Write-Host "[OK] solvis.local is already defined in hosts file." -ForegroundColor Green
} else {
    Write-Host "[*] Adding solvis.local to hosts file ($hostsPath)..." -ForegroundColor Yellow
    $entry = "`r`n# SOLVIS Intranet Development Domain`r`n127.0.0.1       solvis.local`r`n::1             solvis.local`r`n"
    Add-Content -Path $hostsPath -Value $entry -Encoding utf8
    Write-Host "[OK] Added solvis.local -> 127.0.0.1 and ::1" -ForegroundColor Green
}

# 2. Flush DNS Resolver Cache
Write-Host "[*] Flushing DNS resolver cache..." -ForegroundColor Yellow
ipconfig /flushdns | Out-Null
Write-Host "[OK] DNS cache flushed successfully." -ForegroundColor Green

# 3. Install SSL Certificate into Trusted Root Certification Authorities
$cerPath = Join-Path $PSScriptRoot "solvis-dev.cer"
if (Test-Path $cerPath) {
    Write-Host "[*] Installing updated SSL certificate into Trusted Root store..." -ForegroundColor Yellow
    certutil -addstore -f "Root" $cerPath | Out-Null
    Write-Host "[OK] SSL certificate trusted for solvis.local, localhost, and LAN IP!" -ForegroundColor Green
} else {
    Write-Host "[!] solvis-dev.cer not found at $cerPath" -ForegroundColor DarkYellow
}

# 4. Configure Windows Firewall to allow incoming LAN connections
Write-Host "[*] Configuring Windows Firewall for incoming connections (Port 443, 5174)..." -ForegroundColor Yellow
netsh advfirewall firewall delete rule name="SOLVIS HTTPS (Port 443)" >$null 2>&1
netsh advfirewall firewall add rule name="SOLVIS HTTPS (Port 443)" dir=in action=allow protocol=TCP localport=443 >$null 2>&1

netsh advfirewall firewall delete rule name="SOLVIS HTTP (Port 80)" >$null 2>&1
netsh advfirewall firewall add rule name="SOLVIS HTTP (Port 80)" dir=in action=allow protocol=TCP localport=80 >$null 2>&1

netsh advfirewall firewall delete rule name="SOLVIS HTTP (Port 5174)" >$null 2>&1
netsh advfirewall firewall add rule name="SOLVIS HTTP (Port 5174)" dir=in action=allow protocol=TCP localport=5174 >$null 2>&1

netsh advfirewall firewall delete rule name="SOLVIS DNS (Port 53 UDP)" >$null 2>&1
netsh advfirewall firewall add rule name="SOLVIS DNS (Port 53 UDP)" dir=in action=allow protocol=UDP localport=53 >$null 2>&1
Write-Host "[OK] Windows Firewall rules created for ports 443, 80, 5174, and 53 (DNS)." -ForegroundColor Green

# 5. Display Local LAN IP for Phones & other devices
$lanIp = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notmatch "Loopback" -and $_.IPAddress -notmatch "169\.254" } | Select-Object -First 1).IPAddress
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Setup complete! " -ForegroundColor Green
Write-Host " - On this PC:           https://solvis.local" -ForegroundColor Yellow
Write-Host " - On Phone / Same Wi-Fi: https://$lanIp" -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Cyan
