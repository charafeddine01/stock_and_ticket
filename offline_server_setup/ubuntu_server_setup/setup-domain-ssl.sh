#!/bin/bash
# ==============================================================================
# SOLVIS Hospital System - Offline Ubuntu Server Setup
# Script 2: Local Domain (solvis.local), Private Root CA, SAN SSL & Nginx HTTPS
# ==============================================================================

set -e

# Color helpers
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${CYAN}==========================================================${NC}"
echo -e "${CYAN}   SOLVIS - Domain (solvis.local) & SSL HTTPS Setup       ${NC}"
echo -e "${CYAN}==========================================================${NC}"

# Check for root / sudo privileges
if [ "$EUID" -ne 0 ]; then
    echo -e "${RED}[ERROR] This script must be run as root or with sudo.${NC}"
    echo "Usage: sudo bash $0"
    exit 1
fi

# Detect Server IP (from argument, state file, or network interface)
SERVER_IP=""
DOMAIN_NAME="solvis.local"
SHORT_NAME="solvis"

while [[ "$#" -gt 0 ]]; do
    case $1 in
        --ip) SERVER_IP="$2"; shift ;;
        --domain) DOMAIN_NAME="$2"; shift ;;
        *) echo "Unknown parameter: $1"; exit 1 ;;
    esac
    shift
done

if [ -z "$SERVER_IP" ] && [ -f "/etc/solvis/network.conf" ]; then
    SERVER_IP=$(grep -oP '(?<=STATIC_IP=).*' /etc/solvis/network.conf || true)
fi

if [ -z "$SERVER_IP" ]; then
    SERVER_IP=$(ip -4 route get 1.1.1.1 2>/dev/null | grep -oP '(?<=src\s)\d+(\.\d+){3}' || true)
fi

if [ -z "$SERVER_IP" ]; then
    SERVER_IP=$(hostname -I | awk '{print $1}')
fi

# If still not found, prompt
if [ -z "$SERVER_IP" ]; then
    read -p "Enter Server Static IP address (e.g. 192.168.10.50): " SERVER_IP
fi

echo -e "${GREEN}Configuring for Server IP:${NC} $SERVER_IP"
echo -e "${GREEN}Configuring for Domain:${NC}    $DOMAIN_NAME ($SHORT_NAME)"

# ------------------------------------------------------------------------------
# 1. Configure Hostname and /etc/hosts
# ------------------------------------------------------------------------------
echo -e "\n${YELLOW}[1/5] Configuring Hostname and /etc/hosts...${NC}"

hostnamectl set-hostname "$SHORT_NAME"

# Remove any obsolete solvis entries in /etc/hosts
sed -i '/solvis\.local/d' /etc/hosts
sed -i '/[[:space:]]solvis[[:space:]]*$/d' /etc/hosts

# Append clean mapping
echo "$SERVER_IP $DOMAIN_NAME $SHORT_NAME" >> /etc/hosts
echo "127.0.0.1 $DOMAIN_NAME $SHORT_NAME" >> /etc/hosts

echo -e "${GREEN}[OK] Hostname set to '$SHORT_NAME' and mapped in /etc/hosts${NC}"

# ------------------------------------------------------------------------------
# 2. Generate Private Enterprise Root Certificate Authority (CA)
# ------------------------------------------------------------------------------
echo -e "\n${YELLOW}[2/5] Creating Private Root Certificate Authority (CA)...${NC}"

SSL_DIR="/etc/ssl/solvis"
mkdir -p "$SSL_DIR"
chmod 700 "$SSL_DIR"

ROOT_KEY="$SSL_DIR/solvis-root-ca.key"
ROOT_CRT="$SSL_DIR/solvis-root-ca.crt"

# Generate 4096-bit Root CA Private Key if it doesn't already exist
if [ ! -f "$ROOT_KEY" ]; then
    echo "      Generating 4096-bit RSA Root CA key..."
    openssl genrsa -out "$ROOT_KEY" 4096 2>/dev/null
    chmod 400 "$ROOT_KEY"
fi

# Generate 10-Year Self-Signed Root CA Certificate
if [ ! -f "$ROOT_CRT" ]; then
    echo "      Generating 10-Year Root CA Certificate..."
    openssl req -x509 -new -nodes -key "$ROOT_KEY" -sha256 -days 3650 \
        -out "$ROOT_CRT" \
        -subj "/C=SA/ST=Riyadh/O=SOLVIS Hospital Network/OU=IT Infrastructure/CN=SOLVIS Internal Root CA" \
        -addext "basicConstraints=critical,CA:TRUE" \
        -addext "keyUsage=critical,digitalSignature,cRLSign,keyCertSign" 2>/dev/null
    chmod 644 "$ROOT_CRT"
    echo -e "${GREEN}[OK] Root CA Certificate created (valid for 10 years):${NC} $ROOT_CRT"
else
    echo -e "${GREEN}[OK] Existing Root CA Certificate retained:${NC} $ROOT_CRT"
fi

# ------------------------------------------------------------------------------
# 3. Generate Server Certificate with SAN (Subject Alternative Names)
# ------------------------------------------------------------------------------
echo -e "\n${YELLOW}[3/5] Issuing Server Certificate with SAN for $DOMAIN_NAME & $SERVER_IP...${NC}"

SERVER_KEY="$SSL_DIR/solvis.key"
SERVER_CSR="$SSL_DIR/solvis.csr"
SERVER_CRT="$SSL_DIR/solvis.crt"
SERVER_BUNDLE="$SSL_DIR/solvis-bundle.crt"
SAN_CONF="$SSL_DIR/openssl-san.cnf"

# Create OpenSSL configuration file with SAN extensions
cat > "$SAN_CONF" <<EOF
[req]
default_bits        = 2048
distinguished_name  = req_distinguished_name
req_extensions      = req_ext
prompt              = no

[req_distinguished_name]
C                   = SA
ST                  = Riyadh
O                   = SOLVIS Hospital Portal
OU                  = Medical Informatics
CN                  = $DOMAIN_NAME

[req_ext]
subjectAltName      = @alt_names

[v3_req]
basicConstraints    = CA:FALSE
keyUsage            = critical, digitalSignature, keyEncipherment
extendedKeyUsage    = serverAuth, clientAuth
subjectAltName      = @alt_names

[alt_names]
DNS.1               = $DOMAIN_NAME
DNS.2               = $SHORT_NAME
DNS.3               = localhost
IP.1                = $SERVER_IP
IP.2                = 127.0.0.1
EOF

# Generate 2048-bit Server Private Key
openssl genrsa -out "$SERVER_KEY" 2048 2>/dev/null
chmod 400 "$SERVER_KEY"

# Generate CSR
openssl req -new -key "$SERVER_KEY" -out "$SERVER_CSR" -config "$SAN_CONF" 2>/dev/null

# Sign Server Certificate using our Root CA (valid for 5 years = 1825 days)
openssl x509 -req -in "$SERVER_CSR" \
    -CA "$ROOT_CRT" -CAkey "$ROOT_KEY" -CAcreateserial \
    -out "$SERVER_CRT" -days 1825 -sha256 \
    -extfile "$SAN_CONF" -extensions v3_req 2>/dev/null
chmod 644 "$SERVER_CRT"

# Create full certificate chain bundle (Server Cert + Root CA Cert)
cat "$SERVER_CRT" "$ROOT_CRT" > "$SERVER_BUNDLE"
chmod 644 "$SERVER_BUNDLE"

echo -e "${GREEN}[OK] Server SSL Certificate signed by Root CA:${NC} $SERVER_BUNDLE"

# ------------------------------------------------------------------------------
# 4. Configure Nginx Reverse Proxy (HTTP 80 -> HTTPS 443 -> Kestrel 5000)
# ------------------------------------------------------------------------------
echo -e "\n${YELLOW}[4/5] Configuring Nginx Web Server for HTTPS...${NC}"

NGINX_CONF="/etc/nginx/sites-available/solvis"
mkdir -p /etc/nginx/sites-available /etc/nginx/sites-enabled

cat > "$NGINX_CONF" <<EOF
# SOLVIS Hospital Portal - Nginx Reverse Proxy
# Generated by SOLVIS offline setup on $(date)

# 1. HTTP Server - Redirect all port 80 traffic to HTTPS
server {
    listen 80;
    listen [::]:80;
    server_name $DOMAIN_NAME $SHORT_NAME $SERVER_IP localhost;

    # Permanent redirect to secure HTTPS
    return 301 https://\$host\$request_uri;
}

# 2. HTTPS Server - SSL Port 443
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name $DOMAIN_NAME $SHORT_NAME $SERVER_IP localhost;

    # SSL Certificate Configuration
    ssl_certificate $SERVER_BUNDLE;
    ssl_certificate_key $SERVER_KEY;

    # Modern TLS Security Configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5:!3DES:!CAMELLIA:!SRP:!PSK;
    ssl_prefer_server_ciphers on;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 1d;
    ssl_session_tickets off;

    # Security Headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # Allow up to 100MB for medical asset attachments and telemetry reports
    client_max_body_size 100M;

    # Proxy to Kestrel backend (ASP.NET Core)
    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;

        # WebSocket and Connection Headers
        proxy_set_header   Upgrade \$http_upgrade;
        proxy_set_header   Connection "upgrade";
        proxy_cache_bypass \$http_upgrade;

        # Client Identity Headers
        proxy_set_header   Host \$host;
        proxy_set_header   X-Real-IP \$remote_addr;
        proxy_set_header   X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto \$scheme;
        proxy_set_header   X-Forwarded-Host \$host;
        proxy_set_header   X-Forwarded-Port \$server_port;

        # Timeouts for heavy reports
        proxy_connect_timeout 60s;
        proxy_send_timeout    120s;
        proxy_read_timeout    120s;
    }
}
EOF

# Enable solvis site
ln -sf "$NGINX_CONF" /etc/nginx/sites-enabled/solvis

# Disable conflicting default Nginx site if present
if [ -f /etc/nginx/sites-enabled/default ]; then
    rm -f /etc/nginx/sites-enabled/default
    echo "      Removed default Nginx placeholder site."
fi

# Test Nginx syntax
echo "      Testing Nginx configuration syntax..."
nginx -t

# Restart / Reload Nginx
systemctl restart nginx || systemctl reload nginx
echo -e "${GREEN}[OK] Nginx is running and listening on Port 80 (HTTP) & Port 443 (HTTPS)${NC}"

# ------------------------------------------------------------------------------
# 5. Export Windows Domain Deployment Package
# ------------------------------------------------------------------------------
echo -e "\n${YELLOW}[5/5] Exporting Windows Domain Trust Package...${NC}"

# Destination directory for Windows admin package
EXPORT_DIR="/var/www/solvis/windows_domain_setup"
LOCAL_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/windows_domain_setup"

mkdir -p "$EXPORT_DIR"

# Copy Root Certificate (.crt and .cer format)
cp "$ROOT_CRT" "$EXPORT_DIR/solvis-root-ca.crt"
# Convert CRT to DER/CER format for Windows
openssl x509 -outform der -in "$ROOT_CRT" -out "$EXPORT_DIR/solvis-root-ca.cer" 2>/dev/null || true

# If running from repo directory, copy into repo's windows_domain_setup folder too
if [ -d "$LOCAL_SCRIPT_DIR" ]; then
    cp "$ROOT_CRT" "$LOCAL_SCRIPT_DIR/solvis-root-ca.crt"
    cp "$EXPORT_DIR/solvis-root-ca.cer" "$LOCAL_SCRIPT_DIR/solvis-root-ca.cer" 2>/dev/null || true
fi

chmod -R 755 "$EXPORT_DIR"

echo -e "${GREEN}[OK] Windows Domain files exported to:${NC} $EXPORT_DIR"
echo "      - solvis-root-ca.crt (PEM format)"
echo "      - solvis-root-ca.cer (Windows DER format)"

echo ""
echo -e "${CYAN}==========================================================${NC}"
echo -e "${GREEN}   Domain & SSL Configuration Completed Successfully!     ${NC}"
echo -e "${CYAN}==========================================================${NC}"
echo -e "Access URLs for your offline network:"
echo -e "  • ${GREEN}https://$DOMAIN_NAME${NC}"
echo -e "  • ${GREEN}https://$SHORT_NAME${NC}"
echo -e "  • ${GREEN}https://$SERVER_IP${NC}"
echo ""
echo -e "${YELLOW}Next Step for Windows Domain Admin:${NC}"
echo -e "Copy '${GREEN}$EXPORT_DIR/solvis-root-ca.crt${NC}' to your Windows Domain Controller"
echo -e "and deploy via GPO (or run ${GREEN}install-root-ca.bat${NC} as Administrator on client PCs)."
echo -e "${CYAN}==========================================================${NC}"
