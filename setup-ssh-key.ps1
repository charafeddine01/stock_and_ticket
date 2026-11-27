# ==============================================================================
# Setup Passwordless SSH Key to Ubuntu Server (192.168.77.5)
# Run once to enable 100% automated passwordless deployment
# ==============================================================================

param(
    [string]$ServerIp = "192.168.77.5",
    [string]$User = "solvis"
)

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   SOLVIS - Setup Passwordless SSH Key ($ServerIp)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$pubKeyPath = "$env:USERPROFILE\.ssh\id_ed25519.pub"
if (-not (Test-Path $pubKeyPath)) {
    Write-Host "Generating new SSH key pair..." -ForegroundColor Yellow
    & ssh-keygen -t ed25519 -N '""' -f "$env:USERPROFILE\.ssh\id_ed25519"
}

$pubKey = (Get-Content $pubKeyPath -Raw).Trim()
Write-Host "Public Key found: $pubKey" -ForegroundColor Gray

Write-Host "`nYou will be prompted for $User's password ONE LAST TIME to install the key:" -ForegroundColor Yellow
$installCmd = "mkdir -p ~/.ssh && chmod 700 ~/.ssh && echo '$pubKey' >> ~/.ssh/authorized_keys && chmod 600 ~/.ssh/authorized_keys && sort -u ~/.ssh/authorized_keys -o ~/.ssh/authorized_keys"

& ssh "$User@$ServerIp" $installCmd

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[SUCCESS] SSH Key successfully registered on $ServerIp!" -ForegroundColor Green
    Write-Host "Testing passwordless login..." -ForegroundColor Yellow
    & ssh -o BatchMode=yes "$User@$ServerIp" "echo '[OK] Connected without password!'"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[VERIFIED] Passwordless SSH is active. Future deployments will run completely unattended." -ForegroundColor Green
    }
} else {
    Write-Host "`n[ERROR] Failed to install SSH key. Please verify password." -ForegroundColor Red
}
