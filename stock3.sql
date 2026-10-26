/*
 Navicat Premium Dump SQL

 Source Server         : local
 Source Server Type    : MySQL
 Source Server Version : 100432 (10.4.32-MariaDB)
 Source Host           : localhost:3306
 Source Schema         : stock

 Target Server Type    : MySQL
 Target Server Version : 100432 (10.4.32-MariaDB)
 File Encoding         : 65001

 Date: 23/10/2026 13:59:05
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for asset_history
-- ----------------------------
DROP TABLE IF EXISTS `asset_history`;
CREATE TABLE `asset_history`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `assetID` int NOT NULL,
  `userID` int NOT NULL,
  `roomID` int NOT NULL,
  `CustodyType` int NOT NULL,
  `assignedOn` datetime NOT NULL,
  `returnedOn` datetime NULL DEFAULT NULL,
  `notes` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `timeStamp` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 52 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of asset_history
-- ----------------------------
INSERT INTO `asset_history` VALUES (1, 1, 1, 3, 2, '2025-08-22 14:15:19', NULL, 'Initial commissioning and asset custody tagging', '2026-10-22 14:15:19');
INSERT INTO `asset_history` VALUES (2, 2, 4, 5, 2, '2025-04-22 14:15:19', NULL, 'Initial commissioning and asset custody tagging', '2026-10-22 14:15:19');
INSERT INTO `asset_history` VALUES (3, 3, 3, 1, 1, '2026-02-22 14:15:19', NULL, 'Initial commissioning and asset custody tagging', '2026-10-22 14:15:19');
INSERT INTO `asset_history` VALUES (4, 4, 1, 3, 2, '2025-02-22 14:15:19', NULL, 'Initial commissioning and asset custody tagging', '2026-10-22 14:15:19');
INSERT INTO `asset_history` VALUES (5, 6, 3, 4, 2, '2025-06-15 10:00:00', NULL, 'Initial commissioning of GE Healthcare Patient Monitor', '2026-10-23 12:17:25');
INSERT INTO `asset_history` VALUES (6, 7, 3, 4, 2, '2025-02-10 09:30:00', NULL, 'Initial commissioning of Dräger ICU Ventilator', '2026-10-23 12:17:25');
INSERT INTO `asset_history` VALUES (7, 8, 4, 1, 2, '2024-11-20 14:15:00', NULL, 'Initial commissioning of Zoll Medical Defibrillator', '2026-10-23 12:17:25');
INSERT INTO `asset_history` VALUES (8, 9, 4, 5, 2, '2024-05-18 11:00:00', NULL, 'Initial commissioning of Siemens Healthineers Mobile X-Ray', '2026-10-23 12:17:25');
INSERT INTO `asset_history` VALUES (9, 10, 1, 3, 2, '2024-08-10 08:00:00', NULL, 'Initial commissioning of Schneider Electric Facility UPS', '2026-10-23 12:17:25');
INSERT INTO `asset_history` VALUES (10, 11, 1, 6, 2, '2024-03-22 10:30:00', NULL, 'Initial commissioning of Carrier HVAC Clean Air Unit', '2026-10-23 12:17:25');
INSERT INTO `asset_history` VALUES (11, 12, 1, 6, 2, '2023-12-01 12:00:00', NULL, 'Initial commissioning of Caterpillar Standby Generator', '2026-10-23 12:17:25');
INSERT INTO `asset_history` VALUES (12, 13, 1, 6, 2, '2024-01-15 09:00:00', NULL, 'Initial commissioning of BeaconMedaes Medical Vacuum Plant', '2026-10-23 12:17:25');
INSERT INTO `asset_history` VALUES (20, 6, 3, 4, 2, '2025-06-15 10:00:00', NULL, 'Initial commissioning of GE Healthcare Patient Monitor', '2026-10-23 12:20:01');
INSERT INTO `asset_history` VALUES (21, 7, 3, 4, 2, '2025-02-10 09:30:00', NULL, 'Initial commissioning of Dräger ICU Ventilator', '2026-10-23 12:20:01');
INSERT INTO `asset_history` VALUES (22, 8, 4, 1, 2, '2024-11-20 14:15:00', NULL, 'Initial commissioning of Zoll Medical Defibrillator', '2026-10-23 12:20:01');
INSERT INTO `asset_history` VALUES (23, 9, 4, 5, 2, '2024-05-18 11:00:00', NULL, 'Initial commissioning of Siemens Healthineers Mobile X-Ray', '2026-10-23 12:20:01');
INSERT INTO `asset_history` VALUES (24, 10, 1, 3, 2, '2024-08-10 08:00:00', NULL, 'Initial commissioning of Schneider Electric Facility UPS', '2026-10-23 12:20:01');
INSERT INTO `asset_history` VALUES (25, 11, 1, 6, 2, '2024-03-22 10:30:00', NULL, 'Initial commissioning of Carrier HVAC Clean Air Unit', '2026-10-23 12:20:01');
INSERT INTO `asset_history` VALUES (26, 12, 1, 6, 2, '2023-12-01 12:00:00', NULL, 'Initial commissioning of Caterpillar Standby Generator', '2026-10-23 12:20:01');
INSERT INTO `asset_history` VALUES (27, 13, 1, 6, 2, '2024-01-15 09:00:00', NULL, 'Initial commissioning of BeaconMedaes Medical Vacuum Plant', '2026-10-23 12:20:01');
INSERT INTO `asset_history` VALUES (51, 22, 1, 1, 1, '2026-10-23 12:20:25', NULL, 'Initial commissioning of Welch Allyn Vital Signs Monitor', '2026-10-23 12:20:25');

-- ----------------------------
-- Table structure for asset_maintenance
-- ----------------------------
DROP TABLE IF EXISTS `asset_maintenance`;
CREATE TABLE `asset_maintenance`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `assetID` int NULL DEFAULT NULL,
  `ticketID` int NULL DEFAULT NULL,
  `maintenanceType` int NULL DEFAULT NULL,
  `userID` int NULL DEFAULT NULL,
  `supplierID` int NULL DEFAULT NULL,
  `description` varchar(1024) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `partsReplaced` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NULL,
  `timStamp` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 2 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of asset_maintenance
-- ----------------------------
INSERT INTO `asset_maintenance` VALUES (1, 1, 3, 1, 2, NULL, 'Replaced faulty hot-swap power supply unit PSU2 in Dell R750 server.', '{\"Part\": \"Dell 1400W Platinum Hot-Plug Redundant PSU\", \"PartNumber\": \"DP/N 0XXV21\"}', '2026-10-14 14:15:19');

-- ----------------------------
-- Table structure for assets
-- ----------------------------
DROP TABLE IF EXISTS `assets`;
CREATE TABLE `assets`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `price` decimal(10, 2) NULL DEFAULT NULL,
  `hospitalNo` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `serialNo` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `brand` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `modelNo` int NULL DEFAULT NULL,
  `supplierID` int NULL DEFAULT NULL,
  `status` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `userID` int NULL DEFAULT NULL,
  `roomID` int NULL DEFAULT NULL,
  `custodyType` int NULL DEFAULT NULL,
  `type` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `warrantyTill` date NULL DEFAULT NULL,
  `specs` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `buyingDate` datetime NULL DEFAULT NULL,
  `needsPeriodic` tinyint(1) NULL DEFAULT 0,
  `periodicInterval` int NULL DEFAULT NULL COMMENT 'in days',
  `lastPeriodic` date NULL DEFAULT NULL,
  `nextPeriodic` date NULL DEFAULT NULL,
  `maintenance_team` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'IT',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 24 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of assets
-- ----------------------------
INSERT INTO `assets` VALUES (1, 14500.00, 'HOSP-SRV-001', 'SN-DELL-88219-R750', 'Dell EMC', 750, 1, 'In Service', 1, 3, 2, 'Rack Server', '2028-08-22', '{\"CPU\": \"2x Intel Xeon Gold 6330\", \"RAM\": \"128GB DDR4 ECC\", \"Storage\": \"8x 1.92TB SAS SSD RAID 10\"}', '2025-08-22 14:15:19', 1, 90, '2026-08-08', '2026-11-06', 'IT');
INSERT INTO `assets` VALUES (2, 48000.00, 'HOSP-US-004', 'SN-PH-99231-EPIQ7', 'Philips Healthcare', 7, 2, 'In Service', 4, 5, 2, 'Ultrasound Scanner', '2028-04-22', '{\"Model\": \"EPIQ 7 Diagnostic Ultrasound\", \"Probes\": \"Curved array, Linear, Phased array cardiac\", \"Screen\": \"21.5-inch HD Display\"}', '2025-04-22 14:15:19', 1, 180, '2026-04-15', '2026-10-12', 'BIOMEDICAL');
INSERT INTO `assets` VALUES (3, 1250.00, 'HOSP-LAP-012', 'SN-HP-44102-G8', 'HP', 850, 3, 'Under Maintenance', 3, 1, 1, 'Doctor Laptop', '2029-02-22', '{\"Model\": \"EliteBook 850 G8\", \"CPU\": \"Intel Core i7-1165G7\", \"RAM\": \"16GB\", \"SSD\": \"512GB NVMe\"}', '2026-02-22 14:15:19', 0, NULL, NULL, NULL, 'IT');
INSERT INTO `assets` VALUES (4, 6200.00, 'HOSP-NET-003', 'SN-CS-77119-9300', 'Cisco', 9300, 4, 'In Service', 1, 3, 2, 'Core Network Switch', '2030-02-22', '{\"Ports\": \"48x Gigabit PoE+\", \"Uplinks\": \"4x 10G SFP+\", \"Stacking\": \"StackWise-480\"}', '2025-02-22 14:15:19', 1, 180, '2026-08-23', '2027-02-19', 'IT');
INSERT INTO `assets` VALUES (5, 500.00, 'DESKTOP-P5717HQ', '2B1E900E-99CF-49C2-946D-F4F8226957D5', 'lenovo', 331, 1, 'In Service', 6, 1, 1, 'PC', '2030-01-01', '{\"Model\": \"EliteBook 850 G8\", \"CPU\": \"Intel Core i7-1165G7\", \"RAM\": \"16GB\", \"SSD\": \"512GB NVMe\"}', '2025-02-22 14:15:19', 1, 180, '2026-01-01', NULL, 'IT');
INSERT INTO `assets` VALUES (6, 12500.00, 'HOSP-BIO-001', 'SN-GE-CARESCAPE-881', 'GE Healthcare', 800, 5, 'In Service', 3, 4, 2, 'Patient Monitor', '2027-06-15', '{\"Model\": \"Carescape B850 Monitor\", \"Parameters\": \"ECG, SpO2, NIBP, Invasive Pressure, Temp\", \"Screen\": \"19-inch medical touch display\"}', '2025-06-15 10:00:00', 1, 180, '2026-08-24', '2027-02-20', 'BIOMEDICAL');
INSERT INTO `assets` VALUES (7, 34000.00, 'HOSP-BIO-002', 'SN-DRAEGER-VN500-29', 'Dräger', 500, 5, 'In Service', 3, 4, 2, 'ICU Ventilator', '2028-02-10', '{\"Model\": \"Evita Infinity V500\", \"Modes\": \"Invasive & Non-invasive ventilation\", \"Gas Supply\": \"O2 and Medical Air Wall Connection\"}', '2025-02-10 09:30:00', 1, 90, '2026-08-04', '2026-11-02', 'BIOMEDICAL');
INSERT INTO `assets` VALUES (8, 9800.00, 'HOSP-BIO-003', 'SN-ZOLL-R-SERIES-412', 'Zoll Medical', 100, 2, 'In Service', 4, 1, 2, 'Defibrillator', '2026-11-20', '{\"Model\": \"R Series Plus ALS Defibrillator\", \"Capabilities\": \"Biphasic Defibrillation, Pacing, CPR feedback\", \"Battery\": \"SurePower Rechargeable Lithium-ion\"}', '2024-11-20 14:15:00', 1, 30, '2026-09-08', '2026-10-08', 'BIOMEDICAL');
INSERT INTO `assets` VALUES (9, 72000.00, 'HOSP-BIO-004', 'SN-SIEMENS-MOB-33', 'Siemens Healthineers', 300, 5, 'Under Maintenance', 4, 5, 2, 'Mobile X-Ray', '2027-05-18', '{\"Model\": \"Mobilett Elara Max\", \"Detector\": \"MAX wi-D Wireless Detector\", \"Generator\": \"35 kW High Frequency\"}', '2024-05-18 11:00:00', 1, 180, '2026-07-15', '2027-01-11', 'BIOMEDICAL');
INSERT INTO `assets` VALUES (10, 28500.00, 'HOSP-GEN-001', 'SN-SCH-GALAXY-300', 'Schneider Electric', 300, 6, 'In Service', 1, 3, 2, 'Facility UPS', '2027-08-10', '{\"Capacity\": \"40kVA / 36kW\", \"Battery Bank\": \"32x 12V 100Ah Sealed Lead-Acid\", \"Autonomy\": \"45 mins full hospital critical load\"}', '2024-08-10 08:00:00', 1, 90, '2026-09-23', '2026-12-22', 'GENERAL');
INSERT INTO `assets` VALUES (11, 19800.00, 'HOSP-GEN-002', 'SN-CARRIER-AHU-902', 'Carrier', 900, 6, 'In Service', 1, 6, 2, 'HVAC Clean Air Unit', '2026-03-22', '{\"Type\": \"Hygienic Air Handling Unit\", \"Filtration\": \"HEPA H14 + G4 Pre-filter\", \"Airflow\": \"3500 CFM\", \"Serves\": \"Operating Suites 1 & 2\"}', '2024-03-22 10:30:00', 1, 60, '2026-08-19', '2026-10-18', 'GENERAL');
INSERT INTO `assets` VALUES (12, 65000.00, 'HOSP-GEN-003', 'SN-CAT-C15-8840', 'Caterpillar', 15, 6, 'In Service', 1, 6, 2, 'Standby Generator', '2028-12-01', '{\"Rating\": \"500 kVA / 400 kW Standby\", \"Fuel\": \"Diesel 1500L Tank\", \"ATS\": \"Automatic Transfer Switch 800A\"}', '2023-12-01 12:00:00', 1, 30, '2026-10-13', '2026-11-12', 'GENERAL');
INSERT INTO `assets` VALUES (13, 22000.00, 'HOSP-GEN-004', 'SN-BEACON-MEDAES-77', 'BeaconMedaes', 77, 6, 'In Service', 6, 6, 2, 'Medical Vacuum Plant', '2027-01-15', '{\"Configuration\": \"Triplex Rotary Vane Vacuum\", \"Flow Rate\": \"120 m3/h\", \"Standard\": \"HTM 02-01 / NFPA 99 Compliant\"}', '2024-01-15 09:00:00', 1, 90, '2026-09-08', '2026-12-07', 'GENERAL');
INSERT INTO `assets` VALUES (22, 3500.00, 'HOSP-BIO-005', 'SN-WELCH-CONN-901', 'Welch Allyn', 900, 5, 'In Service', 6, 1, 1, 'Vital Signs Monitor', '2027-04-10', '{\"Model\": \"Connex Spot Monitor\", \"NIBP\": \"SureBP 15-second readings\", \"Pulse Oximetry\": \"Covidien Nellcor\"}', '2025-04-10 10:00:00', 1, 180, '2026-09-23', '2027-03-22', 'BIOMEDICAL');
INSERT INTO `assets` VALUES (23, NULL, 'HOSP-IT-E2E-99', 'SN-E2E-TEST-99', 'Dell Latitude', 5540, NULL, 'In Service', NULL, 3, NULL, 'Laptop', NULL, '{\"Processor\":\"Intel Core i7-1370P vPro\",\"RAM\":\"32 GB\",\"Storage\":\"1TB NVMe PCIe Gen4\",\"OS\":\"Windows 11 Pro\",\"ScreenSize\":\"15.6 inch FHD IPS\"}', NULL, 0, NULL, NULL, NULL, 'IT');

-- ----------------------------
-- Table structure for custody_type
-- ----------------------------
DROP TABLE IF EXISTS `custody_type`;
CREATE TABLE `custody_type`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `type` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 4 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of custody_type
-- ----------------------------
INSERT INTO `custody_type` VALUES (1, 'Personal');
INSERT INTO `custody_type` VALUES (2, 'Shared');
INSERT INTO `custody_type` VALUES (3, NULL);

-- ----------------------------
-- Table structure for departments
-- ----------------------------
DROP TABLE IF EXISTS `departments`;
CREATE TABLE `departments`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `headUserID` int NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 6 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of departments
-- ----------------------------
INSERT INTO `departments` VALUES (1, 'Information Technology', 1);
INSERT INTO `departments` VALUES (2, 'Radiology & Imaging', NULL);
INSERT INTO `departments` VALUES (3, 'Cardiology', NULL);
INSERT INTO `departments` VALUES (4, 'Biomedical Engineering', NULL);
INSERT INTO `departments` VALUES (5, 'Hospital Administration', NULL);

-- ----------------------------
-- Table structure for device_forms
-- ----------------------------
DROP TABLE IF EXISTS `device_forms`;
CREATE TABLE `device_forms`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `department` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `form` longtext CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 17 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of device_forms
-- ----------------------------
INSERT INTO `device_forms` VALUES (1, 'Laptop', 'IT', '[\n                            {\"name\":\"Processor\",\"label\":\"Processor / CPU\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. Intel Core i7-1165G7\"},\n                            {\"name\":\"RAM\",\"label\":\"Installed Memory (RAM)\",\"type\":\"select\",\"options\":[\"8 GB\",\"16 GB\",\"32 GB\",\"64 GB\"],\"required\":true},\n                            {\"name\":\"Storage\",\"label\":\"Primary Storage (SSD)\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. 512GB NVMe SSD\"},\n                            {\"name\":\"OS\",\"label\":\"Operating System\",\"type\":\"select\",\"options\":[\"Windows 11 Pro\",\"Windows 10 Pro\",\"Ubuntu Linux\",\"macOS\"],\"required\":true},\n                            {\"name\":\"ScreenSize\",\"label\":\"Display Size\",\"type\":\"text\",\"required\":false,\"placeholder\":\"e.g. 15.6 inch FHD\"}\n                        ]');
INSERT INTO `device_forms` VALUES (2, 'Desktop Workstation', 'IT', '[\n                            {\"name\":\"FormFactor\",\"label\":\"Chassis / Form Factor\",\"type\":\"select\",\"options\":[\"Small Form Factor (SFF)\",\"Mini Tower\",\"All-in-One (AIO)\",\"Micro / Tiny PC\"],\"required\":true},\n                            {\"name\":\"CPU\",\"label\":\"Processor (CPU)\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. Intel Core i5-12500 / Ryzen 5 5600G\"},\n                            {\"name\":\"RAM\",\"label\":\"Installed RAM\",\"type\":\"select\",\"options\":[\"8 GB\",\"16 GB\",\"32 GB\",\"64 GB\"],\"required\":true},\n                            {\"name\":\"Storage\",\"label\":\"Primary Drive\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. 256GB SSD + 1TB HDD\"},\n                            {\"name\":\"GPU\",\"label\":\"Graphics Card\",\"type\":\"text\",\"required\":false,\"placeholder\":\"e.g. Integrated Intel UHD / NVIDIA T600\"},\n                            {\"name\":\"OS\",\"label\":\"Operating System\",\"type\":\"select\",\"options\":[\"Windows 11 Pro\",\"Windows 10 Pro\"],\"required\":true}\n                        ]');
INSERT INTO `device_forms` VALUES (3, 'Core Switch', 'IT', '[\n                            {\"name\":\"Ports\",\"label\":\"Ethernet Port Count\",\"type\":\"select\",\"options\":[\"24-Port Gigabit\",\"48-Port Gigabit\",\"24-Port Multi-Gig\",\"48-Port Multi-Gig\"],\"required\":true},\n                            {\"name\":\"PoE\",\"label\":\"Power Over Ethernet\",\"type\":\"select\",\"options\":[\"PoE+ (30W/port)\",\"PoE++ (60W/port)\",\"Non-PoE\"],\"required\":true},\n                            {\"name\":\"Uplinks\",\"label\":\"Uplink SFP Ports\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. 4x 10G SFP+\"},\n                            {\"name\":\"ManagementIP\",\"label\":\"Default Management IP\",\"type\":\"text\",\"required\":false,\"placeholder\":\"e.g. 10.10.1.5/24\"}\n                        ]');
INSERT INTO `device_forms` VALUES (4, 'Network Printer', 'IT', '[\n                            {\"name\":\"Tech\",\"label\":\"Print Technology\",\"type\":\"select\",\"options\":[\"Laser Monochrome\",\"Laser Color\",\"Thermal Barcode / Wristband\",\"Multi-Function Copier (MFP)\"],\"required\":true},\n                            {\"name\":\"Resolution\",\"label\":\"Max Resolution\",\"type\":\"text\",\"required\":false,\"placeholder\":\"e.g. 1200 x 1200 dpi\"},\n                            {\"name\":\"Duplex\",\"label\":\"Automatic Duplex (2-sided)\",\"type\":\"select\",\"options\":[\"Yes\",\"No\"],\"required\":true},\n                            {\"name\":\"TrayCapacity\",\"label\":\"Paper Tray Capacity\",\"type\":\"text\",\"required\":false,\"placeholder\":\"e.g. 2x 500-sheet trays\"}\n                        ]');
INSERT INTO `device_forms` VALUES (5, 'Rack Server', 'IT', '[\n                            {\"name\":\"Chassis\",\"label\":\"Rack Units (U)\",\"type\":\"select\",\"options\":[\"1U Rack\",\"2U Rack\",\"4U Rack\",\"Blade Enclosure\"],\"required\":true},\n                            {\"name\":\"CPU\",\"label\":\"Processors Installed\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. 2x Intel Xeon Silver 4314\"},\n                            {\"name\":\"RAM\",\"label\":\"Installed ECC Memory\",\"type\":\"select\",\"options\":[\"64 GB DDR4\",\"128 GB DDR4\",\"256 GB DDR4\",\"512 GB DDR4\"],\"required\":true},\n                            {\"name\":\"RAID\",\"label\":\"Storage / RAID Setup\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. 6x 1.92TB SAS SSD RAID 6\"},\n                            {\"name\":\"PSU\",\"label\":\"Power Supply Units\",\"type\":\"text\",\"required\":false,\"placeholder\":\"e.g. Dual Redundant 800W Platinum\"}\n                        ]');
INSERT INTO `device_forms` VALUES (6, 'Patient Monitor', 'BIOMEDICAL', '[\n                            {\"name\":\"Screen\",\"label\":\"Display Screen\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. 19-inch High Resolution Medical Touchscreen\"},\n                            {\"name\":\"Parameters\",\"label\":\"Monitored Parameters\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. ECG, SpO2, NIBP, Invasive Pressure, Temp, EtCO2\"},\n                            {\"name\":\"LeadCount\",\"label\":\"ECG Lead Option\",\"type\":\"select\",\"options\":[\"3-Lead\",\"5-Lead\",\"12-Lead Diagnostic\"],\"required\":true},\n                            {\"name\":\"Battery\",\"label\":\"Battery Autonomy\",\"type\":\"text\",\"required\":false,\"placeholder\":\"e.g. 4 Hours Lithium-ion\"},\n                            {\"name\":\"Telemetry\",\"label\":\"Central Telemetry Link\",\"type\":\"select\",\"options\":[\"Wired Ethernet\",\"Wireless 5GHz Wi-Fi\",\"Dual Link\"],\"required\":true}\n                        ]');
INSERT INTO `device_forms` VALUES (7, 'ICU Ventilator', 'BIOMEDICAL', '[\n                            {\"name\":\"VentModes\",\"label\":\"Ventilation Modes\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. VC-CMV, PC-BIPAP, PSV, APRV, CPAP/NIV\"},\n                            {\"name\":\"PatientRange\",\"label\":\"Patient Demographic\",\"type\":\"select\",\"options\":[\"Adult & Pediatric\",\"Neonatal Specialized\",\"Universal (Neonatal to Adult)\"],\"required\":true},\n                            {\"name\":\"GasSupply\",\"label\":\"Gas Supply Requirements\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. Pipeline O2 & Medical Air (2.8 - 6 bar)\"},\n                            {\"name\":\"InternalBattery\",\"label\":\"Battery Backup Time\",\"type\":\"text\",\"required\":false,\"placeholder\":\"e.g. 180 Minutes Internal Li-Ion\"}\n                        ]');
INSERT INTO `device_forms` VALUES (8, 'Defibrillator', 'BIOMEDICAL', '[\n                            {\"name\":\"Waveform\",\"label\":\"Defibrillation Waveform\",\"type\":\"select\",\"options\":[\"Biphasic Truncated Exponential\",\"Rectilinear Biphasic\"],\"required\":true},\n                            {\"name\":\"Pacing\",\"label\":\"External Transcutaneous Pacing\",\"type\":\"select\",\"options\":[\"Yes - Demand and Fixed\",\"No\"],\"required\":true},\n                            {\"name\":\"CPRFeedback\",\"label\":\"Real-time CPR Sensor Feedback\",\"type\":\"select\",\"options\":[\"Equipped\",\"Not Equipped\"],\"required\":true},\n                            {\"name\":\"BatteryType\",\"label\":\"Rechargeable Battery Chemistry\",\"type\":\"text\",\"required\":false,\"placeholder\":\"e.g. Smart Rechargeable Lithium-ion\"}\n                        ]');
INSERT INTO `device_forms` VALUES (9, 'Ultrasound Scanner', 'BIOMEDICAL', '[\n                            {\"name\":\"Probes\",\"label\":\"Included Transducer Probes\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. Convex Abdominal, Linear Vascular, Phased Cardiac\"},\n                            {\"name\":\"ProbePorts\",\"label\":\"Active Transducer Ports\",\"type\":\"select\",\"options\":[\"2 Active Ports\",\"3 Active Ports\",\"4 Active Ports\"],\"required\":true},\n                            {\"name\":\"Doppler\",\"label\":\"Doppler Capabilities\",\"type\":\"text\",\"required\":false,\"placeholder\":\"e.g. Color Doppler, Pulsed Wave, Continuous Wave, Power Doppler\"},\n                            {\"name\":\"Display\",\"label\":\"Monitor Specification\",\"type\":\"text\",\"required\":false,\"placeholder\":\"e.g. 21.5-inch OLED Articulating Arm\"}\n                        ]');
INSERT INTO `device_forms` VALUES (10, 'Mobile X-Ray', 'BIOMEDICAL', '[\n                            {\"name\":\"GeneratorOutput\",\"label\":\"Generator Output (kW)\",\"type\":\"select\",\"options\":[\"15 kW High Frequency\",\"30 kW High Frequency\",\"35 kW High Frequency\",\"50 kW Ultra High Frequency\"],\"required\":true},\n                            {\"name\":\"Detector\",\"label\":\"Flat Panel Detector (FPD)\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. 14x17 inch Wireless Cesium Iodide (CsI)\"},\n                            {\"name\":\"DriveSystem\",\"label\":\"Motorized Travel Assist\",\"type\":\"select\",\"options\":[\"Dual Motorized Drive\",\"Manual Push\"],\"required\":true}\n                        ]');
INSERT INTO `device_forms` VALUES (11, 'Facility UPS', 'GENERAL', '[\n                            {\"name\":\"Capacity\",\"label\":\"Apparent / Active Power\",\"type\":\"select\",\"options\":[\"10 kVA / 9 kW\",\"20 kVA / 18 kW\",\"40 kVA / 36 kW\",\"80 kVA / 72 kW\",\"120 kVA / 108 kW\"],\"required\":true},\n                            {\"name\":\"BatteryBank\",\"label\":\"Battery Bank Specification\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. 32x 12V 100Ah VRLA Batteries\"},\n                            {\"name\":\"Autonomy\",\"label\":\"Full Load Autonomy Time\",\"type\":\"text\",\"required\":false,\"placeholder\":\"e.g. 45 Minutes Critical Hospital Load\"},\n                            {\"name\":\"InputVoltage\",\"label\":\"Nominal Input / Output\",\"type\":\"select\",\"options\":[\"380/400V 3-Phase 4-Wire\",\"220/230V Single Phase\"],\"required\":true}\n                        ]');
INSERT INTO `device_forms` VALUES (12, 'HVAC Chiller', 'GENERAL', '[\n                            {\"name\":\"CoolingTons\",\"label\":\"Cooling Capacity (TR / kW)\",\"type\":\"select\",\"options\":[\"25 TR (88 kW)\",\"50 TR (175 kW)\",\"100 TR (350 kW)\",\"200 TR (700 kW)\"],\"required\":true},\n                            {\"name\":\"Refrigerant\",\"label\":\"Refrigerant Gas\",\"type\":\"select\",\"options\":[\"R-410A Eco\",\"R-134a\",\"R-32\",\"R-407C\"],\"required\":true},\n                            {\"name\":\"CompressorCount\",\"label\":\"Compressor Circuit Count\",\"type\":\"select\",\"options\":[\"Single Scroll\",\"Dual Scroll\",\"Dual Screw Hermetic\"],\"required\":true},\n                            {\"name\":\"PowerFeed\",\"label\":\"Main Electrical Supply\",\"type\":\"text\",\"required\":false,\"placeholder\":\"e.g. 380V / 3Ph / 50Hz, 125A Breaker\"}\n                        ]');
INSERT INTO `device_forms` VALUES (13, 'Emergency Generator', 'GENERAL', '[\n                            {\"name\":\"PrimeRating\",\"label\":\"Prime Power Rating (kVA)\",\"type\":\"select\",\"options\":[\"150 kVA\",\"250 kVA\",\"500 kVA\",\"750 kVA\",\"1000 kVA\"],\"required\":true},\n                            {\"name\":\"EngineMake\",\"label\":\"Diesel Engine Brand / Model\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. Perkins 2506A / Cummins QSK23\"},\n                            {\"name\":\"ATS\",\"label\":\"Automatic Transfer Switch\",\"type\":\"text\",\"required\":false,\"placeholder\":\"e.g. ASCO 7000 Series 4-Pole 800A\"},\n                            {\"name\":\"FuelTank\",\"label\":\"Base Fuel Tank Capacity\",\"type\":\"text\",\"required\":false,\"placeholder\":\"e.g. 1000 Liters (12 hours runtime)\"}\n                        ]');
INSERT INTO `device_forms` VALUES (14, 'Medical Gas Manifold', 'GENERAL', '[\n                            {\"name\":\"GasService\",\"label\":\"Medical Gas Type\",\"type\":\"select\",\"options\":[\"Medical Oxygen (O2)\",\"Medical Air 4-Bar\",\"Surgical Air 7-Bar\",\"Nitrous Oxide (N2O)\",\"Medical Vacuum\"],\"required\":true},\n                            {\"name\":\"BankSize\",\"label\":\"Cylinder Bank Configuration\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. 2x 10-Cylinder Dual Bank Auto-Changeover\"},\n                            {\"name\":\"SupplyPressure\",\"label\":\"Regulated Distribution Pressure\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. 4.2 bar (60 psig)\"},\n                            {\"name\":\"ReserveSupply\",\"label\":\"Emergency Reserve Bank\",\"type\":\"select\",\"options\":[\"Third Emergency Bank Attached\",\"Standalone High-Pressure Pack\"],\"required\":false}\n                        ]');
INSERT INTO `device_forms` VALUES (15, 'Cleanroom HEPA Filter', 'GENERAL', '[\n                            {\"name\":\"Efficiency\",\"label\":\"Filter Rating / Efficiency\",\"type\":\"select\",\"options\":[\"H14 HEPA 99.995%\",\"U15 ULPA 99.9995%\",\"H13 HEPA 99.95%\"],\"required\":true},\n                            {\"name\":\"FlowRate\",\"label\":\"Rated Airflow Volume\",\"type\":\"text\",\"required\":true,\"placeholder\":\"e.g. 1200 CFM (2000 m3/h)\"},\n                            {\"name\":\"PressureGauge\",\"label\":\"Magnehelic Differential Gauge\",\"type\":\"select\",\"options\":[\"Installed (0-500 Pa)\",\"Digital Sensor to BMS\"],\"required\":true},\n                            {\"name\":\"LocationServed\",\"label\":\"Hospital Critical Area\",\"type\":\"text\",\"required\":false,\"placeholder\":\"e.g. Operating Theatre 1 / Bone Marrow Transplant Room\"}\n                        ]');
INSERT INTO `device_forms` VALUES (16, 'Laptop', 'BIOMEDICAL', '[{\"name\":\"Model\",\"label\":\"Model / Series\",\"type\":\"text\",\"options\":[],\"required\":true},{\"name\":\"Spec1\",\"label\":\"Primary Specification\",\"type\":\"select\",\"options\":[\"a\",\"b\",\"c\",\"d\"],\"required\":false}]');

-- ----------------------------
-- Table structure for rooms
-- ----------------------------
DROP TABLE IF EXISTS `rooms`;
CREATE TABLE `rooms`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `roomNo` int NULL DEFAULT NULL,
  `floor` int NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 7 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of rooms
-- ----------------------------
INSERT INTO `rooms` VALUES (1, 358, 2);
INSERT INTO `rooms` VALUES (2, 102, 1);
INSERT INTO `rooms` VALUES (3, 105, 1);
INSERT INTO `rooms` VALUES (4, 201, 2);
INSERT INTO `rooms` VALUES (5, 301, 3);
INSERT INTO `rooms` VALUES (6, 256, 1);

-- ----------------------------
-- Table structure for suppliers
-- ----------------------------
DROP TABLE IF EXISTS `suppliers`;
CREATE TABLE `suppliers`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `isActive` tinyint(1) NULL DEFAULT 1,
  `type` int NULL DEFAULT NULL COMMENT 'items, maintenance, both',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 7 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of suppliers
-- ----------------------------
INSERT INTO `suppliers` VALUES (1, 'Dell Technologies', 1, 3);
INSERT INTO `suppliers` VALUES (2, 'Philips Healthcare', 1, 3);
INSERT INTO `suppliers` VALUES (3, 'HP Enterprise', 1, 1);
INSERT INTO `suppliers` VALUES (4, 'Cisco Systems', 1, 3);
INSERT INTO `suppliers` VALUES (5, 'GE Healthcare', 1, 3);
INSERT INTO `suppliers` VALUES (6, 'Carrier & Facility Engineering', 1, 2);

-- ----------------------------
-- Table structure for ticket_handler
-- ----------------------------
DROP TABLE IF EXISTS `ticket_handler`;
CREATE TABLE `ticket_handler`  (
  `ticketID` int NOT NULL AUTO_INCREMENT,
  `techUserID` int NULL DEFAULT NULL,
  `assignedDT` datetime NULL DEFAULT NULL,
  PRIMARY KEY (`ticketID`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 11 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of ticket_handler
-- ----------------------------
INSERT INTO `ticket_handler` VALUES (1, 2, '2026-10-21 22:15:19');
INSERT INTO `ticket_handler` VALUES (10, 1, '2026-10-23 11:20:38');

-- ----------------------------
-- Table structure for ticket_msg
-- ----------------------------
DROP TABLE IF EXISTS `ticket_msg`;
CREATE TABLE `ticket_msg`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `ticketID` int NULL DEFAULT NULL,
  `userID` int NULL DEFAULT NULL,
  `msg` varchar(1023) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `timeStamp` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 25 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of ticket_msg
-- ----------------------------
INSERT INTO `ticket_msg` VALUES (1, 1, 3, 'Laptop displays blue screen (CRITICAL_PROCESS_DIED) shortly after booting into Windows.', '2026-10-21 20:15:19');
INSERT INTO `ticket_msg` VALUES (2, 1, 2, 'Hardware diagnostics run. NVMe SSD shows bad SMART sectors. Ordering replacement drive under warranty.', '2026-10-21 23:15:19');
INSERT INTO `ticket_msg` VALUES (3, 2, 4, 'Periodic preventive calibration is overdue by 10 days. Image noise detected on linear probe.', '2026-10-22 10:15:19');
INSERT INTO `ticket_msg` VALUES (4, 2, 1, 'hello, this is a test reply', '2026-10-22 14:30:51');
INSERT INTO `ticket_msg` VALUES (5, 2, 1, 'hi', '2026-10-22 14:45:27');
INSERT INTO `ticket_msg` VALUES (6, 4, 1, 'chdghdfgh', '2026-10-22 14:48:42');
INSERT INTO `ticket_msg` VALUES (7, 5, 1, '?️ AUTOMATED SYSTEM & WORKSTATION REPORT\r\n• Workstation / PC Name: DESKTOP-P5717HQ\r\n• Location: Room 101 (Floor 1)\r\n• Custodian / User: Abbas Al-Amiri\r\n• Equipment: lenovo Model 331 [S/N: 2B1E900E-99CF-49C2-946D-F4F8226957D5]\r\n• Reporter: Abbas Al-Amiri (Abbass)\r\n• Department: Information Technology\r\n• Client Host / IP: 127.0.0.1\r\n• Timestamp: 2026-10-23 10:19:09\r\n', '2026-10-23 10:19:09');
INSERT INTO `ticket_msg` VALUES (8, 5, 1, '? USER NOTES:\nAutomated test note: Monitor flickers intermittently and requires display cable inspection.', '2026-10-23 10:19:10');
INSERT INTO `ticket_msg` VALUES (9, 6, 1, '🖥️ AUTOMATED SYSTEM & WORKSTATION REPORT\r\n• Workstation / PC Name: DESKTOP-P5717HQ\r\n• Location: Room 101 (Floor 1)\r\n• Custodian / User: Abbas Al-Amiri\r\n• Equipment: lenovo Model 331 [S/N: 2B1E900E-99CF-49C2-946D-F4F8226957D5]\r\n• Reporter: Abbas Al-Amiri (Abbass)\r\n• Department: Information Technology\r\n• Client Host / IP: ::1\r\n• Timestamp: 2026-10-23 10:23:39\r\n', '2026-10-23 10:23:39');
INSERT INTO `ticket_msg` VALUES (10, 7, 1, '🖥️ AUTOMATED SYSTEM & WORKSTATION REPORT (LOGIN FAILURE)\r\n• Workstation / PC Name : DESKTOP-P5717HQ\r\n• Location              : Room 101 (Floor 1)\r\n• Hardware Details      : lenovo Model 331 [S/N: 2B1E900E-99CF-49C2-946D-F4F8226957D5]\r\n• Locked-out User       : Abbas (Registered ID: 1)\r\n• Client Host / IP      : ::1\r\n• Timestamp             : 2026-10-23 10:39:34\r\n', '2026-10-23 10:39:34');
INSERT INTO `ticket_msg` VALUES (11, 7, 1, '📝 USER NOTES:\nAccess Denied Test: User Abbas is locked out. Please activate my account for daily tasks.', '2026-10-23 10:39:35');
INSERT INTO `ticket_msg` VALUES (12, 8, 1, '[ON BEHALF OF ASSET / PC]: HOSP-US-004 Ultrasound Scanner in Room 201\r\n\r\nUltrasound transducer cable is intermittent and shows artifact on display.', '2026-10-23 10:41:48');
INSERT INTO `ticket_msg` VALUES (13, 9, 1, '🖥️ AUTOMATED SYSTEM & WORKSTATION REPORT (LOGIN FAILURE)\r\n• Workstation / PC Name : DESKTOP-P5717HQ\r\n• Location              : Room 358 (Floor 2)\r\n• Hardware Details      : lenovo Model 331 [S/N: 2B1E900E-99CF-49C2-946D-F4F8226957D5]\r\n• Locked-out User       : Abbas (Registered ID: 1)\r\n• Client Host / IP      : ::1\r\n• Timestamp             : 2026-10-23 11:17:26\r\n', '2026-10-23 11:17:26');
INSERT INTO `ticket_msg` VALUES (14, 9, 1, '📝 USER NOTES:\nPlease activate my account for workstation DESKTOP-P5717HQ. I require access for daily operations.', '2026-10-23 11:17:27');
INSERT INTO `ticket_msg` VALUES (15, 10, 1, 'drytj', '2026-10-23 11:20:38');
INSERT INTO `ticket_msg` VALUES (16, 10, 1, '[RESOLUTION NOTE]: vb', '2026-10-23 11:20:48');
INSERT INTO `ticket_msg` VALUES (17, 10, 1, 'fgh', '2026-10-23 11:20:55');
INSERT INTO `ticket_msg` VALUES (18, 11, 6, 'sf', '2026-10-23 12:33:49');
INSERT INTO `ticket_msg` VALUES (19, 12, 6, 'fgf', '2026-10-23 12:34:08');
INSERT INTO `ticket_msg` VALUES (20, 13, 6, 'dsfg', '2026-10-23 12:34:25');
INSERT INTO `ticket_msg` VALUES (21, 14, 6, 'dzfsdfg', '2026-10-23 12:34:46');
INSERT INTO `ticket_msg` VALUES (22, 15, 3, 'Automated test: Patient monitor sensor lead disconnected on ICU bed 4', '2026-10-23 12:47:35');
INSERT INTO `ticket_msg` VALUES (23, 16, 3, 'Automated test: Operating room 2 temperature is 26C, chiller not cooling.', '2026-10-23 12:47:50');
INSERT INTO `ticket_msg` VALUES (24, 17, 6, 'gxh', '2026-10-23 13:28:02');

-- ----------------------------
-- Table structure for ticket_status
-- ----------------------------
DROP TABLE IF EXISTS `ticket_status`;
CREATE TABLE `ticket_status`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `status` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 6 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of ticket_status
-- ----------------------------
INSERT INTO `ticket_status` VALUES (1, 'opened');
INSERT INTO `ticket_status` VALUES (2, 'pending');
INSERT INTO `ticket_status` VALUES (3, 'delayed');
INSERT INTO `ticket_status` VALUES (4, 'canceled');
INSERT INTO `ticket_status` VALUES (5, 'closed');

-- ----------------------------
-- Table structure for tickets
-- ----------------------------
DROP TABLE IF EXISTS `tickets`;
CREATE TABLE `tickets`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `assetID` int NULL DEFAULT NULL,
  `openedBy` int NULL DEFAULT NULL,
  `onBehalfOf` int NULL DEFAULT NULL,
  `type` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL COMMENT 'software or hardware',
  `status` int NULL DEFAULT NULL COMMENT 'active, pending, delayed, closed',
  `priority` int NULL DEFAULT NULL,
  `openedDT` datetime NULL DEFAULT NULL,
  `closedDT` datetime NULL DEFAULT NULL,
  `target_team` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'IT',
  `resolution_details` varchar(1024) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 18 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of tickets
-- ----------------------------
INSERT INTO `tickets` VALUES (1, 3, 3, 3, 'Hardware', 2, 3, '2026-10-21 20:15:19', NULL, 'IT', NULL);
INSERT INTO `tickets` VALUES (2, 2, 4, 4, 'Maintenance', 1, 4, '2026-10-22 10:15:19', NULL, 'BIOMEDICAL', NULL);
INSERT INTO `tickets` VALUES (3, 1, 1, 1, 'Hardware', 5, 2, '2026-10-12 14:15:19', '2026-10-14 14:15:19', 'IT', NULL);
INSERT INTO `tickets` VALUES (4, 1, 1, 6, 'Network', 1, 4, '2026-10-22 14:48:42', NULL, 'IT', NULL);
INSERT INTO `tickets` VALUES (5, 5, 1, 1, 'Hardware', 1, 2, '2026-10-23 10:19:08', NULL, 'IT', NULL);
INSERT INTO `tickets` VALUES (6, 5, 1, 1, 'Hardware', 1, 2, '2026-10-23 10:23:39', NULL, 'IT', NULL);
INSERT INTO `tickets` VALUES (7, 5, 1, 1, 'Access', 1, 3, '2026-10-23 10:39:34', NULL, 'IT', NULL);
INSERT INTO `tickets` VALUES (8, NULL, 1, 3, 'Hardware', 1, 3, '2026-10-23 10:41:48', NULL, 'IT', NULL);
INSERT INTO `tickets` VALUES (9, 5, 1, 1, 'Access', 1, 3, '2026-10-23 11:17:26', NULL, 'IT', NULL);
INSERT INTO `tickets` VALUES (10, NULL, 1, 2, 'Hardware', 5, 3, '2026-10-23 11:20:38', '2026-10-23 11:20:48', 'IT', NULL);
INSERT INTO `tickets` VALUES (11, NULL, 6, 6, 'Hardware', 1, 2, '2026-10-23 12:33:49', NULL, 'BIOMEDICAL', NULL);
INSERT INTO `tickets` VALUES (12, 22, 6, 6, 'Hardware', 1, 2, '2026-10-23 12:34:08', NULL, 'BIOMEDICAL', NULL);
INSERT INTO `tickets` VALUES (13, 5, 6, 6, 'Hardware', 1, 2, '2026-10-23 12:34:25', NULL, 'IT', NULL);
INSERT INTO `tickets` VALUES (14, 13, 6, 6, 'Hardware', 1, 2, '2026-10-23 12:34:46', NULL, 'GENERAL', NULL);
INSERT INTO `tickets` VALUES (15, 2, 3, 3, 'Patient Monitor / Sensor', 1, 3, '2026-10-23 12:47:35', NULL, 'BIOMEDICAL', NULL);
INSERT INTO `tickets` VALUES (16, NULL, 3, 3, 'HVAC & Temperature', 1, 3, '2026-10-23 12:47:50', NULL, 'GENERAL', NULL);
INSERT INTO `tickets` VALUES (17, NULL, 6, 6, 'Hardware Breakdown', 1, 2, '2026-10-23 13:28:01', NULL, 'IT', NULL);

-- ----------------------------
-- Table structure for users
-- ----------------------------
DROP TABLE IF EXISTS `users`;
CREATE TABLE `users`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `fullName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `departmentID` int NULL DEFAULT NULL,
  `domainUser` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `role` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `isActive` tinyint(1) NULL DEFAULT 1,
  `maintenance_team` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 7 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of users
-- ----------------------------
INSERT INTO `users` VALUES (1, 'Abbass chd', 1, 'Abbas', 'Admin', 1, 'IT');
INSERT INTO `users` VALUES (2, 'Ahmed Mansour', 1, 'a.mansour', 'MAINTENANCE', 1, 'IT');
INSERT INTO `users` VALUES (3, 'Dr. Sarah Jenkins', 3, 's.jenkins', 'END_USER', 1, NULL);
INSERT INTO `users` VALUES (4, 'Dr. Elena Rostova', 2, 'e.rostova', 'END_USER', 1, NULL);
INSERT INTO `users` VALUES (5, 'Tariq Ziad', 4, 't.ziad', 'MAINTENANCE', 1, 'BIOMEDICAL');
INSERT INTO `users` VALUES (6, 'Hasan Outa', 5, 'hOuta', 'MAINTENANCE', 1, 'GENERAL');

SET FOREIGN_KEY_CHECKS = 1;
