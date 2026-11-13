<?php
/**
 * SOLVIS MaintenanceController
 * Scheduled & Preventive Maintenance, Overdue Alerts, Vendor Dispatches & Custody Auditing
 */

require_once __DIR__ . '/../Database.php';
require_once __DIR__ . '/../Auth.php';
require_once __DIR__ . '/../Helpers.php';

class MaintenanceController {
    public function index(): void {
        $db = Database::getConnection();

        $today = date('Y-m-d');
        $in30Days = date('Y-m-d', strtotime('+30 days'));

        // 1. Active External Dispatches (at Supplier)
        $activeWorkOrders = $db->query("
            SELECT m.*, a.hospitalNo, a.brand, a.modelNo, a.serialNo, s.name as vendorName, s.phone as vendorPhone
            FROM asset_maintenances m
            JOIN assets a ON m.assetId = a.id
            LEFT JOIN suppliers s ON m.vendorId = s.id
            WHERE m.status = 'Active'
            ORDER BY m.id DESC
        ")->fetchAll();

        // 2. Overdue Preventive Inspections
        $overdueAssets = $db->query("
            SELECT a.*, r.name as roomName, r.roomNo, u.fullName as assignedUserName, s.name as supplierName
            FROM assets a
            LEFT JOIN rooms r ON a.roomId = r.id
            LEFT JOIN users u ON a.userId = u.id
            LEFT JOIN suppliers s ON a.supplierId = s.id
            WHERE a.needsPeriodic = 1 AND a.nextPeriodic IS NOT NULL AND a.nextPeriodic < '$today'
            ORDER BY a.nextPeriodic ASC
        ")->fetchAll();

        // 3. Due within 30 days
        $dueWithin30DaysAssets = $db->query("
            SELECT a.*, r.name as roomName, r.roomNo, u.fullName as assignedUserName
            FROM assets a
            LEFT JOIN rooms r ON a.roomId = r.id
            LEFT JOIN users u ON a.userId = u.id
            WHERE a.needsPeriodic = 1 AND a.nextPeriodic >= '$today' AND a.nextPeriodic <= '$in30Days'
            ORDER BY a.nextPeriodic ASC
        ")->fetchAll();

        // 4. Valid Calibrated Assets
        $validCalibratedAssets = $db->query("
            SELECT a.*, r.name as roomName, r.roomNo, u.fullName as assignedUserName
            FROM assets a
            LEFT JOIN rooms r ON a.roomId = r.id
            LEFT JOIN users u ON a.userId = u.id
            WHERE a.needsPeriodic = 1 AND a.nextPeriodic > '$in30Days'
            ORDER BY a.nextPeriodic ASC
        ")->fetchAll();

        // Dropdowns for modals
        $allAssets = $db->query("SELECT id, hospitalNo, brand, modelNo, type FROM assets WHERE status != 'Retired' ORDER BY hospitalNo ASC")->fetchAll();
        $suppliers = $db->query("SELECT * FROM suppliers ORDER BY name ASC")->fetchAll();

        require __DIR__ . '/../../templates/maintenance/index.php';
    }

    public function sendToSupplier(): void {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            header('Location: ' . url('maintenance'));
            exit;
        }

        $db = Database::getConnection();
        $assetId = (int)($_POST['assetId'] ?? 0);
        $vendorId = (int)($_POST['vendorId'] ?? 0);
        $cost = (float)($_POST['cost'] ?? 0);
        $description = trim($_POST['description'] ?? 'Dispatched for external maintenance/calibration');

        if ($assetId > 0 && $vendorId > 0) {
            // Update asset status
            $db->prepare("UPDATE assets SET status = 'Under Maintenance', custodyTypeId = 3 WHERE id = ?")->execute([$assetId]);

            // Create maintenance work order
            $stmt = $db->prepare("
                INSERT INTO asset_maintenances (assetId, vendorId, dispatchedDate, cost, description, status)
                VALUES (?, ?, date('now'), ?, ?, 'Active')
            ");
            $stmt->execute([$assetId, $vendorId, $cost, $description]);

            // Add custody history
            $stmtH = $db->prepare("
                INSERT INTO asset_history (assetId, action, notes, changedBy, timestamp)
                VALUES (?, 'Vendor Custody Transfer', ?, ?, datetime('now'))
            ");
            $vendorName = $db->query("SELECT name FROM suppliers WHERE id = $vendorId")->fetchColumn();
            $notes = "Transferred to external supplier {$vendorName}: {$description}";
            $stmtH->execute([$assetId, $notes, Auth::name()]);

            flash('success', 'Equipment successfully dispatched to external vendor.');
        }

        header('Location: ' . url('maintenance'));
        exit;
    }

    public function returnFromSupplier(): void {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            header('Location: ' . url('maintenance'));
            exit;
        }

        $db = Database::getConnection();
        $maintenanceId = (int)($_POST['maintenanceId'] ?? 0);

        if ($maintenanceId > 0) {
            $maint = $db->query("SELECT * FROM asset_maintenances WHERE id = $maintenanceId")->fetch();
            if ($maint) {
                // Mark maintenance as Completed
                $db->prepare("UPDATE asset_maintenances SET status = 'Completed', returnedDate = date('now') WHERE id = ?")->execute([$maintenanceId]);

                // Restore asset status
                $db->prepare("UPDATE assets SET status = 'In Service', custodyTypeId = 2 WHERE id = ?")->execute([$maint['assetId']]);

                // Custody history
                $db->prepare("
                    INSERT INTO asset_history (assetId, action, notes, changedBy, timestamp)
                    VALUES (?, 'Return to Hospital Custody', 'Returned from external supplier servicing and restored to active hospital duty', ?, datetime('now'))
                ")->execute([$maint['assetId'], Auth::name()]);

                flash('success', 'Asset successfully received back from vendor and restored to service.');
            }
        }

        header('Location: ' . url('maintenance'));
        exit;
    }

    public function logInspection(): void {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            header('Location: ' . url('maintenance'));
            exit;
        }

        $db = Database::getConnection();
        $assetId = (int)($_POST['assetId'] ?? 0);
        $notes = trim($_POST['notes'] ?? 'Periodic preventive inspection completed');

        if ($assetId > 0) {
            $asset = $db->query("SELECT periodicInterval FROM assets WHERE id = $assetId")->fetch();
            $interval = (int)($asset['periodicInterval'] ?? 90);
            if ($interval <= 0) $interval = 90;

            $nextDate = date('Y-m-d', strtotime("+$interval days"));

            $db->prepare("
                UPDATE assets SET 
                    lastPeriodic = date('now'),
                    nextPeriodic = ?,
                    status = 'In Service'
                WHERE id = ?
            ")->execute([$nextDate, $assetId]);

            // Add history record
            $db->prepare("
                INSERT INTO asset_history (assetId, action, notes, changedBy, timestamp)
                VALUES (?, 'Preventive Inspection Certified', ?, ?, datetime('now'))
            ")->execute([$assetId, $notes, Auth::name()]);

            flash('success', "Preventive maintenance recorded! Next scheduled inspection: {$nextDate}");
        }

        header('Location: ' . url('maintenance'));
        exit;
    }
}
