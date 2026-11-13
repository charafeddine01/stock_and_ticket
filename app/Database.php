<?php
/**
 * SOLVIS Database Engine & Schema Initializer
 * Auto-initializes MySQL/MariaDB or SQLite database with rich hospital sample data on first run
 */

require_once __DIR__ . '/../config.php';

class Database {
    private static ?PDO $pdo = null;

    public static function getConnection(): PDO {
        if (self::$pdo !== null) {
            return self::$pdo;
        }

        // Try MySQL first if configured
        if (DB_DRIVER === 'mysql') {
            try {
                $dsn = sprintf(
                    'mysql:host=%s;port=%d;dbname=%s;charset=%s',
                    DB_MYSQL_HOST,
                    DB_MYSQL_PORT,
                    DB_MYSQL_NAME,
                    DB_MYSQL_CHARSET
                );
                self::$pdo = new PDO($dsn, DB_MYSQL_USER, DB_MYSQL_PASS, [
                    PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
                    PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
                    PDO::ATTR_EMULATE_PREPARES => false,
                ]);

                // Check if MySQL database has tables; if empty, auto-import schema.sql
                $check = self::$pdo->query("SHOW TABLES LIKE 'departments'");
                if ($check->rowCount() === 0) {
                    self::initMySqlSchemaAndSeed(self::$pdo);
                }

                return self::$pdo;
            } catch (PDOException $e) {
                // If MySQL fails (e.g. offline, local development, or access error), log and fallback to SQLite
                error_log("SOLVIS: MySQL connection failed (" . $e->getMessage() . "). Falling back to SQLite.");
            }
        }

        // SQLite Driver (Zero-config fallback)
        try {
            $dbDir = dirname(DB_SQLITE_PATH);
            if (!is_dir($dbDir)) {
                @mkdir($dbDir, 0777, true);
            }

            $isNewDb = !file_exists(DB_SQLITE_PATH) || filesize(DB_SQLITE_PATH) === 0;

            self::$pdo = new PDO('sqlite:' . DB_SQLITE_PATH, null, null, [
                PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
                PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            ]);

            // Enable foreign keys and WAL mode for high concurrency
            self::$pdo->exec('PRAGMA foreign_keys = ON;');
            self::$pdo->exec('PRAGMA journal_mode = WAL;');

            if ($isNewDb) {
                self::initSqliteSchemaAndSeed(self::$pdo);
            }

            return self::$pdo;
        } catch (PDOException $e) {
            die("Database Error: " . htmlspecialchars($e->getMessage()));
        }
    }

    public static function initMySqlSchemaAndSeed(PDO $db): void {
        $schemaFile = __DIR__ . '/../database/schema.sql';
        if (file_exists($schemaFile)) {
            $sql = file_get_contents($schemaFile);
            if (!empty($sql)) {
                $db->exec($sql);
            }
        }
    }

    public static function initSqliteSchemaAndSeed(PDO $db): void {
        $db->beginTransaction();

        try {
            // 1. Core Tables DDL
            $db->exec("
                CREATE TABLE IF NOT EXISTS departments (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    name_ar TEXT,
                    headUserID INTEGER
                );

                CREATE TABLE IF NOT EXISTS rooms (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    roomNo INTEGER NOT NULL,
                    floor INTEGER NOT NULL,
                    name TEXT,
                    name_ar TEXT,
                    department_id INTEGER
                );

                CREATE TABLE IF NOT EXISTS suppliers (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    contactPerson TEXT,
                    email TEXT,
                    phone TEXT,
                    address TEXT,
                    notes TEXT
                );

                CREATE TABLE IF NOT EXISTS custody_type (
                    id INTEGER PRIMARY KEY,
                    type TEXT NOT NULL,
                    type_ar TEXT
                );

                CREATE TABLE IF NOT EXISTS ticket_status (
                    id INTEGER PRIMARY KEY,
                    status TEXT NOT NULL,
                    status_ar TEXT
                );

                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    fullName TEXT NOT NULL,
                    domainUser TEXT NOT NULL,
                    role TEXT NOT NULL,
                    departmentId INTEGER,
                    email TEXT,
                    phone TEXT,
                    maintenanceTeam TEXT DEFAULT 'IT',
                    isActive INTEGER DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS asset_categories (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    name_ar TEXT,
                    code TEXT,
                    description TEXT,
                    icon TEXT DEFAULT 'fa-boxes-stacked'
                );

                CREATE TABLE IF NOT EXISTS assets (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    hospitalNo TEXT NOT NULL UNIQUE,
                    serialNo TEXT,
                    brand TEXT,
                    modelNo TEXT,
                    price REAL DEFAULT 0,
                    type TEXT NOT NULL,
                    status TEXT DEFAULT 'In Service',
                    supplierId INTEGER,
                    roomId INTEGER,
                    userId INTEGER,
                    custodyTypeId INTEGER DEFAULT 1,
                    specs TEXT,
                    buyingDate TEXT,
                    warrantyTill TEXT,
                    needsPeriodic INTEGER DEFAULT 0,
                    periodicInterval INTEGER DEFAULT 90,
                    lastPeriodic TEXT,
                    nextPeriodic TEXT,
                    maintenanceTeam TEXT DEFAULT 'IT',
                    categoryId INTEGER,
                    calibrationDate TEXT,
                    calibrationDue TEXT,
                    calibrationCertificate TEXT
                );

                CREATE TABLE IF NOT EXISTS asset_history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    assetId INTEGER NOT NULL,
                    action TEXT NOT NULL,
                    notes TEXT,
                    changedBy TEXT,
                    timestamp TEXT NOT NULL,
                    oldRoomId INTEGER,
                    newRoomId INTEGER,
                    oldUserId INTEGER,
                    newUserId INTEGER
                );

                CREATE TABLE IF NOT EXISTS tickets (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    title TEXT NOT NULL,
                    description TEXT,
                    priority TEXT DEFAULT 'Medium',
                    statusId INTEGER DEFAULT 1,
                    createdByUserId INTEGER NOT NULL,
                    assignedToUserId INTEGER,
                    departmentId INTEGER,
                    assetId INTEGER,
                    maintenanceTeam TEXT DEFAULT 'IT',
                    createdAt TEXT NOT NULL,
                    updatedAt TEXT NOT NULL,
                    closedAt TEXT,
                    resolution TEXT
                );

                CREATE TABLE IF NOT EXISTS ticket_messages (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ticketId INTEGER NOT NULL,
                    userId INTEGER NOT NULL,
                    message TEXT NOT NULL,
                    isInternal INTEGER DEFAULT 0,
                    createdAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS asset_maintenances (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    assetId INTEGER NOT NULL,
                    ticketId INTEGER,
                    maintenanceType TEXT NOT NULL,
                    vendorId INTEGER,
                    dispatchedDate TEXT,
                    returnedDate TEXT,
                    cost REAL DEFAULT 0,
                    description TEXT,
                    status TEXT DEFAULT 'Active'
                );

                CREATE TABLE IF NOT EXISTS spare_parts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    partNo TEXT NOT NULL UNIQUE,
                    name TEXT NOT NULL,
                    name_ar TEXT,
                    category TEXT NOT NULL,
                    quantity INTEGER DEFAULT 0,
                    minThreshold INTEGER DEFAULT 5,
                    unitCost REAL DEFAULT 0,
                    location TEXT,
                    supplierId INTEGER,
                    maintenanceTeam TEXT DEFAULT 'IT',
                    canBeAsset INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS spare_part_transactions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    sparePartId INTEGER NOT NULL,
                    ticketId INTEGER,
                    quantity INTEGER NOT NULL,
                    type TEXT NOT NULL,
                    notes TEXT,
                    createdAt TEXT NOT NULL,
                    performedByUserId INTEGER
                );

                CREATE TABLE IF NOT EXISTS notifications (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    userID INTEGER NOT NULL,
                    ticketID INTEGER,
                    title TEXT NOT NULL,
                    message TEXT NOT NULL,
                    type TEXT DEFAULT 'info',
                    linkUrl TEXT,
                    isRead INTEGER DEFAULT 0,
                    createdAt TEXT NOT NULL
                );
            ");

            // 2. Seed Lookups
            $db->exec("
                INSERT OR IGNORE INTO custody_type (id, type, type_ar) VALUES
                (1, 'Personal Custody', 'عهدة شخصية'),
                (2, 'Department / Shared', 'عهدة قسم / مشتركة'),
                (3, 'Supplier / External Maintenance', 'لدى المورد / صيانة خارجية');

                INSERT OR IGNORE INTO ticket_status (id, status, status_ar) VALUES
                (1, 'New', 'جديدة'),
                (2, 'In Progress', 'قيد المعالجة'),
                (3, 'Pending Spare Parts', 'بانتظار قطع الغيار'),
                (4, 'Resolved', 'تم الإصلاح والحل'),
                (5, 'Closed', 'مغلقة نهائياً'),
                (6, 'Under External Maintenance', 'قيد الصيانة الخارجية');

                INSERT OR IGNORE INTO asset_categories (id, name, name_ar, code, description, icon) VALUES
                (1, 'Biomedical & Clinical Equipment', 'الأجهزة الطبية والحيوية', 'BIO', 'Patient monitors, defibrillators, ultrasound, and vital sign units', 'fa-heart-pulse'),
                (2, 'IT Infrastructure & Computing', 'البنية التحتية والشبكات', 'IT', 'Servers, routers, switches, enterprise workstations, and laptops', 'fa-laptop-code'),
                (3, 'Imaging & Diagnostics', 'التصوير الطبي والأشعة', 'RAD', 'X-Ray, CT scanners, MRI systems, and PACS diagnostic displays', 'fa-radiation'),
                (4, 'Facilities & HVAC Plant', 'المرافق والمباني والتكييف', 'FAC', 'Medical gas regulators, chillers, UPS power distribution, autoclaves', 'fa-fan');

                INSERT OR IGNORE INTO departments (id, name, name_ar, headUserID) VALUES
                (1, 'Information Technology', 'تقنية المعلومات', 1),
                (2, 'Biomedical Engineering', 'الهندسة الطبية الحيوية', 5),
                (3, 'Cardiology', 'قسم أمراض القلب', 3),
                (4, 'Radiology & Imaging', 'الأشعة والتصوير التشخيصي', 4),
                (5, 'Intensive Care Unit (ICU)', 'العناية المركزة (ICU)', 3),
                (6, 'Facilities & Maintenance', 'الصيانة العامة والمرافق', 2);

                INSERT OR IGNORE INTO rooms (id, roomNo, floor, name, name_ar, department_id) VALUES
                (1, 101, 1, 'Cardiology Clinic A', 'عيادة القلب (أ)', 3),
                (2, 105, 1, 'Main Server Data Center', 'مركز البيانات الرئيسي (IT)', 1),
                (3, 201, 2, 'ICU Pod 1 - High Dependency', 'العناية المركزة - الجناح 1', 5),
                (4, 204, 2, 'Biomedical Calibration Lab', 'مختبر معايرة الهندسة الطبية', 2),
                (5, 301, 3, 'CT Scanner Suite 1', 'غرفة الأشعة المقطعية (CT 1)', 4),
                (6, 402, 4, 'Central Facilities Plant Room', 'غرفة معدات التكييف والمرافق', 6);

                INSERT OR IGNORE INTO suppliers (id, name, contactPerson, email, phone, address, notes) VALUES
                (1, 'Philips Healthcare Middle East', 'Tariq Al-Saleh', 'service@philips.health', '+961 1 800 240', 'Beirut Central District', 'Authorized biomedical service center & parts supplier'),
                (2, 'Dell EMC Enterprise Solutions', 'Karim Haddad', 'enterprise@dell.support', '+961 1 700 890', 'Sin El Fil Tech Park', 'Primary hardware supplier for hospital servers & SAN'),
                (3, 'GE HealthCare Diagnostics', 'Rami Mansour', 'diagnostics@ge.com', '+961 1 950 112', 'Hazmieh Medical Center', 'High-end radiology, MRI and ultrasound OEM service'),
                (4, 'Schneider Electric & APC', 'Ziad Kassir', 'ups.support@se.com', '+961 1 611 400', 'Mansourieh Industrial', 'Hospital UPS systems & critical power backup');

                INSERT OR IGNORE INTO users (id, fullName, domainUser, role, departmentId, email, phone, maintenanceTeam, isActive) VALUES
                (1, 'Abbas Al-Amiri', 'a.amiri', 'ADMIN', 1, 'abbas.amiri@solvis-health.org', '+961 70 024 524', 'IT', 1),
                (2, 'Ahmed Mansour', 'a.mansour', 'MAINTENANCE', 1, 'ahmed.m@solvis-health.org', '+961 71 334 112', 'IT', 1),
                (3, 'Dr. Sarah Jenkins', 's.jenkins', 'END_USER', 3, 'dr.sarah@solvis-health.org', '+961 3 550 918', 'GENERAL', 1),
                (4, 'Dr. Elena Rostova', 'e.rostova', 'END_USER', 4, 'dr.elena@solvis-health.org', '+961 76 892 411', 'GENERAL', 1),
                (5, 'Tariq Ziad', 't.ziad', 'MAINTENANCE', 2, 'tariq.z@solvis-health.org', '+961 70 911 340', 'BIOMEDICAL', 1),
                (6, 'Fadi Kabbani', 'f.kabbani', 'MAINTENANCE', 6, 'fadi.k@solvis-health.org', '+961 71 882 109', 'GENERAL', 1);
            ");

            // 3. Seed Realistic Hospital Assets
            $now = new DateTime();
            $dBuy1 = (clone $now)->modify('-14 months')->format('Y-m-d');
            $dWarr1 = (clone $now)->modify('+22 months')->format('Y-m-d');
            $dNextPM1 = (clone $now)->modify('+15 days')->format('Y-m-d');
            $dLastPM1 = (clone $now)->modify('-75 days')->format('Y-m-d');

            $dBuy2 = (clone $now)->modify('-8 months')->format('Y-m-d');
            $dWarr2 = (clone $now)->modify('+16 months')->format('Y-m-d');
            $dNextPM2 = (clone $now)->modify('-5 days')->format('Y-m-d'); // Overdue!
            $dLastPM2 = (clone $now)->modify('-185 days')->format('Y-m-d');

            $dBuy3 = (clone $now)->modify('-20 months')->format('Y-m-d');
            $dWarr3 = (clone $now)->modify('+4 months')->format('Y-m-d');
            $dNextPM3 = (clone $now)->modify('+25 days')->format('Y-m-d');

            $dBuy4 = (clone $now)->modify('-3 months')->format('Y-m-d');
            $dWarr4 = (clone $now)->modify('+33 months')->format('Y-m-d');

            $db->exec("
                INSERT OR IGNORE INTO assets 
                (id, hospitalNo, serialNo, brand, modelNo, price, type, status, supplierId, roomId, userId, custodyTypeId, specs, buyingDate, warrantyTill, needsPeriodic, periodicInterval, lastPeriodic, nextPeriodic, maintenanceTeam, categoryId, calibrationDate, calibrationDue, calibrationCertificate)
                VALUES
                (1, 'HOSP-SRV-001', 'SN-DELL-88219-R750', 'Dell EMC', 'PowerEdge R750', 14500.00, 'Rack Server', 'In Service', 2, 2, 1, 2, '2x Intel Xeon Gold 6330, 128GB DDR4 ECC, 8x 1.92TB SAS SSD RAID 10', '$dBuy1', '$dWarr1', 1, 90, '$dLastPM1', '$dNextPM1', 'IT', 2, '2026-05-10', '2027-05-10', 'CAL-DELL-2026-001'),
                (2, 'HOSP-BIO-042', 'SN-PHIL-99381-MX750', 'Philips Healthcare', 'IntelliVue MX750', 28900.00, 'Patient Monitor', 'In Service', 1, 3, 5, 2, '19-inch multi-parameter vital signs, 12-lead ECG, SpO2, invasive BP, Capnography', '$dBuy2', '$dWarr2', 1, 180, '$dLastPM2', '$dNextPM2', 'BIOMEDICAL', 1, '2026-04-12', '2026-10-12', 'CAL-PHIL-88192'),
                (3, 'HOSP-RAD-018', 'SN-GE-44109-OPT660', 'GE HealthCare', 'Optima CT660', 185000.00, 'CT Scanner', 'In Service', 3, 5, 4, 2, '64-slice high-resolution diagnostic imaging, ASiR iterative reconstruction', '$dBuy3', '$dWarr3', 1, 365, '$dBuy3', '$dNextPM3', 'BIOMEDICAL', 3, '2026-08-01', '2027-08-01', 'CAL-GE-2026-7731'),
                (4, 'HOSP-CLN-104', 'SN-HP-77182-ED800', 'HP Enterprise', 'EliteDesk 800 G9', 1650.00, 'Workstation PC', 'In Service', 2, 1, 3, 1, 'Intel Core i7-13700, 32GB DDR5, 1TB NVMe SSD, Dual 27-inch 4K Displays', '$dBuy4', '$dWarr4', 0, 0, NULL, NULL, 'IT', 2, NULL, NULL, NULL),
                (5, 'HOSP-BIO-099', 'SN-ZOLL-5521-RSR', 'Zoll Medical', 'R Series Plus', 16200.00, 'Defibrillator', 'Under Maintenance', 1, 4, 5, 3, 'Biphasic manual & AED pacing, Real CPR Help, External paddles', '$dBuy2', '$dWarr2', 1, 90, '$dLastPM2', '$dNextPM1', 'BIOMEDICAL', 1, '2026-03-15', '2026-09-15', 'CAL-ZOLL-4412');

                INSERT OR IGNORE INTO asset_history (assetId, action, notes, changedBy, timestamp, oldRoomId, newRoomId, oldUserId, newUserId) VALUES
                (1, 'Initial Commissioning', 'Registered Dell PowerEdge R750 into main data center datacenter rack B2', 'Abbas Al-Amiri', '$dBuy1', NULL, 2, NULL, 1),
                (2, 'Preventive Calibration', 'Completed annual clinical biomedical sensor recalibration and lead safety inspection', 'Tariq Ziad', '$dLastPM2', 3, 3, 5, 5),
                (5, 'Dispatched for Service', 'Dispatched to Philips Healthcare for capacitor replacement and high-voltage safety recertification', 'Tariq Ziad', '$dNextPM2', 3, 4, 5, 5);
            ");

            // 4. Seed Helpdesk Tickets & Discussion
            $tCreated1 = (clone $now)->modify('-2 days')->format('Y-m-d H:i:s');
            $tCreated2 = (clone $now)->modify('-6 hours')->format('Y-m-d H:i:s');
            $tCreated3 = (clone $now)->modify('-1 hour')->format('Y-m-d H:i:s');

            $db->exec("
                INSERT OR IGNORE INTO tickets 
                (id, title, description, priority, statusId, createdByUserId, assignedToUserId, departmentId, assetId, maintenanceTeam, createdAt, updatedAt)
                VALUES
                (1, 'ICU Bed 1 Patient Monitor intermittent SpO2 probe disconnect', 'The pulse oximetry sensor on HOSP-BIO-042 intermittently loses signal and triggers false alarms during clinical shifts. Cable was inspected and requires sensor replacement.', 'Urgent', 2, 3, 5, 5, 2, 'BIOMEDICAL', '$tCreated1', '$tCreated2'),
                (2, 'Cardiology Clinic HP Workstation blue screens on PACS image retrieval', 'When opening high-resolution DICOM angiograms from the PACS server, workstation HOSP-CLN-104 locks up and crashes with kernel memory fault. Need memory test and OS update.', 'High', 2, 3, 2, 3, 4, 'IT', '$tCreated2', '$tCreated3'),
                (3, 'Central UPS backup battery bank periodic maintenance required', 'Annual battery impedance test and load bank inspection scheduled for central facilities UPS unit HOSP-FAC-001.', 'Medium', 1, 1, 6, 6, NULL, 'GENERAL', '$tCreated3', '$tCreated3');

                INSERT OR IGNORE INTO ticket_messages (ticketId, userId, message, isInternal, createdAt) VALUES
                (1, 3, 'Dr. Sarah Jenkins opened ticket: SpO2 readings are dropping to zero intermittently on patient monitor.', 0, '$tCreated1'),
                (1, 5, 'Biomedical Engineer Tariq Ziad: Inspected device in ICU. Sensor cable has internal wire fatigue. Ordered replacement adult reusable clip from spare parts reserve.', 0, '$tCreated2'),
                (2, 3, 'Dr. Sarah Jenkins: Please expedite, this is the primary workstation used for cardiac patient consultations.', 0, '$tCreated2'),
                (2, 2, 'Ahmed Mansour (IT Support): Running Windows Memory Diagnostic. Replaced 16GB DDR5 memory module from reserve stock.', 0, '$tCreated3');
            ");

            // 5. Seed Spare Parts Inventory & Transactions
            $db->exec("
                INSERT OR IGNORE INTO spare_parts 
                (id, partNo, name, name_ar, category, quantity, minThreshold, unitCost, location, supplierId, maintenanceTeam, canBeAsset)
                VALUES
                (1, 'SP-BIO-SPO2-01', 'Philips Adult Reusable SpO2 Sensor Probe (3m)', 'مجس أكسجين الدم SpO2 بالغين قابل للاستخدام', 'Consumables', 8, 3, 145.00, 'Shelf B-12 / Biomed Lab', 1, 'BIOMEDICAL', 0),
                (2, 'SP-IT-DDR5-16GB', 'Kingston 16GB DDR5 4800MHz Desktop RAM', 'ذاكرة رام كينغستون 16 جيجابايت DDR5', 'Spare Parts', 12, 4, 68.00, 'Cabinet IT-04 / Data Center', 2, 'IT', 0),
                (3, 'SP-IT-SSD-2TB', 'Samsung 990 PRO 2TB NVMe PCIe 4.0 SSD', 'قرص تخزين سامسونغ سريع 2 تيرابايت NVMe', 'Spare Parts', 6, 2, 185.00, 'Cabinet IT-04 / Data Center', 2, 'IT', 1),
                (4, 'SP-BIO-PADS-09', 'Zoll Adult CPR Stat-padz Electrodes (Pair)', 'أقطاب إنعاش قلب للبالغين Stat-padz', 'Consumables', 22, 10, 48.00, 'Shelf B-08 / Biomed Lab', 1, 'BIOMEDICAL', 0),
                (5, 'SP-FAC-FILTER-HEPA', 'Hospital Grade HEPA Air Filter 24x24x12', 'فلتر هواء طبي عالي الكفاءة HEPA', 'Periodic Maintenance Reserve', 4, 2, 290.00, 'Warehouse Plant A', 4, 'GENERAL', 0);

                INSERT OR IGNORE INTO spare_part_transactions (sparePartId, ticketId, quantity, type, notes, createdAt, performedByUserId) VALUES
                (1, 1, -1, 'Usage in Ticket', 'Fitted to ICU Patient Monitor HOSP-BIO-042', '$tCreated2', 5),
                (2, 2, -1, 'Usage in Ticket', 'Installed in Cardiology Workstation HOSP-CLN-104', '$tCreated3', 2),
                (3, NULL, 5, 'Replenishment', 'Received batch order from Dell EMC Supplier', '$dBuy4', 1);
            ");

            // 6. Seed Maintenance Work Orders (Dispatches)
            $db->exec("
                INSERT OR IGNORE INTO asset_maintenances 
                (id, assetId, ticketId, maintenanceType, vendorId, dispatchedDate, returnedDate, cost, description, status)
                VALUES
                (1, 5, NULL, 'External OEM Factory Servicing', 1, '$dNextPM2', NULL, 1850.00, 'High-voltage internal capacitor replacement and biomedical safety certification at Philips Regional Service Center', 'Active');
            ");

            // 7. Seed Notifications
            $db->exec("
                INSERT OR IGNORE INTO notifications (userID, ticketID, title, message, type, linkUrl, isRead, createdAt) VALUES
                (1, 1, 'Urgent Ticket Opened', 'Ticket #1: ICU Bed 1 Patient Monitor intermittent SpO2 probe disconnect', 'danger', 'tickets/details?id=1', 0, '$tCreated1'),
                (1, NULL, 'Preventive Maintenance Overdue', 'Asset HOSP-BIO-042 (Philips IntelliVue MX750) has an overdue 180-day inspection.', 'warning', 'assets/details?id=2', 0, '$tCreated2'),
                (2, 2, 'New Ticket Assignment', 'Ticket #2 assigned to IT Support: Cardiology Clinic HP Workstation', 'info', 'tickets/details?id=2', 0, '$tCreated2');
            ");

            $db->commit();
        } catch (Exception $e) {
            $db->rollBack();
            throw $e;
        }
    }
}
