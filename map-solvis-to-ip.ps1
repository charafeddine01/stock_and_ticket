# PowerShell script to map solvis.local to 192.168.77.3 on Windows
# Requires Run as Administrator

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "[ERROR] Please run this script in an elevated PowerShell (Run as Administrator)!" -ForegroundColor Red
    pause
    exit 1
}

$hostsPath = "$env:WINDIR\System32\drivers\etc\hosts"
$ip = "192.168.77.3"
$hostname = "solvis.local"

# Remove any existing solvis.local lines
$lines = Get-Content -Path $hostsPath -ErrorAction Stop | Where-Object { $_ -notmatch "solvis\.local" }
Set-Content -Path $hostsPath -Value $lines -Force

# Add the new mapping
Add-Content -Path $hostsPath -Value "$ip $hostname"

# Flush DNS
Clear-DnsClientCache

Write-Host "[SUCCESS] $hostname mapped to $ip in $hostsPath" -ForegroundColor Green
Write-Host "[SUCCESS] DNS cache cleared." -ForegroundColor Green
Write-Host "You can now open http://$hostname in your browser." -ForegroundColor Cyan
