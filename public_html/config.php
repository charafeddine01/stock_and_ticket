<?php
/**
 * SOLVIS - Hospital Operations & Asset Intelligence Platform
 * Shared Hosting Configuration File (hosting.com / cPanel / Apache / LiteSpeed)
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

// Error reporting: Log errors silently in production, show in development
if (APP_ENV === 'development') {
    ini_set('display_errors', 1);
    error_reporting(E_ALL);
} else {
    ini_set('display_errors', 0);
    error_reporting(E_ALL & ~E_DEPRECATED & ~E_STRICT);
}

// Database Configuration
// Options: 'sqlite' (Zero-config, recommended for live testing) or 'mysql'
define('DB_DRIVER', 'sqlite');

// SQLite Configuration (Database is stored safely outside public web root or protected by .htaccess)
define('DB_SQLITE_PATH', __DIR__ . '/database/solvis.db');

// MySQL / MariaDB Configuration (Optional alternative for cPanel MySQL)
define('DB_MYSQL_HOST', 'localhost');
define('DB_MYSQL_PORT', 3306);
define('DB_MYSQL_NAME', 'solvis_live_demo');
define('DB_MYSQL_USER', 'root');
define('DB_MYSQL_PASS', '');
define('DB_MYSQL_CHARSET', 'utf8mb4');

// Base URL Auto-Detection Helper
function get_base_url(): string {
    $scheme = (isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] === 'on') ? 'https' : 'http';
    $host = $_SERVER['HTTP_HOST'] ?? 'localhost';
    
    // Determine the base script path relative to document root
    $scriptName = $_SERVER['SCRIPT_NAME'] ?? '/index.php';
    $baseDir = rtrim(str_replace('\\', '/', dirname($scriptName)), '/');
    
    return $baseDir;
}

define('BASE_URL', get_base_url());
