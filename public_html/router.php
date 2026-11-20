<?php
declare(strict_types=1);

/**
 * SOLVIS - Local Development & Built-in Server Router
 * Usage: php -S 0.0.0.0:8000 router.php
 */

$rawUri = $_SERVER['REQUEST_URI'] ?? '/';
$uri = urldecode((string)parse_url($rawUri, PHP_URL_PATH));

// Security check: block direct access to database, config, and source files
if (preg_match('/\.(db|sqlite|sql|cs|csproj|env)$/i', $uri) || str_starts_with($uri, '/database')) {
    http_response_code(403);
    echo "403 Forbidden";
    exit;
}

// Serve static files from root or wwwroot
$directFile = __DIR__ . $uri;
if ($uri !== '/' && is_file($directFile) && !str_ends_with($uri, '.php')) {
    return false; // Let built-in server serve static file
}

$wwwrootFile = __DIR__ . '/wwwroot' . $uri;
if (is_file($wwwrootFile)) {
    $_SERVER['SCRIPT_FILENAME'] = $wwwrootFile;
    return false;
}

// Pass everything else to index.php
require __DIR__ . '/index.php';
