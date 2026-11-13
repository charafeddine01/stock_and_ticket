<?php
/**
 * SOLVIS DirectoryController
 * Hospital Staff Directory, Operating Rooms & Clinics, Departments & Suppliers
 */

require_once __DIR__ . '/../Database.php';
require_once __DIR__ . '/../Auth.php';
require_once __DIR__ . '/../Helpers.php';

class DirectoryController {
    public function index(): void {
        $db = Database::getConnection();

        $tab = $_GET['tab'] ?? 'users';
        $search = trim($_GET['search'] ?? '');

        // 1. Staff Users
        $usersSql = "
            SELECT u.*, d.name as departmentName, d.name_ar as departmentNameAr,
                   (SELECT COUNT(*) FROM assets WHERE userId = u.id) as custodyCount,
                   (SELECT COUNT(*) FROM tickets WHERE assignedToUserId = u.id AND statusId IN (1, 2, 3)) as activeTicketCount
            FROM users u
            LEFT JOIN departments d ON u.departmentId = d.id
            WHERE u.isActive = 1
        ";
        if ($search !== '' && $tab === 'users') {
            $usersSql .= " AND (u.fullName LIKE '%$search%' OR u.email LIKE '%$search%' OR u.role LIKE '%$search%')";
        }
        $usersSql .= " ORDER BY u.fullName ASC";
        $users = $db->query($usersSql)->fetchAll();

        // 2. Hospital Rooms
        $roomsSql = "
            SELECT r.*, d.name as departmentName, d.name_ar as departmentNameAr,
                   (SELECT COUNT(*) FROM assets WHERE roomId = r.id) as assetCount
            FROM rooms r
            LEFT JOIN departments d ON r.department_id = d.id
            WHERE 1=1
        ";
        if ($search !== '' && $tab === 'rooms') {
            $roomsSql .= " AND (r.name LIKE '%$search%' OR r.name_ar LIKE '%$search%' OR r.roomNo LIKE '%$search%')";
        }
        $roomsSql .= " ORDER BY r.floor ASC, r.roomNo ASC";
        $rooms = $db->query($roomsSql)->fetchAll();

        // 3. Departments
        $departmentsSql = "
            SELECT d.*, u.fullName as headUserName,
                   (SELECT COUNT(*) FROM users WHERE departmentId = d.id) as staffCount,
                   (SELECT COUNT(*) FROM rooms WHERE department_id = d.id) as roomCount
            FROM departments d
            LEFT JOIN users u ON d.headUserID = u.id
            WHERE 1=1
        ";
        if ($search !== '' && $tab === 'departments') {
            $departmentsSql .= " AND (d.name LIKE '%$search%' OR d.name_ar LIKE '%$search%')";
        }
        $departmentsSql .= " ORDER BY d.id ASC";
        $departments = $db->query($departmentsSql)->fetchAll();

        // 4. Suppliers
        $suppliersSql = "
            SELECT s.*, 
                   (SELECT COUNT(*) FROM assets WHERE supplierId = s.id) as suppliedAssetsCount,
                   (SELECT COUNT(*) FROM asset_maintenances WHERE vendorId = s.id AND status = 'Active') as activeRepairsCount
            FROM suppliers s
            WHERE 1=1
        ";
        if ($search !== '' && $tab === 'suppliers') {
            $suppliersSql .= " AND (s.name LIKE '%$search%' OR s.contactPerson LIKE '%$search%' OR s.email LIKE '%$search%')";
        }
        $suppliersSql .= " ORDER BY s.name ASC";
        $suppliers = $db->query($suppliersSql)->fetchAll();

        require __DIR__ . '/../../templates/directory/index.php';
    }
}
