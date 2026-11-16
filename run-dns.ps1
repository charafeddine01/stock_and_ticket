# Lightweight DNS Responder for SOLVIS (solvis.local -> LAN IP)
# Forwards all other DNS queries to Google DNS (8.8.8.8)
param(
    [string]$TargetDomain = "solvis.local",
    [string]$UpstreamDns = ""
)

$ErrorActionPreference = "Continue"

# Automatically detect router gateway IP for upstream DNS, fallback to 8.8.8.8
if (-not $UpstreamDns) {
    try {
        $gw = (Get-NetRoute -DestinationPrefix "0.0.0.0/0" -ErrorAction SilentlyContinue | Select-Object -First 1).NextHop
        if ($gw -and $gw -ne "0.0.0.0") {
            $UpstreamDns = $gw
        } else {
            $UpstreamDns = "8.8.8.8"
        }
    } catch {
        $UpstreamDns = "8.8.8.8"
    }
}

# Detect primary LAN IP
$lanIp = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notmatch "Loopback" -and $_.IPAddress -notmatch "169\.254" } | Select-Object -First 1).IPAddress
if (-not $lanIp) { $lanIp = "127.0.0.1" }
$ipBytes = [System.Net.IPAddress]::Parse($lanIp).GetAddressBytes()

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "        SOLVIS Lightweight DNS Server                     " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "[*] Resolving '$TargetDomain' -> $lanIp" -ForegroundColor Green
Write-Host "[*] Forwarding other queries -> $UpstreamDns" -ForegroundColor Gray
Write-Host "[*] Listening on UDP port 53 (Press Ctrl+C to stop)..." -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Cyan

try {
    $udpListener = [System.Net.Sockets.UdpClient]::new(53)
} catch {
    Write-Host "[!] Failed to bind port 53: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "[!] Ensure you run as Administrator and no other DNS service is running." -ForegroundColor Yellow
    exit 1
}

$upstreamEp = [System.Net.IPEndPoint]::new([System.Net.IPAddress]::Parse($UpstreamDns), 53)

try {
    while ($true) {
        $clientEp = [System.Net.IPEndPoint]::new([System.Net.IPAddress]::Any, 0)
        $queryBytes = $udpListener.Receive([ref]$clientEp)

        if ($queryBytes.Length -lt 12) { continue }

        # Check if query contains target domain
        $queryStr = [System.Text.Encoding]::ASCII.GetString($queryBytes)

        if ($queryStr -like "*$($TargetDomain.Replace('.', '*'))*" -or $queryStr -like "*mypc*local*") {
            # Build DNS Response for target domain
            $resp = [System.Collections.Generic.List[byte]]::new()
            # Transaction ID
            $resp.Add($queryBytes[0]); $resp.Add($queryBytes[1])
            # Flags: Standard query response, No error (0x8180)
            $resp.Add(0x81); $resp.Add(0x80)
            # QDCOUNT = 1
            $resp.Add(0x00); $resp.Add(0x01)
            # ANCOUNT = 1
            $resp.Add(0x00); $resp.Add(0x01)
            # NSCOUNT = 0, ARCOUNT = 0
            $resp.Add(0x00); $resp.Add(0x00); $resp.Add(0x00); $resp.Add(0x00)

            # Copy Question Section
            $i = 12
            while ($i -lt $queryBytes.Length -and $queryBytes[$i] -ne 0) { $i++ }
            $questionEnd = [Math]::Min($queryBytes.Length, $i + 5)
            for ($j = 12; $j -lt $questionEnd; $j++) {
                $resp.Add($queryBytes[$j])
            }

            # Answer Section:
            # Pointer to QNAME at offset 12 (0xC00C)
            $resp.Add(0xC0); $resp.Add(0x0C)
            # Type: A (0x0001)
            $resp.Add(0x00); $resp.Add(0x01)
            # Class: IN (0x0001)
            $resp.Add(0x00); $resp.Add(0x01)
            # TTL: 60s
            $resp.Add(0x00); $resp.Add(0x00); $resp.Add(0x00); $resp.Add(0x3C)
            # RDLENGTH: 4
            $resp.Add(0x00); $resp.Add(0x04)
            # RDATA: IP
            foreach ($b in $ipBytes) { $resp.Add($b) }

            $respArr = $resp.ToArray()
            $udpListener.Send($respArr, $respArr.Length, $clientEp) | Out-Null
            Write-Host "[DNS] Resolved '$TargetDomain' -> $lanIp for $($clientEp.Address)" -ForegroundColor Green
        } else {
            # Forward to Upstream DNS (e.g. 8.8.8.8) asynchronously
            try {
                $forwarder = [System.Net.Sockets.UdpClient]::new()
                $forwarder.Client.ReceiveTimeout = 3000
                $forwarder.Send($queryBytes, $queryBytes.Length, $upstreamEp) | Out-Null
                $upstreamClient = [System.Net.IPEndPoint]::new([System.Net.IPAddress]::Any, 0)
                $upstreamResp = $forwarder.Receive([ref]$upstreamClient)
                $forwarder.Close()
                if ($upstreamResp.Length -gt 0) {
                    $udpListener.Send($upstreamResp, $upstreamResp.Length, $clientEp) | Out-Null
                }
            } catch {
                # Ignore upstream timeout
            }
        }
    }
} finally {
    $udpListener.Close()
}
