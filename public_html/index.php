<?php
/**
 * SOLVIS - Hospital Operations & Asset Intelligence Platform
 * Front Controller & Routing Engine (Compatible with Shared Hosting / hosting.com)
 */

require_once __DIR__ . '/config.php';
require_once __DIR__ . '/app/I18n.php';
require_once __DIR__ . '/app/Database.php';
require_once __DIR__ . '/app/Auth.php';
require_once __DIR__ . '/app/Helpers.php';

// Initialize core components
I18n::init();
Database::getConnection();
Auth::init();

// Determine requested route
$route = '';
if (isset($_GET['r'])) {
    $route = trim($_GET['r'], '/');
} else {
    $requestUri = $_SERVER['REQUEST_URI'] ?? '/';
    $base = rtrim(BASE_URL, '/');
    
    // Strip query string
    if (($pos = strpos($requestUri, '?')) !== false) {
        $requestUri = substr($requestUri, 0, $pos);
    }
    
    // Strip base directory
    if ($base !== '' && strpos($requestUri, $base) === 0) {
        $requestUri = substr($requestUri, strlen($base));
    }
    
    $route = trim($requestUri, '/');
}

// Routing Table
$routes = [
    // Home & KPI Dashboard
    '' => ['HomeController', 'index'],
    'home' => ['HomeController', 'index'],
    'home/index' => ['HomeController', 'index'],

    // Hospital Equipment & Assets
    'assets' => ['AssetsController', 'index'],
    'assets/index' => ['AssetsController', 'index'],
    'assets/details' => ['AssetsController', 'details'],
    'assets/create' => ['AssetsController', 'create'],
    'assets/edit' => ['AssetsController', 'edit'],
    'assets/label' => ['AssetsController', 'label'],

    // Service & Support Tickets
    'tickets' => ['TicketsController', 'index'],
    'tickets/index' => ['TicketsController', 'index'],
    'tickets/details' => ['TicketsController', 'details'],
    'tickets/create' => ['TicketsController', 'create'],
    'tickets/addComment' => ['TicketsController', 'addComment'],
    'tickets/updateStatus' => ['TicketsController', 'updateStatus'],
    'tickets/logSparePart' => ['TicketsController', 'logSparePart'],
    'tickets/workorder' => ['TicketsController', 'workorder'],
    'tickets/exportCsv' => ['TicketsController', 'exportCsv'],

    // Scheduled & Preventive Maintenance
    'maintenance' => ['MaintenanceController', 'index'],
    'maintenance/index' => ['MaintenanceController', 'index'],
    'maintenance/sendToSupplier' => ['MaintenanceController', 'sendToSupplier'],
    'maintenance/returnFromSupplier' => ['MaintenanceController', 'returnFromSupplier'],
    'maintenance/logInspection' => ['MaintenanceController', 'logInspection'],

    // Reserve Stock & Spare Parts
    'spareparts' => ['SparePartsController', 'index'],
    'spareparts/index' => ['SparePartsController', 'index'],
    'spareparts/create' => ['SparePartsController', 'create'],
    'spareparts/restock' => ['SparePartsController', 'restock'],
    'spareparts/commissionAsAsset' => ['SparePartsController', 'commissionAsAsset'],
    'spareparts/transactions' => ['SparePartsController', 'transactions'],

    // Hospital Directory
    'directory' => ['DirectoryController', 'index'],
    'directory/index' => ['DirectoryController', 'index'],

    // User Profile & Identity Switcher
    'account/profile' => ['AccountController', 'profile'],
    'account/switchUser' => ['AccountController', 'switchUser'],

    // Language & Notifications
    'language/setLanguage' => ['LanguageController', 'setLanguage'],
    'notifications/markAllAsRead' => ['NotificationsController', 'markAllAsRead'],
];

if (isset($routes[$route])) {
    [$controllerName, $actionName] = $routes[$route];
    require_once __DIR__ . '/app/controllers/' . $controllerName . '.php';
    $controller = new $controllerName();
    $controller->$actionName();
} else {
    // Fallback: If static file in wwwroot, let webserver handle or serve
    if (strpos($route, 'wwwroot/') === 0 && file_exists(__DIR__ . '/' . $route)) {
        return false;
    }
    
    // Redirect unknown paths to home
    require_once __DIR__ . '/app/controllers/HomeController.php';
    $controller = new HomeController();
    $controller->index();
}
