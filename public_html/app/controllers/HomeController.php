<?php
/**
 * SOLVIS HomeController
 * Executive KPI Dashboard & Maintenance Team Analytics
 */

require_once __DIR__ . '/../Database.php';
require_once __DIR__ . '/../Auth.php';
require_once __DIR__ . '/../Helpers.php';

class HomeController {
    public function index(): void {
        $db = Database::getConnection();
        $user = Auth::user();
        
        $tab = $_GET['tab'] ?? 'overview';
        $teamFilter = $_GET['teamFilter'] ?? 'ALL';

        // Base where clause for team filtering
        $teamCondition = '';
        $teamParams = [];
        if ($teamFilter !== 'ALL') {
            $teamCondition = " AND maintenanceTeam = ?";
            $teamParams = [$teamFilter];
        }

        // 1. Asset Metrics
        $totalAssets = (int)$db->query("SELECT COUNT(*) FROM assets WHERE 1=1" . ($teamFilter !== 'ALL' ? " AND maintenanceTeam = '$teamFilter'" : ""))->fetchColumn();
        $inServiceAssets = (int)$db->query("SELECT COUNT(*) FROM assets WHERE status = 'In Service'" . ($teamFilter !== 'ALL' ? " AND maintenanceTeam = '$teamFilter'" : ""))->fetchColumn();
        $underMaintAssets = (int)$db->query("SELECT COUNT(*) FROM assets WHERE status = 'Under Maintenance'" . ($teamFilter !== 'ALL' ? " AND maintenanceTeam = '$teamFilter'" : ""))->fetchColumn();
        $totalInventoryValue = (float)$db->query("SELECT SUM(price) FROM assets WHERE 1=1" . ($teamFilter !== 'ALL' ? " AND maintenanceTeam = '$teamFilter'" : ""))->fetchColumn();

        // 2. Ticket Metrics
        $openTickets = (int)$db->query("SELECT COUNT(*) FROM tickets WHERE statusId IN (1, 2, 3)" . ($teamFilter !== 'ALL' ? " AND maintenanceTeam = '$teamFilter'" : ""))->fetchColumn();
        $urgentTicketsCount = (int)$db->query("SELECT COUNT(*) FROM tickets WHERE priority = 'Urgent' AND statusId IN (1, 2, 3)" . ($teamFilter !== 'ALL' ? " AND maintenanceTeam = '$teamFilter'" : ""))->fetchColumn();
        $closedTickets = (int)$db->query("SELECT COUNT(*) FROM tickets WHERE statusId IN (4, 5)" . ($teamFilter !== 'ALL' ? " AND maintenanceTeam = '$teamFilter'" : ""))->fetchColumn();

        // 3. Maintenance Metrics
        $activeDispatches = (int)$db->query("SELECT COUNT(*) FROM asset_maintenances WHERE status = 'Active'")->fetchColumn();
        
        $today = date('Y-m-d');
        $in30Days = date('Y-m-d', strtotime('+30 days'));

        $overduePMCount = (int)$db->query("
            SELECT COUNT(*) FROM assets 
            WHERE needsPeriodic = 1 AND nextPeriodic IS NOT NULL AND nextPeriodic < '$today'
        ")->fetchColumn();

        $expiringWarrantiesCount = (int)$db->query("
            SELECT COUNT(*) FROM assets 
            WHERE warrantyTill IS NOT NULL AND warrantyTill >= '$today' AND warrantyTill <= '$in30Days'
        ")->fetchColumn();

        // 4. Recent Urgent Tickets List
        $recentUrgentTickets = $db->query("
            SELECT t.*, u.fullName as createdByName, a.hospitalNo as assetTag, a.brand as assetBrand
            FROM tickets t
            LEFT JOIN users u ON t.createdByUserId = u.id
            LEFT JOIN assets a ON t.assetId = a.id
            WHERE t.priority = 'Urgent' AND t.statusId IN (1, 2, 3)
            ORDER BY t.createdAt DESC
            LIMIT 5
        ")->fetchAll();

        // 5. Overdue Assets List
        $overdueAssets = $db->query("
            SELECT a.*, r.name as roomName, r.roomNo, u.fullName as assignedUserName
            FROM assets a
            LEFT JOIN rooms r ON a.roomId = r.id
            LEFT JOIN users u ON a.userId = u.id
            WHERE a.needsPeriodic = 1 AND a.nextPeriodic IS NOT NULL AND a.nextPeriodic < '$today'
            ORDER BY a.nextPeriodic ASC
            LIMIT 5
        ")->fetchAll();

        // 6. Recent Asset Activity / History
        $recentActivity = $db->query("
            SELECT h.*, a.hospitalNo, a.brand
            FROM asset_history h
            JOIN assets a ON h.assetId = a.id
            ORDER BY h.id DESC
            LIMIT 6
        ")->fetchAll();

        // 7. Spare parts alerts
        $lowStockCount = (int)$db->query("SELECT COUNT(*) FROM spare_parts WHERE quantity <= minThreshold")->fetchColumn();

        // Pass to template
        $data = [
            'tab' => $tab,
            'teamFilter' => $teamFilter,
            'totalAssets' => $totalAssets,
            'inServiceAssets' => $inServiceAssets,
            'underMaintAssets' => $underMaintAssets,
            'totalInventoryValue' => $totalInventoryValue,
            'openTickets' => $openTickets,
            'urgentTicketsCount' => $urgentTicketsCount,
            'closedTickets' => $closedTickets,
            'activeDispatches' => $activeDispatches,
            'overduePMCount' => $overduePMCount,
            'expiringWarrantiesCount' => $expiringWarrantiesCount,
            'recentUrgentTickets' => $recentUrgentTickets,
            'overdueAssets' => $overdueAssets,
            'recentActivity' => $recentActivity,
            'lowStockCount' => $lowStockCount,
        ];

        require __DIR__ . '/../../templates/home/index.php';
    }
}
