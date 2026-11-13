<?php
/**
 * SOLVIS AssetsController
 * Hospital Equipment & Asset Registry, Custody Audit, and Thermal Label Printing
 */

require_once __DIR__ . '/../Database.php';
require_once __DIR__ . '/../Auth.php';
require_once __DIR__ . '/../Helpers.php';

class AssetsController {
    public function index(): void {
        $db = Database::getConnection();
        
        $search = trim($_GET['search'] ?? '');
        $status = trim($_GET['status'] ?? '');
        $type = trim($_GET['type'] ?? '');
        $roomId = !empty($_GET['roomId']) ? (int)$_GET['roomId'] : null;
        $categoryId = !empty($_GET['categoryId']) ? (int)$_GET['categoryId'] : null;
        $calibration = trim($_GET['calibration'] ?? '');

        // Query builders
        $where = ["1=1"];
        $params = [];

        if ($search !== '') {
            $where[] = "(a.hospitalNo LIKE ? OR a.serialNo LIKE ? OR a.brand LIKE ? OR a.modelNo LIKE ? OR u.fullName LIKE ?)";
            $term = "%$search%";
            $params = array_merge($params, [$term, $term, $term, $term, $term]);
        }

        if ($status !== '') {
            $where[] = "a.status = ?";
            $params[] = $status;
        }

        if ($type !== '') {
            $where[] = "a.type = ?";
            $params[] = $type;
        }

        if ($roomId !== null) {
            $where[] = "a.roomId = ?";
            $params[] = $roomId;
        }

        if ($categoryId !== null) {
            $where[] = "a.categoryId = ?";
            $params[] = $categoryId;
        }

        $today = date('Y-m-d');
        $in30Days = date('Y-m-d', strtotime('+30 days'));

        if ($calibration === 'required') {
            $where[] = "a.needsPeriodic = 1";
        } elseif ($calibration === 'overdue') {
            $where[] = "a.needsPeriodic = 1 AND a.nextPeriodic IS NOT NULL AND a.nextPeriodic < '$today'";
        } elseif ($calibration === 'due30') {
            $where[] = "a.needsPeriodic = 1 AND a.nextPeriodic >= '$today' AND a.nextPeriodic <= '$in30Days'";
        } elseif ($calibration === 'valid') {
            $where[] = "a.needsPeriodic = 1 AND a.nextPeriodic > '$in30Days'";
        }

        $sql = "
            SELECT a.*, r.name as roomName, r.roomNo, u.fullName as assignedUserName, 
                   c.type as custodyName, s.name as supplierName, cat.name as categoryName
            FROM assets a
            LEFT JOIN rooms r ON a.roomId = r.id
            LEFT JOIN users u ON a.userId = u.id
            LEFT JOIN custody_type c ON a.custodyTypeId = c.id
            LEFT JOIN suppliers s ON a.supplierId = s.id
            LEFT JOIN asset_categories cat ON a.categoryId = cat.id
            WHERE " . implode(' AND ', $where) . "
            ORDER BY a.id DESC
        ";

        $stmt = $db->prepare($sql);
        $stmt->execute($params);
        $assets = $stmt->fetchAll();

        // Dropdown lookups
        $rooms = $db->query("SELECT * FROM rooms ORDER BY roomNo ASC")->fetchAll();
        $categories = $db->query("SELECT * FROM asset_categories ORDER BY id ASC")->fetchAll();
        $types = $db->query("SELECT DISTINCT type FROM assets WHERE type IS NOT NULL AND type != '' ORDER BY type ASC")->fetchAll(PDO::FETCH_COLUMN);

        require __DIR__ . '/../../templates/assets/index.php';
    }

    public function details(): void {
        $db = Database::getConnection();
        $id = (int)($_GET['id'] ?? 0);

        $stmt = $db->prepare("
            SELECT a.*, r.name as roomName, r.roomNo, r.floor, u.fullName as assignedUserName, u.email as assignedUserEmail,
                   c.type as custodyName, s.name as supplierName, s.phone as supplierPhone, s.email as supplierEmail,
                   cat.name as categoryName, cat.name_ar as categoryNameAr
            FROM assets a
            LEFT JOIN rooms r ON a.roomId = r.id
            LEFT JOIN users u ON a.userId = u.id
            LEFT JOIN custody_type c ON a.custodyTypeId = c.id
            LEFT JOIN suppliers s ON a.supplierId = s.id
            LEFT JOIN asset_categories cat ON a.categoryId = cat.id
            WHERE a.id = ?
        ");
        $stmt->execute([$id]);
        $asset = $stmt->fetch();

        if (!$asset) {
            flash('error', 'Asset not found.');
            header('Location: ' . url('assets'));
            exit;
        }

        // Custody history
        $stmtHist = $db->prepare("SELECT * FROM asset_history WHERE assetId = ? ORDER BY id DESC");
        $stmtHist->execute([$id]);
        $history = $stmtHist->fetchAll();

        // Linked tickets
        $stmtTix = $db->prepare("
            SELECT t.*, u.fullName as createdByName
            FROM tickets t
            LEFT JOIN users u ON t.createdByUserId = u.id
            WHERE t.assetId = ?
            ORDER BY t.id DESC
        ");
        $stmtTix->execute([$id]);
        $tickets = $stmtTix->fetchAll();

        // External maintenance work orders
        $stmtMaint = $db->prepare("
            SELECT m.*, s.name as vendorName
            FROM asset_maintenances m
            LEFT JOIN suppliers s ON m.vendorId = s.id
            WHERE m.assetId = ?
            ORDER BY m.id DESC
        ");
        $stmtMaint->execute([$id]);
        $maintenances = $stmtMaint->fetchAll();

        require __DIR__ . '/../../templates/assets/details.php';
    }

    public function create(): void {
        $db = Database::getConnection();

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $hospitalNo = trim($_POST['hospitalNo'] ?? '');
            $brand = trim($_POST['brand'] ?? '');
            $modelNo = trim($_POST['modelNo'] ?? '');
            $serialNo = trim($_POST['serialNo'] ?? '');
            $type = trim($_POST['type'] ?? '');
            $status = trim($_POST['status'] ?? 'In Service');
            $price = (float)($_POST['price'] ?? 0);
            $roomId = !empty($_POST['roomId']) ? (int)$_POST['roomId'] : null;
            $userId = !empty($_POST['userId']) ? (int)$_POST['userId'] : null;
            $supplierId = !empty($_POST['supplierId']) ? (int)$_POST['supplierId'] : null;
            $categoryId = !empty($_POST['categoryId']) ? (int)$_POST['categoryId'] : null;
            $maintenanceTeam = trim($_POST['maintenanceTeam'] ?? 'IT');
            $specs = trim($_POST['specs'] ?? '');
            $buyingDate = !empty($_POST['buyingDate']) ? $_POST['buyingDate'] : date('Y-m-d');
            $warrantyTill = !empty($_POST['warrantyTill']) ? $_POST['warrantyTill'] : null;
            $needsPeriodic = isset($_POST['needsPeriodic']) ? 1 : 0;
            $periodicInterval = (int)($_POST['periodicInterval'] ?? 90);

            if ($hospitalNo === '' || $brand === '' || $type === '') {
                flash('error', 'Hospital No, Brand, and Type are required fields.');
            } else {
                $nextPeriodic = null;
                if ($needsPeriodic) {
                    $nextPeriodic = date('Y-m-d', strtotime("+$periodicInterval days"));
                }

                $stmt = $db->prepare("
                    INSERT INTO assets 
                    (hospitalNo, serialNo, brand, modelNo, price, type, status, supplierId, roomId, userId, 
                     specs, buyingDate, warrantyTill, needsPeriodic, periodicInterval, nextPeriodic, maintenanceTeam, categoryId)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                ");
                $stmt->execute([
                    $hospitalNo, $serialNo, $brand, $modelNo, $price, $type, $status, $supplierId, $roomId, $userId,
                    $specs, $buyingDate, $warrantyTill, $needsPeriodic, $periodicInterval, $nextPeriodic, $maintenanceTeam, $categoryId
                ]);
                $assetId = $db->lastInsertId();

                // Log Initial Commissioning
                $stmtH = $db->prepare("
                    INSERT INTO asset_history (assetId, action, notes, changedBy, timestamp, newRoomId, newUserId)
                    VALUES (?, 'Initial Commissioning', 'Asset created via SOLVIS Live Portal', ?, datetime('now'), ?, ?)
                ");
                $stmtH->execute([$assetId, Auth::name(), $roomId, $userId]);

                flash('success', "Asset {$hospitalNo} successfully registered into inventory!");
                header('Location: ' . url('assets/details', ['id' => $assetId]));
                exit;
            }
        }

        $rooms = $db->query("SELECT * FROM rooms ORDER BY roomNo ASC")->fetchAll();
        $users = $db->query("SELECT * FROM users WHERE isActive = 1 ORDER BY fullName ASC")->fetchAll();
        $suppliers = $db->query("SELECT * FROM suppliers ORDER BY name ASC")->fetchAll();
        $categories = $db->query("SELECT * FROM asset_categories ORDER BY id ASC")->fetchAll();

        require __DIR__ . '/../../templates/assets/create.php';
    }

    public function edit(): void {
        $db = Database::getConnection();
        $id = (int)($_GET['id'] ?? 0);

        $stmt = $db->prepare("SELECT * FROM assets WHERE id = ?");
        $stmt->execute([$id]);
        $asset = $stmt->fetch();

        if (!$asset) {
            flash('error', 'Asset not found.');
            header('Location: ' . url('assets'));
            exit;
        }

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $brand = trim($_POST['brand'] ?? '');
            $modelNo = trim($_POST['modelNo'] ?? '');
            $serialNo = trim($_POST['serialNo'] ?? '');
            $type = trim($_POST['type'] ?? '');
            $status = trim($_POST['status'] ?? 'In Service');
            $price = (float)($_POST['price'] ?? 0);
            $roomId = !empty($_POST['roomId']) ? (int)$_POST['roomId'] : null;
            $userId = !empty($_POST['userId']) ? (int)$_POST['userId'] : null;
            $supplierId = !empty($_POST['supplierId']) ? (int)$_POST['supplierId'] : null;
            $categoryId = !empty($_POST['categoryId']) ? (int)$_POST['categoryId'] : null;
            $maintenanceTeam = trim($_POST['maintenanceTeam'] ?? 'IT');
            $specs = trim($_POST['specs'] ?? '');
            $buyingDate = !empty($_POST['buyingDate']) ? $_POST['buyingDate'] : null;
            $warrantyTill = !empty($_POST['warrantyTill']) ? $_POST['warrantyTill'] : null;

            // Detect custody change for audit log
            $custodyChanged = ($asset['roomId'] != $roomId || $asset['userId'] != $userId || $asset['status'] != $status);

            $stmtU = $db->prepare("
                UPDATE assets SET 
                    brand = ?, modelNo = ?, serialNo = ?, type = ?, status = ?, price = ?, 
                    roomId = ?, userId = ?, supplierId = ?, categoryId = ?, maintenanceTeam = ?, 
                    specs = ?, buyingDate = ?, warrantyTill = ?
                WHERE id = ?
            ");
            $stmtU->execute([
                $brand, $modelNo, $serialNo, $type, $status, $price,
                $roomId, $userId, $supplierId, $categoryId, $maintenanceTeam,
                $specs, $buyingDate, $warrantyTill, $id
            ]);

            if ($custodyChanged) {
                $stmtH = $db->prepare("
                    INSERT INTO asset_history (assetId, action, notes, changedBy, timestamp, oldRoomId, newRoomId, oldUserId, newUserId)
                    VALUES (?, 'Custody / Status Update', 'Updated asset location, user custody, or operational status', ?, datetime('now'), ?, ?, ?, ?)
                ");
                $stmtH->execute([$id, Auth::name(), $asset['roomId'], $roomId, $asset['userId'], $userId]);
            }

            flash('success', "Asset {$asset['hospitalNo']} updated successfully!");
            header('Location: ' . url('assets/details', ['id' => $id]));
            exit;
        }

        $rooms = $db->query("SELECT * FROM rooms ORDER BY roomNo ASC")->fetchAll();
        $users = $db->query("SELECT * FROM users WHERE isActive = 1 ORDER BY fullName ASC")->fetchAll();
        $suppliers = $db->query("SELECT * FROM suppliers ORDER BY name ASC")->fetchAll();
        $categories = $db->query("SELECT * FROM asset_categories ORDER BY id ASC")->fetchAll();

        require __DIR__ . '/../../templates/assets/edit.php';
    }

    public function label(): void {
        $db = Database::getConnection();
        $id = (int)($_GET['id'] ?? 0);

        $stmt = $db->prepare("
            SELECT a.*, r.name as roomName, r.roomNo, u.fullName as assignedUserName, d.name as departmentName
            FROM assets a
            LEFT JOIN rooms r ON a.roomId = r.id
            LEFT JOIN users u ON a.userId = u.id
            LEFT JOIN departments d ON u.departmentId = d.id
            WHERE a.id = ?
        ");
        $stmt->execute([$id]);
        $asset = $stmt->fetch();

        if (!$asset) {
            die("Asset not found.");
        }

        require __DIR__ . '/../../templates/assets/label.php';
    }
}
