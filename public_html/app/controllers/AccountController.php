<?php
/**
 * SOLVIS AccountController
 * User Profile & Interactive Identity / Persona Switcher for Client Demos
 */

require_once __DIR__ . '/../Database.php';
require_once __DIR__ . '/../Auth.php';
require_once __DIR__ . '/../Helpers.php';

class AccountController {
    public function profile(): void {
        $db = Database::getConnection();
        $targetId = !empty($_GET['id']) ? (int)$_GET['id'] : Auth::id();

        $stmt = $db->prepare("
            SELECT u.*, d.name as departmentName, d.name_ar as departmentNameAr
            FROM users u
            LEFT JOIN departments d ON u.departmentId = d.id
            WHERE u.id = ?
        ");
        $stmt->execute([$targetId]);
        $profileUser = $stmt->fetch();

        if (!$profileUser) {
            flash('error', 'User profile not found.');
            header('Location: ' . url());
            exit;
        }

        // Assigned Assets in custody
        $stmtAssets = $db->prepare("
            SELECT a.*, r.name as roomName, r.roomNo
            FROM assets a
            LEFT JOIN rooms r ON a.roomId = r.id
            WHERE a.userId = ?
            ORDER BY a.id DESC
        ");
        $stmtAssets->execute([$targetId]);
        $assignedAssets = $stmtAssets->fetchAll();

        // Recent Tickets opened or assigned
        $stmtTix = $db->prepare("
            SELECT t.*, ts.status as statusName
            FROM tickets t
            LEFT JOIN ticket_status ts ON t.statusId = ts.id
            WHERE t.createdByUserId = ? OR t.assignedToUserId = ?
            ORDER BY t.id DESC
            LIMIT 10
        ");
        $stmtTix->execute([$targetId, $targetId]);
        $userTickets = $stmtTix->fetchAll();

        require __DIR__ . '/../../templates/account/profile.php';
    }

    public function switchUser(): void {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            header('Location: ' . url());
            exit;
        }

        $userId = (int)($_POST['id'] ?? 0);
        $returnUrl = $_POST['returnUrl'] ?? url();

        if ($userId > 0) {
            if (Auth::switchUser($userId)) {
                $user = Auth::user();
                flash('success', "Switched persona to: {$user['fullName']} ({$user['role']} - {$user['departmentName']})");
            } else {
                flash('error', 'Unable to switch to the selected user.');
            }
        } else {
            // Revert to Admin
            Auth::switchUser(1);
            flash('success', 'Reverted persona to System Administrator (Abbas Al-Amiri).');
        }

        header('Location: ' . $returnUrl);
        exit;
    }
}
