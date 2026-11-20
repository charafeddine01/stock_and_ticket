# SOLVIS Local Domain & DNS Removal Script
$ErrorActionPreference = "Continue"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "    Removing solvis.local Configuration from this PC      " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Clean Windows hosts file
$hostsPath = "$env:SystemRoot\System32\drivers\etc\hosts"
if (Test-Path $hostsPath) {
    Write-Host "[*] Inspecting Windows hosts file ($hostsPath)..." -ForegroundColor Yellow
    $lines = Get-Content $hostsPath
    $cleanLines = $lines | Where-Object { 
        $_ -notmatch "solvis\.local" -and 
        $_ -notmatch "SOLVIS Intranet Development Domain" -and
        $_ -notmatch "SOLVIS Local Domain"
    }
    
    # Trim trailing empty lines
    while ($cleanLines.Count -gt 0 -and [string]::IsNullOrWhiteSpace($cleanLines[$cleanLines.Count - 1])) {
        $cleanLines = $cleanLines[0..($cleanLines.Count - 2)]
    }

    try {
        Set-Content -Path $hostsPath -Value $cleanLines -Force -Encoding utf8
        Write-Host "[OK] Successfully removed solvis.local from hosts file." -ForegroundColor Green
    } catch {
        Write-Host "[!] Failed to update hosts file: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "    Make sure you run this script as Administrator!" -ForegroundColor Yellow
    }
} else {
    Write-Host "[!] Hosts file not found at $hostsPath" -ForegroundColor DarkYellow
}

# 2. Flush DNS Resolver Cache
Write-Host "[*] Flushing DNS resolver cache..." -ForegroundColor Yellow
try {
    Clear-DnsClientCache -ErrorAction SilentlyContinue
    ipconfig /flushdns | Out-Null
    Write-Host "[OK] DNS cache cleared successfully." -ForegroundColor Green
} catch {
    Write-Host "[!] Could not flush DNS: $($_.Exception.Message)" -ForegroundColor DarkYellow
}

# 3. Remove Certificates
Write-Host "[*] Removing solvis.local certificates from Windows Certificate stores..." -ForegroundColor Yellow
$stores = @("Cert:\LocalMachine\Root", "Cert:\LocalMachine\My", "Cert:\CurrentUser\Root", "Cert:\CurrentUser\My")
foreach ($store in $stores) {
    if (Test-Path $store) {
        $certs = Get-ChildItem $store -ErrorAction SilentlyContinue | Where-Object { 
            $_.Subject -like "*solvis*" -or 
            $_.FriendlyName -like "*solvis*" -or 
            $_.DnsNameList.Unicode -contains "solvis.local"
        }
        foreach ($c in $certs) {
            try {
                Remove-Item $c.PSPath -Force -ErrorAction Stop
                Write-Host "    [OK] Removed cert $($c.Thumbprint) from $store" -ForegroundColor Green
            } catch {
                Write-Host "    [!] Could not remove cert $($c.Thumbprint) from ${store}: $($_.Exception.Message)" -ForegroundColor DarkYellow
            }
        }
    }
}

# Run certutil fallback cleanup
try {
    certutil -delstore "Root" "solvis.local" >$null 2>&1
    certutil -user -delstore "Root" "solvis.local" >$null 2>&1
} catch {}

# 4. Remove Windows Firewall rules
Write-Host "[*] Cleaning up SOLVIS Windows Firewall rules..." -ForegroundColor Yellow
netsh advfirewall firewall delete rule name="SOLVIS HTTPS (Port 443)" >$null 2>&1
netsh advfirewall firewall delete rule name="SOLVIS HTTP (Port 80)" >$null 2>&1
netsh advfirewall firewall delete rule name="SOLVIS HTTP (Port 5174)" >$null 2>&1
netsh advfirewall firewall delete rule name="SOLVIS DNS (Port 53 UDP)" >$null 2>&1
Write-Host "[OK] Windows Firewall rules removed." -ForegroundColor Green

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Removal complete! " -ForegroundColor Green
Write-Host " This PC no longer binds or overrides 'solvis.local'." -ForegroundColor Yellow
Write-Host " It will now resolve 'solvis.local' from your network / router to your other device." -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
