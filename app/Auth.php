<?php
/**
 * SOLVIS Authentication & Identity Switching Service
 * Supports 1-click persona simulation for customer live testing
 */

require_once __DIR__ . '/Database.php';

class Auth {
    private static ?array $currentUser = null;

    public static function init(): void {
        if (!isset($_SESSION['solvis_user_id'])) {
            // Default to Abbas Al-Amiri (Admin) on initial visit
            $_SESSION['solvis_user_id'] = 1;
        }

        $userId = (int)$_SESSION['solvis_user_id'];
        $db = Database::getConnection();

        $stmt = $db->prepare("
            SELECT u.*, d.name as departmentName, d.name_ar as departmentNameAr
            FROM users u
            LEFT JOIN departments d ON u.departmentId = d.id
            WHERE u.id = ? AND u.isActive = 1
        ");
        $stmt->execute([$userId]);
        $user = $stmt->fetch();

        if (!$user) {
            // Fallback to first active user
            $stmt = $db->query("SELECT * FROM users WHERE isActive = 1 ORDER BY id ASC LIMIT 1");
            $user = $stmt->fetch();
            if ($user) {
                $_SESSION['solvis_user_id'] = $user['id'];
            }
        }

        self::$currentUser = $user ?: [
            'id' => 1,
            'fullName' => 'Abbas Al-Amiri',
            'domainUser' => 'a.amiri',
            'role' => 'ADMIN',
            'maintenanceTeam' => 'IT',
            'email' => 'admin@solvis-health.org',
            'departmentId' => 1,
            'departmentName' => 'Information Technology',
            'departmentNameAr' => 'تقنية المعلومات'
        ];
    }

    public static function user(): array {
        if (self::$currentUser === null) {
            self::init();
        }
        return self::$currentUser;
    }

    public static function id(): int {
        return (int)(self::user()['id'] ?? 1);
    }

    public static function name(): string {
        return self::user()['fullName'] ?? 'Staff User';
    }

    public static function role(): string {
        return self::user()['role'] ?? 'ADMIN';
    }

    public static function isAdmin(): bool {
        return strtoupper(self::role()) === 'ADMIN';
    }

    public static function isMaintenance(): bool {
        return strtoupper(self::role()) === 'MAINTENANCE';
    }

    public static function isEndUser(): bool {
        return strtoupper(self::role()) === 'END_USER';
    }

    public static function getMaintenanceTeam(): string {
        return self::user()['maintenanceTeam'] ?? 'IT';
    }

    public static function canManageAssets(): bool {
        return self::isAdmin() || self::isMaintenance();
    }

    public static function canSwitchProfile(): bool {
        // In the live customer demo, persona switching is enabled so clients can experience all user roles!
        return true;
    }

    public static function switchUser(int $id): bool {
        $db = Database::getConnection();
        $stmt = $db->prepare("SELECT id FROM users WHERE id = ? AND isActive = 1");
        $stmt->execute([$id]);
        if ($stmt->fetch()) {
            $_SESSION['solvis_user_id'] = $id;
            self::$currentUser = null;
            self::init();
            return true;
        }
        return false;
    }

    public static function getAllTestUsers(): array {
        $db = Database::getConnection();
        $stmt = $db->query("
            SELECT u.*, d.name as departmentName, d.name_ar as departmentNameAr
            FROM users u
            LEFT JOIN departments d ON u.departmentId = d.id
            WHERE u.isActive = 1
            ORDER BY u.id ASC
        ");
        return $stmt->fetchAll();
    }
}
