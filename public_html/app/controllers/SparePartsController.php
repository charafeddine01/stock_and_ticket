<?php
/**
 * SOLVIS SparePartsController
 * Reserve Stock & Spare Parts Inventory, Restock, Commissioning, and Stock Auditing
 */

require_once __DIR__ . '/../Database.php';
require_once __DIR__ . '/../Auth.php';
require_once __DIR__ . '/../Helpers.php';

class SparePartsController {
    public function index(): void {
        $db = Database::getConnection();

        $category = trim($_GET['category'] ?? '');
        $filter = trim($_GET['filter'] ?? '');
        $search = trim($_GET['search'] ?? '');

        $where = ["1=1"];
        $params = [];

        if ($search !== '') {
            $where[] = "(sp.partNo LIKE ? OR sp.name LIKE ? OR sp.name_ar LIKE ? OR sp.location LIKE ?)";
            $term = "%$search%";
            $params = array_merge($params, [$term, $term, $term, $term]);
        }

        if ($category !== '') {
            $where[] = "sp.category = ?";
            $params[] = $category;
        }

        if ($filter === 'low_stock') {
            $where[] = "sp.quantity <= sp.minThreshold";
        } elseif ($filter === 'dual_nature') {
            $where[] = "sp.canBeAsset = 1";
        }

        $sql = "
            SELECT sp.*, s.name as supplierName
            FROM spare_parts sp
            LEFT JOIN suppliers s ON sp.supplierId = s.id
            WHERE " . implode(' AND ', $where) . "
            ORDER BY sp.quantity <= sp.minThreshold DESC, sp.name ASC
        ";

        $stmt = $db->prepare($sql);
        $stmt->execute($params);
        $parts = $stmt->fetchAll();

        // KPI Summary Counters
        $totalItems = (int)$db->query("SELECT COUNT(*) FROM spare_parts")->fetchColumn();
        $totalUnits = (int)$db->query("SELECT SUM(quantity) FROM spare_parts")->fetchColumn();
        $lowStockCount = (int)$db->query("SELECT COUNT(*) FROM spare_parts WHERE quantity <= minThreshold")->fetchColumn();
        $totalValue = (float)$db->query("SELECT SUM(quantity * unitCost) FROM spare_parts")->fetchColumn();

        $categories = $db->query("SELECT DISTINCT category FROM spare_parts ORDER BY category ASC")->fetchAll(PDO::FETCH_COLUMN);
        $suppliers = $db->query("SELECT * FROM suppliers ORDER BY name ASC")->fetchAll();
        $rooms = $db->query("SELECT * FROM rooms ORDER BY roomNo ASC")->fetchAll();
        $users = $db->query("SELECT * FROM users WHERE isActive = 1 ORDER BY fullName ASC")->fetchAll();

        require __DIR__ . '/../../templates/spareparts/index.php';
    }

    public function restock(): void {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            header('Location: ' . url('spareparts'));
            exit;
        }

        $db = Database::getConnection();
        $sparePartId = (int)($_POST['sparePartId'] ?? 0);
        $quantity = (int)($_POST['quantity'] ?? 0);
        $notes = trim($_POST['notes'] ?? 'Restock replenishment');

        if ($sparePartId > 0 && $quantity > 0) {
            $db->prepare("UPDATE spare_parts SET quantity = quantity + ? WHERE id = ?")->execute([$quantity, $sparePartId]);

            $stmtTx = $db->prepare("
                INSERT INTO spare_part_transactions (sparePartId, quantity, type, notes, createdAt, performedByUserId)
                VALUES (?, ?, 'Replenishment', ?, datetime('now'), ?)
            ");
            $stmtTx->execute([$sparePartId, $quantity, $notes, Auth::id()]);

            flash('success', "Stock replenished successfully by {$quantity} units.");
        }

        header('Location: ' . url('spareparts'));
        exit;
    }

    public function create(): void {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            header('Location: ' . url('spareparts'));
            exit;
        }

        $db = Database::getConnection();
        $partNo = trim($_POST['partNo'] ?? '');
        $name = trim($_POST['name'] ?? '');
        $name_ar = trim($_POST['name_ar'] ?? '');
        $category = trim($_POST['category'] ?? 'Spare Parts');
        $quantity = (int)($_POST['quantity'] ?? 0);
        $minThreshold = (int)($_POST['minThreshold'] ?? 5);
        $unitCost = (float)($_POST['unitCost'] ?? 0);
        $location = trim($_POST['location'] ?? '');
        $supplierId = !empty($_POST['supplierId']) ? (int)$_POST['supplierId'] : null;
        $maintenanceTeam = trim($_POST['maintenanceTeam'] ?? 'IT');
        $canBeAsset = isset($_POST['canBeAsset']) ? 1 : 0;

        if ($partNo === '' || $name === '') {
            flash('error', 'Part SKU and Name are required.');
        } else {
            $stmt = $db->prepare("
                INSERT INTO spare_parts 
                (partNo, name, name_ar, category, quantity, minThreshold, unitCost, location, supplierId, maintenanceTeam, canBeAsset)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ");
            $stmt->execute([
                $partNo, $name, $name_ar, $category, $quantity, $minThreshold, $unitCost, $location, $supplierId, $maintenanceTeam, $canBeAsset
            ]);
            $id = $db->lastInsertId();

            if ($quantity > 0) {
                $db->prepare("
                    INSERT INTO spare_part_transactions (sparePartId, quantity, type, notes, createdAt, performedByUserId)
                    VALUES (?, ?, 'Initial Intake', 'Initial stock intake into reserve storage', datetime('now'), ?)
                ")->execute([$id, $quantity, Auth::id()]);
            }

            flash('success', "Reserve item {$name} ({$partNo}) added to inventory.");
        }

        header('Location: ' . url('spareparts'));
        exit;
    }

    public function commissionAsAsset(): void {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            header('Location: ' . url('spareparts'));
            exit;
        }

        $db = Database::getConnection();
        $sparePartId = (int)($_POST['sparePartId'] ?? 0);
        $hospitalNo = trim($_POST['hospitalNo'] ?? '');
        $serialNo = trim($_POST['serialNo'] ?? '');
        $roomId = !empty($_POST['roomId']) ? (int)$_POST['roomId'] : 1;
        $userId = !empty($_POST['userId']) ? (int)$_POST['userId'] : Auth::id();

        if ($sparePartId > 0 && $hospitalNo !== '') {
            $part = $db->query("SELECT * FROM spare_parts WHERE id = $sparePartId")->fetch();
            if ($part && $part['quantity'] >= 1) {
                // Deduct 1 from reserve
                $db->prepare("UPDATE spare_parts SET quantity = quantity - 1 WHERE id = ?")->execute([$sparePartId]);

                // Create new Asset
                $stmt = $db->prepare("
                    INSERT INTO assets 
                    (hospitalNo, serialNo, brand, modelNo, price, type, status, supplierId, roomId, userId, 
                     specs, buyingDate, warrantyTill, maintenanceTeam, categoryId)
                    VALUES (?, ?, ?, ?, ?, ?, 'In Service', ?, ?, ?, ?, date('now'), date('now', '+2 years'), ?, 2)
                ");
                $stmt->execute([
                    $hospitalNo, $serialNo, $part['name'], $part['partNo'], $part['unitCost'], 'High-Value Reserve Component',
                    $part['supplierId'], $roomId, $userId, "Commissioned from reserve stock {$part['partNo']}", $part['maintenanceTeam']
                ]);
                $assetId = $db->lastInsertId();

                // Log spare part transaction
                $db->prepare("
                    INSERT INTO spare_part_transactions (sparePartId, quantity, type, notes, createdAt, performedByUserId)
                    VALUES (?, -1, 'Commissioned as Asset', ?, datetime('now'), ?)
                ")->execute([$sparePartId, "Commissioned as independent hospital asset {$hospitalNo}", Auth::id()]);

                // Log asset history
                $db->prepare("
                    INSERT INTO asset_history (assetId, action, notes, changedBy, timestamp, newRoomId, newUserId)
                    VALUES (?, 'Commissioned from Reserve Stock', ?, ?, datetime('now'), ?, ?)
                ")->execute([$assetId, "Dual-nature component commissioned into hospital duty from reserve stock SKU {$part['partNo']}", Auth::name(), $roomId, $userId]);

                flash('success', "Component successfully converted into independent hospital asset {$hospitalNo}!");
                header('Location: ' . url('assets/details', ['id' => $assetId]));
                exit;
            } else {
                flash('error', 'Zero stock available to commission.');
            }
        }

        header('Location: ' . url('spareparts'));
        exit;
    }

    public function transactions(): void {
        $db = Database::getConnection();

        $transactions = $db->query("
            SELECT tx.*, sp.partNo, sp.name as partName, u.fullName as staffName, t.title as ticketTitle
            FROM spare_part_transactions tx
            JOIN spare_parts sp ON tx.sparePartId = sp.id
            LEFT JOIN users u ON tx.performedByUserId = u.id
            LEFT JOIN tickets t ON tx.ticketId = t.id
            ORDER BY tx.id DESC
            LIMIT 100
        ")->fetchAll();

        require __DIR__ . '/../../templates/spareparts/transactions.php';
    }
}
