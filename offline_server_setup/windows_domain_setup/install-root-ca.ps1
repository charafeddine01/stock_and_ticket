# ==============================================================================
# SOLVIS Hospital System - Windows Client Root Certificate Installer
# Run as Administrator to install solvis-root-ca.crt into LocalMachine\Root
# ==============================================================================

# Ensure script is running with elevated administrator privileges
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Warning "Administrator rights required! Re-launching as Administrator..."
    Start-Process powershell -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    exit
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$certPath = Join-Path $scriptDir "solvis-root-ca.crt"
if (-not (Test-Path $certPath)) {
    $certPath = Join-Path $scriptDir "solvis-root-ca.cer"
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   SOLVIS - Root Certificate Installer for Windows        " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

if (-not (Test-Path $certPath)) {
    Write-Host "[ERROR] Cannot find solvis-root-ca.crt or solvis-root-ca.cer in $scriptDir" -ForegroundColor Red
    Write-Host "Please place the exported root certificate in the same directory as this script." -ForegroundColor Yellow
    pause
    exit 1
}

try {
    Write-Host "Installing Root Certificate into Trusted Root Certification Authorities..." -ForegroundColor Yellow
    
    # Import certificate into Local Machine Root store
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certPath)
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store([System.Security.Cryptography.X509Certificates.StoreName]::Root, [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine)
    $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    $store.Add($cert)
    $store.Close()

    Write-Host "[SUCCESS] SOLVIS Internal Root CA installed successfully!" -ForegroundColor Green
    Write-Host "          Subject: $($cert.Subject)" -ForegroundColor Gray
    Write-Host "          Thumbprint: $($cert.Thumbprint)" -ForegroundColor Gray
    Write-Host "          Valid Until: $($cert.NotAfter.ToString('yyyy-MM-dd'))" -ForegroundColor Gray
    Write-Host ""
    Write-Host "All browsers on this computer (Chrome, Edge, Firefox) will now trust https://solvis.local" -ForegroundColor Cyan
}
catch {
    Write-Host "[ERROR] Failed to install certificate: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
