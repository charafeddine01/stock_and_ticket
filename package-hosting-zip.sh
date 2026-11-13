#!/usr/bin/env bash
# SOLVIS - Package for hosting.com Shared Hosting Deployment
set -e

ZIP_NAME="solvis-live-demo-hosting.zip"
echo "📦 Packaging SOLVIS Live Demo for hosting.com shared hosting into ${ZIP_NAME}..."

rm -f "${ZIP_NAME}"

# Ensure database directory exists
mkdir -p database

# Create clean zip file with all PHP files, templates, config, wwwroot assets and .htaccess
zip -q -r "${ZIP_NAME}" \
    index.php \
    config.php \
    .htaccess \
    .cpanel.yml \
    maintenance_flowchart.html \
    Hospital_Maintenance_Workflow_Flowchart.pdf \
    app/ \
    templates/ \
    database/ \
    wwwroot/ \
    DEPLOY_HOSTING_COM.md

echo "✅ Package created successfully: ${ZIP_NAME} ($(du -h ${ZIP_NAME} | cut -f1))"
echo "👉 Ready to upload and extract in cPanel File Manager (e.g. public_html/ or subdomain)."
