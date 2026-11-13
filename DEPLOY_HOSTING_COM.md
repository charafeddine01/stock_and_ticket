# Deploying SOLVIS Live Demo to hosting.com (cPanel Shared Hosting)

This guide walks you through deploying the **SOLVIS Live Testing Demonstration Environment** to **hosting.com** (cPanel/Apache/LiteSpeed shared hosting with PHP 8.2+ and SQLite or MySQL).

---

## 🌟 Overview & Zero-Configuration Architecture

- **No .NET runtime needed:** Runs completely on standard **PHP 8.2+** available on all shared hosting plans.
- **Zero-Setup Database:** Includes automatic self-initializing SQLite database with pre-seeded hospital data (clinical equipment, departments, staff personas, helpdesk tickets, and spare parts).
- **Optional MySQL Support:** Also includes a complete `database/schema.sql` if you prefer to use cPanel MySQL.
- **Client Live Testing Persona Switcher:** Customers can simulate and switch roles live (Admin, Biomedical Engineer, IT Support Technician, Nurse/Doctor).
- **Bilingual English & Arabic:** Instant 1-click toggle with native RTL/LTR layouts.

---

## 📦 Deployment Archive

To generate the ready-to-upload zip on your local machine, run:
```bash
./package-hosting-zip.sh
```
This produces `solvis-live-demo-hosting.zip` containing all application files, templates, database schemas, `.htaccess`, and assets.

---

## Step 1: Create the Subdomain in cPanel

1. Log into your **hosting.com cPanel** account.
2. In the **Domains** section, click **Domains** (or **Subdomains**).
3. Click **Create A New Domain**:
   - **Domain:** e.g., `solvis.charafeddine.xyz` (or any subdomain)
   - **Document Root:** Enter `public_html/solvis` or `solvis.charafeddine.xyz`.
4. Click **Submit**.

---

## Step 2: Verify PHP 8.2+ in cPanel

1. In cPanel, find **Select PHP Version** (or **MultiPHP Manager**).
2. Ensure your subdomain is set to **PHP 8.2** or **PHP 8.3**.
3. Under **Extensions**, verify standard extensions are enabled:
   - `pdo`
   - `pdo_sqlite` (or `pdo_mysql` if using MySQL)
   - `json`
   - `mbstring`

*(Standard hosting.com cPanel accounts enable all of these by default).*

---

## Step 3: Upload and Extract the Zip

1. In cPanel, open **File Manager**.
2. Navigate to the Document Root directory of your subdomain (e.g. `/public_html/solvis`).
3. Click **Upload** in the top toolbar.
4. Select `solvis-live-demo-hosting.zip` from your computer.
5. Once the upload finishes (green 100%), return to the File Manager.
6. Right-click `solvis-live-demo-hosting.zip` and select **Extract** &rarr; **Extract Files**.
7. Confirm that `index.php`, `.htaccess`, `config.php`, `app/`, `templates/`, and `wwwroot/` are extracted into that directory.
8. *(Optional)* Delete `solvis-live-demo-hosting.zip` to keep the folder clean.

---

## Step 4: Verify Folder Permissions

Ensure the `database/` folder has write permissions so SQLite can create and update `solvis.db`:
- Right-click `database/` in cPanel File Manager &rarr; **Change Permissions** &rarr; set to **755** (or **777** if required by your hosting environment).

---

## Step 5: (Optional) Using MySQL instead of SQLite

If you prefer using cPanel MySQL instead of the default SQLite:
1. In cPanel, create a database and user via **MySQL Databases**.
2. In **phpMyAdmin**, import `database/schema.sql`.
3. Open `config.php` in cPanel File Manager and edit:
   ```php
   define('DB_DRIVER', 'mysql');
   define('DB_MYSQL_HOST', 'localhost');
   define('DB_MYSQL_NAME', 'your_cpanel_solvis_db');
   define('DB_MYSQL_USER', 'your_cpanel_db_user');
   define('DB_MYSQL_PASS', 'your_password');
   ```

---

## Step 6: Test Live Client Environment!

Open your browser and visit:
```
https://solvis.charafeddine.xyz/
```

### What You and Prospective Clients Can Test Live:
1. **Executive KPI Dashboard:** Real-time metrics, urgent tickets alerts, overdue inspections, department filters.
2. **Equipment & Asset Inventory:** Search, filter, view complete custody timeline, and print high-definition QR/Barcode thermal labels.
3. **Helpdesk & Service Tickets:** Create tickets, post diagnostic updates in the discussion timeline, and consume spare parts with real-time stock deductions.
4. **Scheduled & Preventive Maintenance:** External vendor dispatches, overdue inspection certifications, and return-to-hospital workflows.
5. **Reserve Stock & Spare Parts:** Inventory threshold alerts, restock orders, and 1-click commissioning of dual-nature items into independent hospital assets.
6. **Live Persona Switcher:** Switch instantaneously between:
   - **Abbas Al-Amiri** (System Administrator)
   - **Tariq Ziad** (Biomedical Equipment Engineer)
   - **Ahmed Mansour** (IT Support Field Technician)
   - **Dr. Sarah Jenkins** (Lead Cardiologist / End User)
   - **Dr. Elena Rostova** (Radiology Consultant)
7. **Bilingual Toggle:** Switch between English and Arabic with native typography (`Cairo` / `Inter`) and full RTL layouts.
