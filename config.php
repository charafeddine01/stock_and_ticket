<?php
/**
 * SOLVIS - Hospital Operations & Asset Intelligence Platform
 * Shared Hosting Configuration File (hosting.com / Enhance / cPanel)
 */

if (session_status() === PHP_SESSION_NONE) {
    // Ensure session save path is writable
    $sessionSavePath = session_save_path();
    if (empty($sessionSavePath) || !is_writable($sessionSavePath)) {
        $tempDir = sys_get_temp_dir();
        if (is_writable($tempDir)) {
            session_save_path($tempDir);
        }
    }

    // 30-day session lifetime
    ini_set('session.gc_maxlifetime', 2592000);
    session_set_cookie_params([
        'lifetime' => 2592000,
        'path' => '/',
        'secure' => isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] === 'on',
        'httponly' => true,
        'samesite' => 'Lax'
    ]);
    @session_start();
}

// Application Constants
define('APP_NAME', 'SOLVIS');
define('APP_VERSION', '2.0.0-LiveDemo');
define('APP_ENV', 'production'); // 'development' or 'production'

// Error reporting: Log errors silently in production
if (APP_ENV === 'development') {
    ini_set('display_errors', 1);
    error_reporting(E_ALL);
} else {
    ini_set('display_errors', 0);
    error_reporting(E_ALL & ~E_DEPRECATED & ~E_STRICT);
}

// Database Configuration
// Connected to your live hosting database
define('DB_DRIVER', 'mysql');

// MySQL / MariaDB Live Credentials
define('DB_MYSQL_HOST', 'localhost');
define('DB_MYSQL_PORT', 3306);
define('DB_MYSQL_NAME', 'solvis_c1_solvis');
define('DB_MYSQL_USER', 'solvis_c1_solvis');
define('DB_MYSQL_PASS', 'esCmnzDbV@@sZsL84');
define('DB_MYSQL_CHARSET', 'utf8mb4');

// SQLite Fallback Path (Zero-config backup if MySQL is unreachable)
define('DB_SQLITE_PATH', __DIR__ . '/database/solvis.db');

// Base URL Auto-Detection Helper
function get_base_url(): string {
    $scriptName = $_SERVER['SCRIPT_NAME'] ?? '/index.php';
    $baseDir = rtrim(str_replace('\\', '/', dirname($scriptName)), '/');
    return $baseDir;
}

define('BASE_URL', get_base_url());
