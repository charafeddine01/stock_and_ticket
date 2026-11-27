#!/bin/bash
# ==============================================================================
# SOLVIS Hospital System - Master Offline Installation Wizard
# Runs: Static IP Configuration -> Domain & SSL Setup -> Windows Package Export
# ==============================================================================

set -e

# Color helpers
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

clear
echo -e "${CYAN}========================================================================${NC}"
echo -e "${BOLD}${CYAN}          SOLVIS PORTAL - OFFLINE SERVER AUTOMATED SETUP                 ${NC}"
echo -e "${CYAN}========================================================================${NC}"
echo -e "This wizard will configure your Ubuntu server for an isolated offline network:"
echo -e "  1. Configure Static IP, Gateway and DNS via Netplan"
echo -e "  2. Configure server hostname and local domain (${GREEN}solvis.local${NC})"
echo -e "  3. Create Enterprise Root Certificate Authority (Root CA - 10 years)"
echo -e "  4. Issue Server SAN SSL Certificate & configure Nginx for HTTPS (Port 443)"
echo -e "  5. Export Windows Domain GPO certificate files for client computers"
echo -e "${CYAN}========================================================================${NC}"
echo ""

# Check for root / sudo privileges
if [ "$EUID" -ne 0 ]; then
    echo -e "${RED}[ERROR] This wizard must be run as root or with sudo.${NC}"
    echo "Please execute: sudo bash $0"
    exit 1
fi

# Step 1: Execute Static IP Setup
echo -e "${BOLD}${YELLOW}>>> STEP 1: STATIC IP CONFIGURATION <<<${NC}"
if [ -f "$SCRIPT_DIR/set-static-ip.sh" ]; then
    bash "$SCRIPT_DIR/set-static-ip.sh" "$@"
else
    echo -e "${RED}[ERROR] Cannot find set-static-ip.sh in $SCRIPT_DIR${NC}"
    exit 1
fi

echo ""
echo -e "${BOLD}${YELLOW}>>> STEP 2: DOMAIN & SSL CERTIFICATE CONFIGURATION <<<${NC}"
if [ -f "$SCRIPT_DIR/setup-domain-ssl.sh" ]; then
    bash "$SCRIPT_DIR/setup-domain-ssl.sh"
else
    echo -e "${RED}[ERROR] Cannot find setup-domain-ssl.sh in $SCRIPT_DIR${NC}"
    exit 1
fi

# Step 3: Check Kestrel service status
echo ""
echo -e "${BOLD}${YELLOW}>>> STEP 3: APPLICATION SERVICE HEALTH CHECK <<<${NC}"
if systemctl is-active --quiet solvis 2>/dev/null; then
    echo -e "${GREEN}[OK] SOLVIS service is active and running.${NC}"
elif systemctl is-active --quiet stockportal 2>/dev/null; then
    echo -e "${GREEN}[OK] StockPortal service is active and running.${NC}"
else
    echo -e "${YELLOW}[NOTE] Application service is not running yet or named differently.${NC}"
    echo "To start: sudo systemctl start solvis"
fi

# Final Summary
SERVER_IP=$(grep -oP '(?<=STATIC_IP=).*' /etc/solvis/network.conf 2>/dev/null || hostname -I | awk '{print $1}')

echo ""
echo -e "${GREEN}========================================================================${NC}"
echo -e "${BOLD}${GREEN}               OFFLINE SETUP COMPLETED SUCCESSFULLY!                    ${NC}"
echo -e "${GREEN}========================================================================${NC}"
echo -e "Server Access URLs:"
echo -e "  • ${CYAN}https://solvis.local${NC}   (Recommended - with green padlock)"
echo -e "  • ${CYAN}https://solvis${NC}"
echo -e "  • ${CYAN}https://$SERVER_IP${NC}"
echo ""
echo -e "${BOLD}WINDOWS DOMAIN ADMINISTRATOR INSTRUCTIONS:${NC}"
echo -e "1. ${YELLOW}DNS A-Record:${NC} In Windows DNS Manager, add an 'A' record for:"
echo -e "   Name: ${GREEN}solvis${NC} (or ${GREEN}solvis.local${NC}) -> IP: ${GREEN}$SERVER_IP${NC}"
echo ""
echo -e "2. ${YELLOW}Trust Certificate:${NC} Copy the file:"
echo -e "   ${GREEN}/var/www/solvis/windows_domain_setup/solvis-root-ca.crt${NC}"
echo -e "   to your Windows Domain Controller and deploy it via Group Policy (GPO) to:"
echo -e "   'Trusted Root Certification Authorities'."
echo ""
echo -e "   (Or for single PC testing, copy the folder to a PC and run: ${GREEN}install-root-ca.bat${NC})"
echo -e "${GREEN}========================================================================${NC}"
