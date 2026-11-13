#!/usr/bin/env bash
# SOLVIS - Enhance & Shared Hosting Build & Auto-Sync Script
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "=== SOLVIS Live Demo: Initializing Production & Syncing public_html ==="

# 1. Silence Git initial default branch hints globally on the server
git config --global init.defaultBranch main 2>/dev/null || true

# 2. Ensure internal public_html directory is fully populated
mkdir -p public_html/app
mkdir -p public_html/templates
mkdir -p public_html/database
mkdir -p public_html/wwwroot

cp -f index.php public_html/
cp -f config.php public_html/
cp -f .htaccess public_html/
cp -f maintenance_flowchart.html public_html/ 2>/dev/null || true
cp -f Hospital_Maintenance_Workflow_Flowchart.pdf public_html/ 2>/dev/null || true

cp -rf app/* public_html/app/ 2>/dev/null || true
cp -rf templates/* public_html/templates/ 2>/dev/null || true
cp -rf database/* public_html/database/ 2>/dev/null || true
cp -rf wwwroot/* public_html/wwwroot/ 2>/dev/null || true

# Ensure database directory is writable
chmod -R 775 database 2>/dev/null || true
chmod -R 775 public_html/database 2>/dev/null || true

# 3. Detect Enhance website root (/var/www/<uuid>/) or cPanel root
# When Enhance deploys into /var/www/<uuid>/deploys/<commit>/, the live web root is /var/www/<uuid>/public_html/
CANDIDATE_ROOTS=("../.." ".." "." "$HOME")
for ROOT in "${CANDIDATE_ROOTS[@]}"; do
    ABS_ROOT="$(cd "$ROOT" 2>/dev/null && pwd || true)"
    if [ -n "$ABS_ROOT" ] && [ -d "$ABS_ROOT/public_html" ] && [ "$ABS_ROOT/public_html" != "$SCRIPT_DIR/public_html" ]; then
        echo "-> Detected Enhance live web root at: $ABS_ROOT/public_html"
        
        cp -f index.php "$ABS_ROOT/public_html/" 2>/dev/null || true
        cp -f config.php "$ABS_ROOT/public_html/" 2>/dev/null || true
        cp -f .htaccess "$ABS_ROOT/public_html/" 2>/dev/null || true
        cp -f maintenance_flowchart.html "$ABS_ROOT/public_html/" 2>/dev/null || true
        cp -f Hospital_Maintenance_Workflow_Flowchart.pdf "$ABS_ROOT/public_html/" 2>/dev/null || true
        
        mkdir -p "$ABS_ROOT/public_html/app"
        mkdir -p "$ABS_ROOT/public_html/templates"
        mkdir -p "$ABS_ROOT/public_html/database"
        mkdir -p "$ABS_ROOT/public_html/wwwroot"
        
        cp -rf app/* "$ABS_ROOT/public_html/app/" 2>/dev/null || true
        cp -rf templates/* "$ABS_ROOT/public_html/templates/" 2>/dev/null || true
        cp -rf database/* "$ABS_ROOT/public_html/database/" 2>/dev/null || true
        cp -rf wwwroot/* "$ABS_ROOT/public_html/wwwroot/" 2>/dev/null || true
        
        chmod -R 775 "$ABS_ROOT/public_html/database" 2>/dev/null || true
        chmod -R 755 "$ABS_ROOT/public_html" 2>/dev/null || true
        echo "-> Successfully synchronized all files to: $ABS_ROOT/public_html"
        break
    fi
done

echo "=== SOLVIS build & public_html sync completed successfully ==="
exit 0
