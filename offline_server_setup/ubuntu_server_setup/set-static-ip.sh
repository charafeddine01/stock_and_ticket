#!/bin/bash
# ==============================================================================
# SOLVIS Hospital System - Offline Ubuntu Server Setup
# Script 1: Configure Static IP Address via Netplan
# ==============================================================================

set -e

# Color helpers
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${CYAN}==========================================================${NC}"
echo -e "${CYAN}   SOLVIS - Static IP Network Configuration (Netplan)     ${NC}"
echo -e "${CYAN}==========================================================${NC}"

# Check for root / sudo privileges
if [ "$EUID" -ne 0 ]; then
    echo -e "${RED}[ERROR] This script must be run as root or with sudo.${NC}"
    echo "Usage: sudo bash $0"
    exit 1
fi

# Detect available network interfaces (exclude loopback 'lo')
INTERFACES=($(ip -br link show | awk '$1 !~ "lo" {print $1}'))

if [ ${#INTERFACES[@]} -eq 0 ]; then
    echo -e "${RED}[ERROR] No physical or virtual network interfaces detected.${NC}"
    exit 1
fi

DEFAULT_IFACE="${INTERFACES[0]}"
echo -e "${YELLOW}Detected network interface(s):${NC} ${INTERFACES[*]}"
echo -e "${GREEN}Default interface selected:${NC} ${DEFAULT_IFACE}"

# Parse optional command-line arguments:
# --iface <name> --ip <ip> --cidr <prefix> --gateway <gw> --dns <dns1,dns2>
IFACE="$DEFAULT_IFACE"
STATIC_IP=""
CIDR="24"
GATEWAY=""
DNS_SERVERS=""
SEARCH_DOMAIN="solvis.local"

while [[ "$#" -gt 0 ]]; do
    case $1 in
        --iface) IFACE="$2"; shift ;;
        --ip) STATIC_IP="$2"; shift ;;
        --cidr) CIDR="$2"; shift ;;
        --gateway) GATEWAY="$2"; shift ;;
        --dns) DNS_SERVERS="$2"; shift ;;
        --search) SEARCH_DOMAIN="$2"; shift ;;
        *) echo "Unknown parameter: $1"; exit 1 ;;
    esac
    shift
done

# If parameters not provided via CLI, prompt interactively
if [ -z "$STATIC_IP" ]; then
    echo ""
    echo -e "${CYAN}Please enter network parameters for the offline server:${NC}"
    
    # 1. Network Interface
    read -p "Network Interface to configure [$DEFAULT_IFACE]: " input_iface
    IFACE="${input_iface:-$DEFAULT_IFACE}"

    # Current IP for reference
    CURRENT_IP=$(ip -4 addr show "$IFACE" 2>/dev/null | grep -oP '(?<=inet\s)\d+(\.\d+){3}' | head -n 1 || echo "None")
    echo -e "Current IP on $IFACE is: ${YELLOW}$CURRENT_IP${NC}"

    # 2. Static IP Address
    while [ -z "$STATIC_IP" ]; do
        read -p "Enter Target Static IP (e.g. 192.168.10.50): " STATIC_IP
        if [[ ! "$STATIC_IP" =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
            echo -e "${RED}Invalid IP format. Please enter a valid IPv4 address.${NC}"
            STATIC_IP=""
        fi
    done

    # 3. Subnet CIDR Prefix
    read -p "Enter Subnet Prefix CIDR (e.g. 24 for 255.255.255.0) [24]: " input_cidr
    CIDR="${input_cidr:-24}"

    # 4. Gateway
    # Guess gateway based on static IP (e.g. 192.168.10.1)
    SUBNET_PREFIX=$(echo "$STATIC_IP" | cut -d'.' -f1-3)
    GUESSED_GW="${SUBNET_PREFIX}.1"
    read -p "Enter Default Gateway [$GUESSED_GW]: " input_gw
    GATEWAY="${input_gw:-$GUESSED_GW}"

    # 5. DNS Servers
    read -p "Enter DNS Server(s) (comma-separated, usually Windows Domain Controller IP) [$GATEWAY]: " input_dns
    DNS_SERVERS="${input_dns:-$GATEWAY}"

    # 6. Search domain
    read -p "Enter DNS Search Domain [solvis.local]: " input_search
    SEARCH_DOMAIN="${input_search:-solvis.local}"
fi

# Format DNS servers array for Netplan YAML
IFS=',' read -ra DNS_ARRAY <<< "$DNS_SERVERS"
DNS_YAML=""
for dns in "${DNS_ARRAY[@]}"; do
    clean_dns=$(echo "$dns" | tr -d ' ')
    if [ -n "$clean_dns" ]; then
        if [ -z "$DNS_YAML" ]; then
            DNS_YAML="$clean_dns"
        else
            DNS_YAML="$DNS_YAML, $clean_dns"
        fi
    fi
done

echo ""
echo -e "${CYAN}Configuration Summary:${NC}"
echo "----------------------------------------"
echo "Interface:       $IFACE"
echo "Static IP:       $STATIC_IP/$CIDR"
echo "Gateway:         $GATEWAY"
echo "DNS Server(s):   $DNS_YAML"
echo "Search Domain:   $SEARCH_DOMAIN"
echo "----------------------------------------"

# Confirm if running in an interactive session
if [ -t 0 ]; then
    read -p "Apply this network configuration now? (y/n) [y]: " confirm
    confirm="${confirm:-y}"
    if [[ ! "$confirm" =~ ^[Yy]$ ]]; then
        echo -e "${YELLOW}Aborted by user.${NC}"
        exit 0
    fi
fi

# Backup existing Netplan files
BACKUP_DIR="/etc/netplan/backup_$(date +%Y%m%d_%H%M%S)"
mkdir -p "$BACKUP_DIR"
if compgen -G "/etc/netplan/*.yaml" > /dev/null; then
    cp /etc/netplan/*.yaml "$BACKUP_DIR/" 2>/dev/null || true
    echo -e "${GREEN}[OK] Existing Netplan configurations backed up to:${NC} $BACKUP_DIR"
    # Disable old netplan files by renaming them
    for f in /etc/netplan/*.yaml; do
        [ -f "$f" ] && mv "$f" "${f}.disabled"
    done
fi

# Write new Netplan configuration file
NETPLAN_FILE="/etc/netplan/01-solvis-static.yaml"
cat > "$NETPLAN_FILE" <<EOF
# SOLVIS Hospital Portal - Static IP Configuration
# Generated on $(date)
network:
  version: 2
  renderer: networkd
  ethernets:
    $IFACE:
      dhcp4: no
      dhcp6: no
      addresses:
        - $STATIC_IP/$CIDR
      routes:
        - to: default
          via: $GATEWAY
      nameservers:
        addresses: [$DNS_YAML]
        search: [$SEARCH_DOMAIN]
EOF

# Netplan strictly requires 600 permissions
chmod 600 "$NETPLAN_FILE"
echo -e "${GREEN}[OK] Generated new Netplan configuration:${NC} $NETPLAN_FILE"

# Apply netplan
echo -e "${YELLOW}Applying Netplan network configuration...${NC}"
netplan apply

sleep 2

# Verify applied IP address
NEW_IP=$(ip -4 addr show "$IFACE" 2>/dev/null | grep -oP '(?<=inet\s)\d+(\.\d+){3}' | head -n 1 || echo "Unknown")

if [ "$NEW_IP" == "$STATIC_IP" ]; then
    echo -e "${GREEN}[SUCCESS] Static IP $STATIC_IP/$CIDR successfully assigned to $IFACE!${NC}"
else
    echo -e "${YELLOW}[WARNING] IP on $IFACE is currently $NEW_IP. Netplan may need a service restart or link flap.${NC}"
    echo "Run: sudo systemctl restart systemd-networkd"
fi

# Save the configured IP into a local state file for subsequent setup scripts
CONFIG_DIR="/etc/solvis"
mkdir -p "$CONFIG_DIR"
cat > "$CONFIG_DIR/network.conf" <<EOF
STATIC_IP=$STATIC_IP
CIDR=$CIDR
GATEWAY=$GATEWAY
INTERFACE=$IFACE
DNS_SERVERS=$DNS_YAML
SEARCH_DOMAIN=$SEARCH_DOMAIN
EOF
chmod 644 "$CONFIG_DIR/network.conf"

echo -e "${GREEN}[OK] Saved network state to $CONFIG_DIR/network.conf${NC}"
echo -e "${CYAN}==========================================================${NC}"
echo -e "${GREEN}   Static IP Setup Completed!                             ${NC}"
echo -e "${CYAN}==========================================================${NC}"
