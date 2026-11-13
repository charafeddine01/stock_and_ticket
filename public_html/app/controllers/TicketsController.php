<?php
/**
 * SOLVIS TicketsController
 * Multi-Team Helpdesk, Priority Routing, Diagnostics Timeline, and Printable Work Orders
 */

require_once __DIR__ . '/../Database.php';
require_once __DIR__ . '/../Auth.php';
require_once __DIR__ . '/../Helpers.php';

class TicketsController {
    public function index(): void {
        $db = Database::getConnection();
        $user = Auth::user();

        $filter = $_GET['filter'] ?? '';
        $statusId = !empty($_GET['statusId']) ? (int)$_GET['statusId'] : null;
        $priority = trim($_GET['priority'] ?? '');
        $team = trim($_GET['team'] ?? '');
        $search = trim($_GET['search'] ?? '');

        $where = ["1=1"];
        $params = [];

        // Role-based filtering: If End User, only show tickets created by them
        if (Auth::isEndUser()) {
            $where[] = "t.createdByUserId = ?";
            $params[] = Auth::id();
        }

        if ($search !== '') {
            $where[] = "(t.title LIKE ? OR t.description LIKE ? OR a.hospitalNo LIKE ? OR uCreator.fullName LIKE ?)";
            $term = "%$search%";
            $params = array_merge($params, [$term, $term, $term, $term]);
        }

        if ($statusId !== null) {
            $where[] = "t.statusId = ?";
            $params[] = $statusId;
        }

        if ($priority !== '') {
            $where[] = "t.priority = ?";
            $params[] = $priority;
        }

        if ($team !== '') {
            $where[] = "t.maintenanceTeam = ?";
            $params[] = $team;
        }

        // Quick View Tab logic
        if ($filter === 'my_assigned') {
            $where[] = "t.assignedToUserId = ?";
            $params[] = Auth::id();
        } elseif ($filter === 'unassigned') {
            $where[] = "t.assignedToUserId IS NULL";
        } elseif ($filter === 'pending_parts') {
            $where[] = "t.statusId = 3";
        } elseif ($filter === 'urgent') {
            $where[] = "t.priority = 'Urgent' AND t.statusId IN (1, 2, 3)";
        }

        $sql = "
            SELECT t.*, ts.status as statusName, ts.status_ar as statusNameAr,
                   uCreator.fullName as createdByName, uAssigned.fullName as assignedName,
                   a.hospitalNo as assetTag, a.brand as assetBrand, a.modelNo as assetModel,
                   d.name as departmentName
            FROM tickets t
            LEFT JOIN ticket_status ts ON t.statusId = ts.id
            LEFT JOIN users uCreator ON t.createdByUserId = uCreator.id
            LEFT JOIN users uAssigned ON t.assignedToUserId = uAssigned.id
            LEFT JOIN assets a ON t.assetId = a.id
            LEFT JOIN departments d ON t.departmentId = d.id
            WHERE " . implode(' AND ', $where) . "
            ORDER BY 
                CASE WHEN t.priority = 'Urgent' AND t.statusId IN (1, 2, 3) THEN 0 ELSE 1 END,
                t.id DESC
        ";

        $stmt = $db->prepare($sql);
        $stmt->execute($params);
        $tickets = $stmt->fetchAll();

        // Count queries for Quick View Tabs
        $uid = Auth::id();
        $allCount = (int)$db->query("SELECT COUNT(*) FROM tickets")->fetchColumn();
        $myAssignedCount = (int)$db->query("SELECT COUNT(*) FROM tickets WHERE assignedToUserId = $uid")->fetchColumn();
        $unassignedCount = (int)$db->query("SELECT COUNT(*) FROM tickets WHERE assignedToUserId IS NULL")->fetchColumn();
        $pendingPartsCount = (int)$db->query("SELECT COUNT(*) FROM tickets WHERE statusId = 3")->fetchColumn();
        $urgentCount = (int)$db->query("SELECT COUNT(*) FROM tickets WHERE priority = 'Urgent' AND statusId IN (1, 2, 3)")->fetchColumn();

        $statuses = $db->query("SELECT * FROM ticket_status ORDER BY id ASC")->fetchAll();
        $allTechnicians = $db->query("SELECT * FROM users WHERE role IN ('ADMIN', 'MAINTENANCE') AND isActive = 1 ORDER BY fullName ASC")->fetchAll();

        require __DIR__ . '/../../templates/tickets/index.php';
    }

    public function details(): void {
        $db = Database::getConnection();
        $id = (int)($_GET['id'] ?? 0);

        $stmt = $db->prepare("
            SELECT t.*, ts.status as statusName, ts.status_ar as statusNameAr,
                   uCreator.fullName as createdByName, uCreator.email as creatorEmail, uCreator.phone as creatorPhone,
                   uAssigned.fullName as assignedName, uAssigned.email as assignedEmail,
                   a.hospitalNo as assetTag, a.brand as assetBrand, a.modelNo as assetModel, a.type as assetType, a.status as assetStatus,
                   d.name as departmentName, r.name as roomName, r.roomNo
            FROM tickets t
            LEFT JOIN ticket_status ts ON t.statusId = ts.id
            LEFT JOIN users uCreator ON t.createdByUserId = uCreator.id
            LEFT JOIN users uAssigned ON t.assignedToUserId = uAssigned.id
            LEFT JOIN assets a ON t.assetId = a.id
            LEFT JOIN rooms r ON a.roomId = r.id
            LEFT JOIN departments d ON t.departmentId = d.id
            WHERE t.id = ?
        ");
        $stmt->execute([$id]);
        $ticket = $stmt->fetch();

        if (!$ticket) {
            flash('error', 'Ticket not found.');
            header('Location: ' . url('tickets'));
            exit;
        }

        // Messages timeline
        $stmtMsg = $db->prepare("
            SELECT m.*, u.fullName as authorName, u.role as authorRole
            FROM ticket_messages m
            LEFT JOIN users u ON m.userId = u.id
            WHERE m.ticketId = ?
            ORDER BY m.id ASC
        ");
        $stmtMsg->execute([$id]);
        $messages = $stmtMsg->fetchAll();

        // Used Spare Parts in this ticket
        $stmtParts = $db->prepare("
            SELECT tx.*, sp.partNo, sp.name as partName, sp.unitCost, u.fullName as technicianName
            FROM spare_part_transactions tx
            JOIN spare_parts sp ON tx.sparePartId = sp.id
            LEFT JOIN users u ON tx.performedByUserId = u.id
            WHERE tx.ticketId = ?
            ORDER BY tx.id DESC
        ");
        $stmtParts->execute([$id]);
        $usedParts = $stmtParts->fetchAll();

        // Available spare parts for dropdown
        $availableParts = $db->query("SELECT * FROM spare_parts WHERE quantity > 0 ORDER BY name ASC")->fetchAll();
        $allTechnicians = $db->query("SELECT * FROM users WHERE role IN ('ADMIN', 'MAINTENANCE') AND isActive = 1 ORDER BY fullName ASC")->fetchAll();
        $statuses = $db->query("SELECT * FROM ticket_status ORDER BY id ASC")->fetchAll();

        require __DIR__ . '/../../templates/tickets/details.php';
    }

    public function addComment(): void {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            header('Location: ' . url('tickets'));
            exit;
        }

        $db = Database::getConnection();
        $ticketId = (int)($_POST['ticketId'] ?? 0);
        $message = trim($_POST['message'] ?? '');

        if ($ticketId > 0 && $message !== '') {
            $stmt = $db->prepare("
                INSERT INTO ticket_messages (ticketId, userId, message, isInternal, createdAt)
                VALUES (?, ?, ?, 0, datetime('now'))
            ");
            $stmt->execute([$ticketId, Auth::id(), $message]);

            // Touch ticket updatedAt
            $db->prepare("UPDATE tickets SET updatedAt = datetime('now') WHERE id = ?")->execute([$ticketId]);

            flash('success', 'Diagnostic note added to timeline.');
        }

        header('Location: ' . url('tickets/details', ['id' => $ticketId]));
        exit;
    }

    public function updateStatus(): void {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            header('Location: ' . url('tickets'));
            exit;
        }

        $db = Database::getConnection();
        $ticketId = (int)($_POST['ticketId'] ?? 0);
        $statusId = (int)($_POST['statusId'] ?? 0);
        $assignedToUserId = !empty($_POST['assignedToUserId']) ? (int)$_POST['assignedToUserId'] : null;

        if ($ticketId > 0 && $statusId > 0) {
            $closedAt = in_array($statusId, [4, 5]) ? date('Y-m-d H:i:s') : null;

            $stmt = $db->prepare("
                UPDATE tickets SET 
                    statusId = ?, 
                    assignedToUserId = COALESCE(?, assignedToUserId), 
                    updatedAt = datetime('now'),
                    closedAt = ?
                WHERE id = ?
            ");
            $stmt->execute([$statusId, $assignedToUserId, $closedAt, $ticketId]);

            // Add system note
            $stmtNote = $db->prepare("
                INSERT INTO ticket_messages (ticketId, userId, message, isInternal, createdAt)
                VALUES (?, ?, ?, 1, datetime('now'))
            ");
            $noteMsg = "Status updated by " . Auth::name();
            $stmtNote->execute([$ticketId, Auth::id(), $noteMsg]);

            flash('success', 'Ticket status updated successfully.');
        }

        header('Location: ' . url('tickets/details', ['id' => $ticketId]));
        exit;
    }

    public function logSparePart(): void {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            header('Location: ' . url('tickets'));
            exit;
        }

        $db = Database::getConnection();
        $ticketId = (int)($_POST['ticketId'] ?? 0);
        $sparePartId = (int)($_POST['sparePartId'] ?? 0);
        $quantity = max(1, (int)($_POST['quantity'] ?? 1));
        $notes = trim($_POST['notes'] ?? 'Fitted during ticket servicing');

        if ($ticketId > 0 && $sparePartId > 0) {
            // Verify stock
            $stmtP = $db->prepare("SELECT name, quantity FROM spare_parts WHERE id = ?");
            $stmtP->execute([$sparePartId]);
            $part = $stmtP->fetch();

            if ($part && $part['quantity'] >= $quantity) {
                // Deduct stock
                $db->prepare("UPDATE spare_parts SET quantity = quantity - ? WHERE id = ?")->execute([$quantity, $sparePartId]);

                // Record transaction
                $stmtTx = $db->prepare("
                    INSERT INTO spare_part_transactions (sparePartId, ticketId, quantity, type, notes, createdAt, performedByUserId)
                    VALUES (?, ?, ?, 'Usage in Ticket', ?, datetime('now'), ?)
                ");
                $stmtTx->execute([$sparePartId, $ticketId, -$quantity, $notes, Auth::id()]);

                // Add timeline message
                $msg = sprintf("Technician consumed %d x %s from reserve stock (%s).", $quantity, $part['name'], $notes);
                $db->prepare("
                    INSERT INTO ticket_messages (ticketId, userId, message, isInternal, createdAt)
                    VALUES (?, ?, ?, 0, datetime('now'))
                ")->execute([$ticketId, Auth::id(), $msg]);

                flash('success', "Spare part {$part['name']} logged and deducted from stock.");
            } else {
                flash('error', "Insufficient reserve stock for this part.");
            }
        }

        header('Location: ' . url('tickets/details', ['id' => $ticketId]));
        exit;
    }

    public function create(): void {
        $db = Database::getConnection();

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $title = trim($_POST['title'] ?? '');
            $description = trim($_POST['description'] ?? '');
            $priority = trim($_POST['priority'] ?? 'Medium');
            $assetId = !empty($_POST['assetId']) ? (int)$_POST['assetId'] : null;
            $departmentId = !empty($_POST['departmentId']) ? (int)$_POST['departmentId'] : null;
            $maintenanceTeam = trim($_POST['maintenanceTeam'] ?? 'IT');
            $assignedToUserId = !empty($_POST['assignedToUserId']) ? (int)$_POST['assignedToUserId'] : null;

            if ($title === '') {
                flash('error', 'Ticket title is required.');
            } else {
                $stmt = $db->prepare("
                    INSERT INTO tickets 
                    (title, description, priority, statusId, createdByUserId, assignedToUserId, departmentId, assetId, maintenanceTeam, createdAt, updatedAt)
                    VALUES (?, ?, ?, 1, ?, ?, ?, ?, ?, datetime('now'), datetime('now'))
                ");
                $stmt->execute([
                    $title, $description, $priority, Auth::id(), $assignedToUserId, $departmentId, $assetId, $maintenanceTeam
                ]);
                $ticketId = $db->lastInsertId();

                // Add initial creation message
                $db->prepare("
                    INSERT INTO ticket_messages (ticketId, userId, message, isInternal, createdAt)
                    VALUES (?, ?, ?, 0, datetime('now'))
                ")->execute([$ticketId, Auth::id(), "Ticket opened by " . Auth::name() . ": " . $description]);

                flash('success', "Ticket #{$ticketId} opened successfully.");
                header('Location: ' . url('tickets/details', ['id' => $ticketId]));
                exit;
            }
        }

        $assets = $db->query("SELECT id, hospitalNo, brand, modelNo, type FROM assets ORDER BY hospitalNo ASC")->fetchAll();
        $departments = $db->query("SELECT * FROM departments ORDER BY name ASC")->fetchAll();
        $technicians = $db->query("SELECT * FROM users WHERE role IN ('ADMIN', 'MAINTENANCE') AND isActive = 1 ORDER BY fullName ASC")->fetchAll();

        require __DIR__ . '/../../templates/tickets/create.php';
    }

    public function workorder(): void {
        $db = Database::getConnection();
        $id = (int)($_GET['id'] ?? 0);

        $stmt = $db->prepare("
            SELECT t.*, ts.status as statusName,
                   uCreator.fullName as createdByName, uCreator.phone as creatorPhone,
                   uAssigned.fullName as assignedName, uAssigned.phone as assignedPhone,
                   a.hospitalNo as assetTag, a.brand as assetBrand, a.modelNo as assetModel, a.serialNo as assetSerial, a.type as assetType,
                   r.name as roomName, r.roomNo, d.name as departmentName
            FROM tickets t
            LEFT JOIN ticket_status ts ON t.statusId = ts.id
            LEFT JOIN users uCreator ON t.createdByUserId = uCreator.id
            LEFT JOIN users uAssigned ON t.assignedToUserId = uAssigned.id
            LEFT JOIN assets a ON t.assetId = a.id
            LEFT JOIN rooms r ON a.roomId = r.id
            LEFT JOIN departments d ON t.departmentId = d.id
            WHERE t.id = ?
        ");
        $stmt->execute([$id]);
        $ticket = $stmt->fetch();

        if (!$ticket) {
            die("Ticket not found.");
        }

        $stmtParts = $db->prepare("
            SELECT tx.*, sp.partNo, sp.name as partName, sp.unitCost
            FROM spare_part_transactions tx
            JOIN spare_parts sp ON tx.sparePartId = sp.id
            WHERE tx.ticketId = ?
        ");
        $stmtParts->execute([$id]);
        $usedParts = $stmtParts->fetchAll();

        require __DIR__ . '/../../templates/tickets/workorder.php';
    }

    public function exportCsv(): void {
        $db = Database::getConnection();
        $tickets = $db->query("
            SELECT t.id, t.title, t.priority, ts.status, t.maintenanceTeam, 
                   uCreator.fullName as creator, uAssigned.fullName as technician,
                   a.hospitalNo, t.createdAt, t.closedAt
            FROM tickets t
            LEFT JOIN ticket_status ts ON t.statusId = ts.id
            LEFT JOIN users uCreator ON t.createdByUserId = uCreator.id
            LEFT JOIN users uAssigned ON t.assignedToUserId = uAssigned.id
            LEFT JOIN assets a ON t.assetId = a.id
            ORDER BY t.id DESC
        ")->fetchAll();

        header('Content-Type: text/csv; charset=utf-8');
        header('Content-Disposition: attachment; filename=solvis_tickets_' . date('Y-m-d') . '.csv');

        $out = fopen('php://output', 'w');
        fputcsv($out, ['Ticket ID', 'Title', 'Priority', 'Status', 'Maintenance Team', 'Created By', 'Technician', 'Asset Tag', 'Created At', 'Closed At']);

        foreach ($tickets as $t) {
            fputcsv($out, [
                $t['id'], $t['title'], $t['priority'], $t['status'], $t['maintenanceTeam'],
                $t['creator'], $t['technician'], $t['hospitalNo'], $t['createdAt'], $t['closedAt']
            ]);
        }
        fclose($out);
        exit;
    }
}
