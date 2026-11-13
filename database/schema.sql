-- ==========================================================
-- SOLVIS Hospital Operations & Asset Intelligence Platform
-- MySQL / MariaDB Database Schema & Seed Data (hosting.com cPanel)
-- ==========================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS `departments` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) NOT NULL,
  `name_ar` varchar(255) DEFAULT NULL,
  `headUserID` int DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `rooms` (
  `id` int NOT NULL AUTO_INCREMENT,
  `roomNo` int NOT NULL,
  `floor` int NOT NULL,
  `name` varchar(255) DEFAULT NULL,
  `name_ar` varchar(255) DEFAULT NULL,
  `department_id` int DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `suppliers` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) NOT NULL,
  `contactPerson` varchar(255) DEFAULT NULL,
  `email` varchar(255) DEFAULT NULL,
  `phone` varchar(50) DEFAULT NULL,
  `address` text,
  `notes` text,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `custody_type` (
  `id` int NOT NULL,
  `type` varchar(255) NOT NULL,
  `type_ar` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ticket_status` (
  `id` int NOT NULL,
  `status` varchar(255) NOT NULL,
  `status_ar` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `users` (
  `id` int NOT NULL AUTO_INCREMENT,
  `fullName` varchar(255) NOT NULL,
  `domainUser` varchar(100) NOT NULL,
  `role` varchar(50) NOT NULL,
  `departmentId` int DEFAULT NULL,
  `email` varchar(255) DEFAULT NULL,
  `phone` varchar(50) DEFAULT NULL,
  `maintenanceTeam` varchar(50) DEFAULT 'IT',
  `isActive` tinyint(1) DEFAULT 1,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `asset_categories` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) NOT NULL,
  `name_ar` varchar(255) DEFAULT NULL,
  `code` varchar(50) DEFAULT NULL,
  `description` text,
  `icon` varchar(50) DEFAULT 'fa-boxes-stacked',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `assets` (
  `id` int NOT NULL AUTO_INCREMENT,
  `hospitalNo` varchar(100) NOT NULL UNIQUE,
  `serialNo` varchar(100) DEFAULT NULL,
  `brand` varchar(100) NOT NULL,
  `modelNo` varchar(100) DEFAULT NULL,
  `price` decimal(12,2) DEFAULT 0.00,
  `type` varchar(100) NOT NULL,
  `status` varchar(50) DEFAULT 'In Service',
  `supplierId` int DEFAULT NULL,
  `roomId` int DEFAULT NULL,
  `userId` int DEFAULT NULL,
  `custodyTypeId` int DEFAULT 1,
  `specs` text,
  `buyingDate` date DEFAULT NULL,
  `warrantyTill` date DEFAULT NULL,
  `needsPeriodic` tinyint(1) DEFAULT 0,
  `periodicInterval` int DEFAULT 90,
  `lastPeriodic` date DEFAULT NULL,
  `nextPeriodic` date DEFAULT NULL,
  `maintenanceTeam` varchar(50) DEFAULT 'IT',
  `categoryId` int DEFAULT NULL,
  `calibrationDate` date DEFAULT NULL,
  `calibrationDue` date DEFAULT NULL,
  `calibrationCertificate` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `asset_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `assetId` int NOT NULL,
  `action` varchar(100) NOT NULL,
  `notes` text,
  `changedBy` varchar(100) DEFAULT NULL,
  `timestamp` datetime NOT NULL,
  `oldRoomId` int DEFAULT NULL,
  `newRoomId` int DEFAULT NULL,
  `oldUserId` int DEFAULT NULL,
  `newUserId` int DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `tickets` (
  `id` int NOT NULL AUTO_INCREMENT,
  `title` varchar(255) NOT NULL,
  `description` text,
  `priority` varchar(50) DEFAULT 'Medium',
  `statusId` int DEFAULT 1,
  `createdByUserId` int NOT NULL,
  `assignedToUserId` int DEFAULT NULL,
  `departmentId` int DEFAULT NULL,
  `assetId` int DEFAULT NULL,
  `maintenanceTeam` varchar(50) DEFAULT 'IT',
  `createdAt` datetime NOT NULL,
  `updatedAt` datetime NOT NULL,
  `closedAt` datetime DEFAULT NULL,
  `resolution` text,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ticket_messages` (
  `id` int NOT NULL AUTO_INCREMENT,
  `ticketId` int NOT NULL,
  `userId` int NOT NULL,
  `message` text NOT NULL,
  `isInternal` tinyint(1) DEFAULT 0,
  `createdAt` datetime NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `asset_maintenances` (
  `id` int NOT NULL AUTO_INCREMENT,
  `assetId` int NOT NULL,
  `ticketId` int DEFAULT NULL,
  `maintenanceType` varchar(100) NOT NULL,
  `vendorId` int DEFAULT NULL,
  `dispatchedDate` date DEFAULT NULL,
  `returnedDate` date DEFAULT NULL,
  `cost` decimal(10,2) DEFAULT 0.00,
  `description` text,
  `status` varchar(50) DEFAULT 'Active',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `spare_parts` (
  `id` int NOT NULL AUTO_INCREMENT,
  `partNo` varchar(100) NOT NULL UNIQUE,
  `name` varchar(255) NOT NULL,
  `name_ar` varchar(255) DEFAULT NULL,
  `category` varchar(100) NOT NULL,
  `quantity` int DEFAULT 0,
  `minThreshold` int DEFAULT 5,
  `unitCost` decimal(10,2) DEFAULT 0.00,
  `location` varchar(100) DEFAULT NULL,
  `supplierId` int DEFAULT NULL,
  `maintenanceTeam` varchar(50) DEFAULT 'IT',
  `canBeAsset` tinyint(1) DEFAULT 0,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `spare_part_transactions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `sparePartId` int NOT NULL,
  `ticketId` int DEFAULT NULL,
  `quantity` int NOT NULL,
  `type` varchar(100) NOT NULL,
  `notes` text,
  `createdAt` datetime NOT NULL,
  `performedByUserId` int DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `notifications` (
  `id` int NOT NULL AUTO_INCREMENT,
  `userID` int NOT NULL,
  `ticketID` int DEFAULT NULL,
  `title` varchar(255) NOT NULL,
  `message` text NOT NULL,
  `type` varchar(50) DEFAULT 'info',
  `linkUrl` varchar(255) DEFAULT NULL,
  `isRead` tinyint(1) DEFAULT 0,
  `createdAt` datetime NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Seed Data
INSERT INTO `custody_type` (`id`, `type`, `type_ar`) VALUES
(1, 'Personal Custody', 'عهدة شخصية'),
(2, 'Department / Shared', 'عهدة قسم / مشتركة'),
(3, 'Supplier / External Maintenance', 'لدى المورد / صيانة خارجية')
ON DUPLICATE KEY UPDATE `type`=VALUES(`type`);

INSERT INTO `ticket_status` (`id`, `status`, `status_ar`) VALUES
(1, 'New', 'جديدة'),
(2, 'In Progress', 'قيد المعالجة'),
(3, 'Pending Spare Parts', 'بانتظار قطع الغيار'),
(4, 'Resolved', 'تم الإصلاح والحل'),
(5, 'Closed', 'مغلقة نهائياً'),
(6, 'Under External Maintenance', 'قيد الصيانة الخارجية')
ON DUPLICATE KEY UPDATE `status`=VALUES(`status`);

INSERT INTO `asset_categories` (`id`, `name`, `name_ar`, `code`, `description`, `icon`) VALUES
(1, 'Biomedical & Clinical Equipment', 'الأجهزة الطبية والحيوية', 'BIO', 'Patient monitors, defibrillators, ultrasound, and vital sign units', 'fa-heart-pulse'),
(2, 'IT Infrastructure & Computing', 'البنية التحتية والشبكات', 'IT', 'Servers, routers, switches, enterprise workstations, and laptops', 'fa-laptop-code'),
(3, 'Imaging & Diagnostics', 'التصوير الطبي والأشعة', 'RAD', 'X-Ray, CT scanners, MRI systems, and PACS diagnostic displays', 'fa-radiation'),
(4, 'Facilities & HVAC Plant', 'المرافق والمباني والتكييف', 'FAC', 'Medical gas regulators, chillers, UPS power distribution, autoclaves', 'fa-fan')
ON DUPLICATE KEY UPDATE `name`=VALUES(`name`);

INSERT INTO `departments` (`id`, `name`, `name_ar`, `headUserID`) VALUES
(1, 'Information Technology', 'تقنية المعلومات', 1),
(2, 'Biomedical Engineering', 'الهندسة الطبية الحيوية', 5),
(3, 'Cardiology', 'قسم أمراض القلب', 3),
(4, 'Radiology & Imaging', 'الأشعة والتصوير التشخيصي', 4),
(5, 'Intensive Care Unit (ICU)', 'العناية المركزة (ICU)', 3),
(6, 'Facilities & Maintenance', 'الصيانة العامة والمرافق', 2)
ON DUPLICATE KEY UPDATE `name`=VALUES(`name`);

INSERT INTO `rooms` (`id`, `roomNo`, `floor`, `name`, `name_ar`, `department_id`) VALUES
(1, 101, 1, 'Cardiology Clinic A', 'عيادة القلب (أ)', 3),
(2, 105, 1, 'Main Server Data Center', 'مركز البيانات الرئيسي (IT)', 1),
(3, 201, 2, 'ICU Pod 1 - High Dependency', 'العناية المركزة - الجناح 1', 5),
(4, 204, 2, 'Biomedical Calibration Lab', 'مختبر معايرة الهندسة الطبية', 2),
(5, 301, 3, 'CT Scanner Suite 1', 'غرفة الأشعة المقطعية (CT 1)', 4),
(6, 402, 4, 'Central Facilities Plant Room', 'غرفة معدات التكييف والمرافق', 6)
ON DUPLICATE KEY UPDATE `name`=VALUES(`name`);

INSERT INTO `suppliers` (`id`, `name`, `contactPerson`, `email`, `phone`, `address`, `notes`) VALUES
(1, 'Philips Healthcare Middle East', 'Tariq Al-Saleh', 'service@philips.health', '+961 1 800 240', 'Beirut Central District', 'Authorized biomedical service center & parts supplier'),
(2, 'Dell EMC Enterprise Solutions', 'Karim Haddad', 'enterprise@dell.support', '+961 1 700 890', 'Sin El Fil Tech Park', 'Primary hardware supplier for hospital servers & SAN'),
(3, 'GE HealthCare Diagnostics', 'Rami Mansour', 'diagnostics@ge.com', '+961 1 950 112', 'Hazmieh Medical Center', 'High-end radiology, MRI and ultrasound OEM service'),
(4, 'Schneider Electric & APC', 'Ziad Kassir', 'ups.support@se.com', '+961 1 611 400', 'Mansourieh Industrial', 'Hospital UPS systems & critical power backup')
ON DUPLICATE KEY UPDATE `name`=VALUES(`name`);

INSERT INTO `users` (`id`, `fullName`, `domainUser`, `role`, `departmentId`, `email`, `phone`, `maintenanceTeam`, `isActive`) VALUES
(1, 'Abbas Al-Amiri', 'a.amiri', 'ADMIN', 1, 'abbas.amiri@solvis-health.org', '+961 70 024 524', 'IT', 1),
(2, 'Ahmed Mansour', 'a.mansour', 'MAINTENANCE', 1, 'ahmed.m@solvis-health.org', '+961 71 334 112', 'IT', 1),
(3, 'Dr. Sarah Jenkins', 's.jenkins', 'END_USER', 3, 'dr.sarah@solvis-health.org', '+961 3 550 918', 'GENERAL', 1),
(4, 'Dr. Elena Rostova', 'e.rostova', 'END_USER', 4, 'dr.elena@solvis-health.org', '+961 76 892 411', 'GENERAL', 1),
(5, 'Tariq Ziad', 't.ziad', 'MAINTENANCE', 2, 'tariq.z@solvis-health.org', '+961 70 911 340', 'BIOMEDICAL', 1),
(6, 'Fadi Kabbani', 'f.kabbani', 'MAINTENANCE', 6, 'fadi.k@solvis-health.org', '+961 71 882 109', 'GENERAL', 1)
ON DUPLICATE KEY UPDATE `fullName`=VALUES(`fullName`);

INSERT INTO `assets` (`id`, `hospitalNo`, `serialNo`, `brand`, `modelNo`, `price`, `type`, `status`, `supplierId`, `roomId`, `userId`, `custodyTypeId`, `specs`, `buyingDate`, `warrantyTill`, `needsPeriodic`, `periodicInterval`, `lastPeriodic`, `nextPeriodic`, `maintenanceTeam`, `categoryId`, `calibrationDate`, `calibrationDue`, `calibrationCertificate`) VALUES
(1, 'HOSP-SRV-001', 'SN-DELL-88219-R750', 'Dell EMC', 'PowerEdge R750', 14500.00, 'Rack Server', 'In Service', 2, 2, 1, 2, '2x Intel Xeon Gold 6330, 128GB DDR4 ECC, 8x 1.92TB SAS SSD RAID 10', '2025-09-13', '2028-09-13', 1, 90, '2026-08-30', '2026-11-28', 'IT', 2, '2026-05-10', '2027-05-10', 'CAL-DELL-2026-001'),
(2, 'HOSP-BIO-042', 'SN-PHIL-99381-MX750', 'Philips Healthcare', 'IntelliVue MX750', 28900.00, 'Patient Monitor', 'In Service', 1, 3, 5, 2, '19-inch multi-parameter vital signs, 12-lead ECG, SpO2, invasive BP, Capnography', '2026-03-13', '2028-03-13', 1, 180, '2026-05-12', '2026-11-08', 'BIOMEDICAL', 1, '2026-04-12', '2026-10-12', 'CAL-PHIL-88192'),
(3, 'HOSP-RAD-018', 'SN-GE-44109-OPT660', 'GE HealthCare', 'Optima CT660', 185000.00, 'CT Scanner', 'In Service', 3, 5, 4, 2, '64-slice high-resolution diagnostic imaging, ASiR iterative reconstruction', '2025-03-13', '2027-03-13', 1, 365, '2025-03-13', '2026-12-08', 'BIOMEDICAL', 3, '2026-08-01', '2027-08-01', 'CAL-GE-2026-7731'),
(4, 'HOSP-CLN-104', 'SN-HP-77182-ED800', 'HP Enterprise', 'EliteDesk 800 G9', 1650.00, 'Workstation PC', 'In Service', 2, 1, 3, 1, 'Intel Core i7-13700, 32GB DDR5, 1TB NVMe SSD, Dual 27-inch 4K Displays', '2026-08-13', '2029-08-13', 0, 0, NULL, NULL, 'IT', 2, NULL, NULL, NULL),
(5, 'HOSP-BIO-099', 'SN-ZOLL-5521-RSR', 'Zoll Medical', 'R Series Plus', 16200.00, 'Defibrillator', 'Under Maintenance', 1, 4, 5, 3, 'Biphasic manual & AED pacing, Real CPR Help, External paddles', '2026-03-13', '2028-03-13', 1, 90, '2026-05-12', '2026-11-28', 'BIOMEDICAL', 1, '2026-03-15', '2026-09-15', 'CAL-ZOLL-4412')
ON DUPLICATE KEY UPDATE `brand`=VALUES(`brand`);

INSERT INTO `spare_parts` (`id`, `partNo`, `name`, `name_ar`, `category`, `quantity`, `minThreshold`, `unitCost`, `location`, `supplierId`, `maintenanceTeam`, `canBeAsset`) VALUES
(1, 'SP-BIO-SPO2-01', 'Philips Adult Reusable SpO2 Sensor Probe (3m)', 'مجس أكسجين الدم SpO2 بالغين قابل للاستخدام', 'Consumables', 8, 3, 145.00, 'Shelf B-12 / Biomed Lab', 1, 'BIOMEDICAL', 0),
(2, 'SP-IT-DDR5-16GB', 'Kingston 16GB DDR5 4800MHz Desktop RAM', 'ذاكرة رام كينغستون 16 جيجابايت DDR5', 'Spare Parts', 12, 4, 68.00, 'Cabinet IT-04 / Data Center', 2, 'IT', 0),
(3, 'SP-IT-SSD-2TB', 'Samsung 990 PRO 2TB NVMe PCIe 4.0 SSD', 'قرص تخزين سامسونغ سريع 2 تيرابايت NVMe', 'Spare Parts', 6, 2, 185.00, 'Cabinet IT-04 / Data Center', 2, 'IT', 1),
(4, 'SP-BIO-PADS-09', 'Zoll Adult CPR Stat-padz Electrodes (Pair)', 'أقطاب إنعاش قلب للبالغين Stat-padz', 'Consumables', 22, 10, 48.00, 'Shelf B-08 / Biomed Lab', 1, 'BIOMEDICAL', 0),
(5, 'SP-FAC-FILTER-HEPA', 'Hospital Grade HEPA Air Filter 24x24x12', 'فلتر هواء طبي عالي الكفاءة HEPA', 'Periodic Maintenance Reserve', 4, 2, 290.00, 'Warehouse Plant A', 4, 'GENERAL', 0)
ON DUPLICATE KEY UPDATE `name`=VALUES(`name`);

INSERT INTO `tickets` (`id`, `title`, `description`, `priority`, `statusId`, `createdByUserId`, `assignedToUserId`, `departmentId`, `assetId`, `maintenanceTeam`, `createdAt`, `updatedAt`) VALUES
(1, 'ICU Bed 1 Patient Monitor intermittent SpO2 probe disconnect', 'The pulse oximetry sensor on HOSP-BIO-042 intermittently loses signal and triggers false alarms during clinical shifts. Cable was inspected and requires sensor replacement.', 'Urgent', 2, 3, 5, 5, 2, 'BIOMEDICAL', '2026-11-11 10:00:00', '2026-11-12 14:00:00'),
(2, 'Cardiology Clinic HP Workstation blue screens on PACS image retrieval', 'When opening high-resolution DICOM angiograms from the PACS server, workstation HOSP-CLN-104 locks up and crashes with kernel memory fault. Need memory test and OS update.', 'High', 2, 3, 2, 3, 4, 'IT', '2026-11-12 18:00:00', '2026-11-13 06:00:00'),
(3, 'Central UPS backup battery bank periodic maintenance required', 'Annual battery impedance test and load bank inspection scheduled for central facilities UPS unit HOSP-FAC-001.', 'Medium', 1, 1, 6, 6, NULL, 'GENERAL', '2026-11-13 05:00:00', '2026-11-13 05:00:00')
ON DUPLICATE KEY UPDATE `title`=VALUES(`title`);

SET FOREIGN_KEY_CHECKS = 1;
