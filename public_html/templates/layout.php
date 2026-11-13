<?php
/**
 * SOLVIS Master Layout Template
 * High-definition Hospital Operations UI with Autohide Sidebar, RTL Support & Live Persona Switcher
 */

$isAr = I18n::isArabic();
$dir = I18n::getDirection();
$lang = I18n::getLang();
$user = Auth::user();
$allTestUsers = Auth::getAllTestUsers();

// Notifications count
$db = Database::getConnection();
$notifCount = (int)$db->query("SELECT COUNT(*) FROM notifications WHERE isRead = 0 AND (userID = {$user['id']} OR userID = 1)")->fetchColumn();
$recentNotifs = $db->query("SELECT * FROM notifications WHERE isRead = 0 AND (userID = {$user['id']} OR userID = 1) ORDER BY id DESC LIMIT 5")->fetchAll();

// Live Counters for sidebar badges
$openTicketsCount = (int)$db->query("SELECT COUNT(*) FROM tickets WHERE statusId IN (1, 2, 3)")->fetchColumn();
$maintCount = (int)$db->query("SELECT COUNT(*) FROM asset_maintenances WHERE status = 'Active'")->fetchColumn();
$lowStockCount = (int)$db->query("SELECT COUNT(*) FROM spare_parts WHERE quantity <= minThreshold")->fetchColumn();
?>
<!DOCTYPE html>
<html lang="<?= e($lang) ?>" dir="<?= e($dir) ?>">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title><?= e($pageTitle ?? 'Hospital Operations & Asset Intelligence') ?> - SOLVIS</title>
    
    <link rel="icon" type="image/png" href="<?= asset_url('wwwroot/images/solvis-icon-transparent.png') ?>" />
    <link rel="shortcut icon" href="<?= asset_url('wwwroot/favicon.ico') ?>" />
    
    <!-- Google Fonts: Inter & Cairo -->
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Cairo:wght@400;500;600;700;800&family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    
    <!-- Bootstrap 5 (RTL when Arabic, LTR when English) & FontAwesome 6 -->
    <?php if ($isAr): ?>
        <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.rtl.min.css" rel="stylesheet">
    <?php else: ?>
        <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
    <?php endif; ?>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.2/css/all.min.css">
    
    <!-- SOLVIS Site Styles, Animations & Responsive Autohide Sidebar -->
    <link rel="stylesheet" href="<?= asset_url('wwwroot/css/site.css') ?>" />
    
    <!-- Instant Theme Preloader -->
    <script>
        (function() {
            var savedTheme = localStorage.getItem('stockportal_theme') || 'light';
            if (savedTheme === 'clarity' || savedTheme === 'navy') savedTheme = 'light';
            document.documentElement.setAttribute('data-theme', savedTheme);
        })();
    </script>
</head>
<body>
    <!-- Top Progress Loading Bar -->
    <div id="pageLoadingBar" class="page-loading-bar"></div>

    <!-- Edge Peek Trigger Zone for Desktop Autohide Sidebar -->
    <div id="sidebarAutohideTrigger" class="sidebar-autohide-trigger" title="<?= __('Auto-hide') ?>"></div>

    <!-- Mobile Drawer Backdrop -->
    <div id="sidebarBackdrop" class="sidebar-backdrop"></div>

    <!-- Sidebar -->
    <aside class="sidebar">
        <div class="sidebar-brand position-relative p-0 overflow-hidden">
            <div class="position-absolute top-0 end-0 p-2 d-flex align-items-center gap-1" style="z-index: 10;">
                <span class="autohide-indicator" style="font-size: 0.6rem; padding: 2px 6px;"><?= __('Auto-hide') ?></span>
                <button id="sidebarPinBtn" type="button" class="btn btn-sm btn-link text-white-50 p-1 d-none d-lg-inline-flex" title="<?= __('Pin Sidebar') ?>" style="line-height: 1; opacity: 0.75;">
                    <i class="fa-solid <?= $isAr ? 'fa-angles-right' : 'fa-angles-left' ?>"></i>
                </button>
                <button id="sidebarCloseBtn" type="button" class="btn btn-sm btn-link text-white-50 p-1 d-lg-none" title="<?= __('Close Sidebar') ?>" style="line-height: 1;">
                    <i class="fa-solid fa-xmark fs-5"></i>
                </button>
            </div>
            <a href="<?= url() ?>" class="d-flex align-items-center justify-content-center text-decoration-none w-100 pt-2 pb-2" title="SOLVIS">
                <img src="<?= asset_url('wwwroot/images/solvis-logo-transparent.png') ?>" alt="SOLVIS Logo" style="height: 52px; width: auto; object-fit: contain;" />
            </a>
        </div>

        <nav class="sidebar-nav">
            <!-- Operations Section -->
            <div class="nav-section-title"><?= __('Operations') ?></div>
            
            <a href="<?= url() ?>" class="nav-link <?= empty($activeNav) || $activeNav === 'home' ? 'active' : '' ?>">
                <i class="fa-solid fa-gauge-high"></i>
                <span><?= __('Dashboard') ?></span>
            </a>

            <a href="<?= url('assets') ?>" class="nav-link <?= ($activeNav ?? '') === 'assets' ? 'active' : '' ?>">
                <i class="fa-solid fa-boxes-stacked"></i>
                <span><?= __('Hospital Assets') ?></span>
            </a>

            <a href="<?= url('tickets') ?>" class="nav-link <?= ($activeNav ?? '') === 'tickets' ? 'active' : '' ?>">
                <i class="fa-solid fa-ticket"></i>
                <span><?= __('Tickets & Helpdesk') ?></span>
                <?php if ($openTicketsCount > 0): ?>
                    <span class="badge bg-danger rounded-pill ms-auto"><?= $openTicketsCount ?></span>
                <?php endif; ?>
            </a>

            <a href="<?= url('maintenance') ?>" class="nav-link <?= ($activeNav ?? '') === 'maintenance' ? 'active' : '' ?>">
                <i class="fa-solid fa-calendar-check"></i>
                <span><?= __('Scheduled & Preventive Maintenance') ?></span>
                <?php if ($maintCount > 0): ?>
                    <span class="badge bg-warning text-dark rounded-pill ms-auto"><?= $maintCount ?></span>
                <?php endif; ?>
            </a>

            <a href="<?= url('spareparts') ?>" class="nav-link <?= ($activeNav ?? '') === 'spareparts' ? 'active' : '' ?>">
                <i class="fa-solid fa-layer-group"></i>
                <span><?= __('Reserve Stock & Spare Parts') ?></span>
                <?php if ($lowStockCount > 0): ?>
                    <span class="badge bg-warning text-dark rounded-pill ms-auto" title="Low Stock"><i class="fa-solid fa-triangle-exclamation"></i></span>
                <?php endif; ?>
            </a>

            <!-- Directory Section -->
            <div class="nav-section-title"><?= __('Hospital Directory') ?></div>

            <a href="<?= url('directory', ['tab' => 'users']) ?>" class="nav-link <?= ($activeNav ?? '') === 'directory-users' ? 'active' : '' ?>">
                <i class="fa-solid fa-user-doctor"></i>
                <span><?= __('Staff Directory') ?></span>
            </a>

            <a href="<?= url('directory', ['tab' => 'rooms']) ?>" class="nav-link <?= ($activeNav ?? '') === 'directory-rooms' ? 'active' : '' ?>">
                <i class="fa-solid fa-door-open"></i>
                <span><?= __('Hospital Rooms') ?></span>
            </a>

            <a href="<?= url('directory', ['tab' => 'departments']) ?>" class="nav-link <?= ($activeNav ?? '') === 'directory-departments' ? 'active' : '' ?>">
                <i class="fa-solid fa-hospital"></i>
                <span><?= __('Departments') ?></span>
            </a>

            <a href="<?= url('directory', ['tab' => 'suppliers']) ?>" class="nav-link <?= ($activeNav ?? '') === 'directory-suppliers' ? 'active' : '' ?>">
                <i class="fa-solid fa-truck-field"></i>
                <span><?= __('Suppliers') ?></span>
            </a>

            <!-- Workspace Section -->
            <div class="nav-section-title"><?= __('My Workspace') ?></div>

            <a href="<?= url('account/profile') ?>" class="nav-link <?= ($activeNav ?? '') === 'profile' ? 'active' : '' ?>">
                <i class="fa-solid fa-id-badge"></i>
                <span><?= __('My Profile') ?></span>
            </a>

            <a href="<?= asset_url('maintenance_flowchart.html') ?>" target="_blank" class="nav-link">
                <i class="fa-solid fa-diagram-project text-info"></i>
                <span><?= $isAr ? 'مخطط سير العمليات' : 'Workflow Architecture' ?></span>
                <i class="fa-solid fa-arrow-up-right-from-square small text-muted ms-auto"></i>
            </a>
        </nav>

        <!-- Sidebar Footer Status -->
        <div class="sidebar-footer p-3 border-top border-white border-opacity-10 text-white-50 small">
            <div class="d-flex align-items-center justify-content-between mb-1">
                <span class="text-white fw-bold"><i class="fa-solid fa-circle text-success me-1" style="font-size: 0.6rem;"></i> <?= __('Live Production Online') ?></span>
                <span class="badge bg-primary bg-opacity-25 text-white" style="font-size: 0.65rem;">PHP 8.2+</span>
            </div>
            <div style="font-size: 0.7rem; opacity: 0.75;">SOLVIS Enterprise Suite</div>
        </div>
    </aside>

    <!-- Main Content Container -->
    <div class="main-content">
        <!-- Top Navbar -->
        <header class="top-navbar d-flex align-items-center justify-content-between px-3 py-2 border-bottom shadow-xs">
            <div class="d-flex align-items-center gap-2">
                <button id="sidebarToggleBtn" class="btn btn-sm btn-light border-0" type="button" title="Toggle Navigation">
                    <i class="fa-solid fa-bars fs-5 text-secondary"></i>
                </button>
                <div class="d-none d-sm-block">
                    <h5 class="fw-bold mb-0 text-dark" style="font-size: 1.05rem;">
                        <?= e($pageTitle ?? 'Hospital Operations & Asset Intelligence') ?>
                    </h5>
                    <small class="text-muted" style="font-size: 0.75rem;">SOLVIS Healthcare Asset & Facility Management</small>
                </div>
            </div>

            <!-- Top Actions: Persona Switcher, Notifications, Language, Theme, User Dropdown -->
            <div class="d-flex align-items-center gap-2">
                <!-- Live Client Testing Simulator Pill -->
                <span class="badge bg-success-subtle text-success border border-success d-none d-md-inline-flex align-items-center gap-1 py-1 px-2 font-monospace" style="font-size: 0.72rem;">
                    <i class="fa-solid fa-cloud-check"></i>
                    <span>SHARED HOSTING LIVE DEMO</span>
                </span>

                <!-- Persona / Identity Switcher for Client Testing -->
                <div class="dropdown">
                    <button class="btn btn-sm btn-outline-primary dropdown-toggle d-flex align-items-center gap-1 py-1" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="<?= __('Testing Profile Switcher') ?>">
                        <i class="fa-solid fa-user-gear text-primary"></i>
                        <span class="d-none d-lg-inline fw-semibold"><?= e($user['fullName']) ?></span>
                        <span class="badge bg-primary text-white" style="font-size: 0.65rem;"><?= e($user['role']) ?></span>
                    </button>
                    <ul class="dropdown-menu dropdown-menu-end shadow-lg py-2 border-0" style="min-width: 280px;">
                        <li class="dropdown-header text-uppercase fw-bold text-muted small" style="font-size: 0.7rem;">
                            <i class="fa-solid fa-mask me-1"></i> <?= __('Testing Profile Switcher') ?>
                        </li>
                        <?php foreach ($allTestUsers as $u): ?>
                            <li>
                                <form method="post" action="<?= url('account/switchUser') ?>" class="m-0">
                                    <input type="hidden" name="id" value="<?= $u['id'] ?>">
                                    <input type="hidden" name="returnUrl" value="<?= e($_SERVER['REQUEST_URI'] ?? url()) ?>">
                                    <button type="submit" class="dropdown-item d-flex align-items-center justify-content-between py-2 <?= $u['id'] == $user['id'] ? 'bg-primary-subtle fw-bold' : '' ?>">
                                        <div>
                                            <div class="text-dark"><?= e($u['fullName']) ?></div>
                                            <div class="text-muted small" style="font-size: 0.72rem;">
                                                <?= e($u['role']) ?> &bull; <?= e($isAr && !empty($u['departmentNameAr']) ? $u['departmentNameAr'] : $u['departmentName']) ?>
                                            </div>
                                        </div>
                                        <?php if ($u['id'] == $user['id']): ?>
                                            <i class="fa-solid fa-check text-primary"></i>
                                        <?php endif; ?>
                                    </button>
                                </form>
                            </li>
                        <?php endforeach; ?>
                        <li><hr class="dropdown-divider"></li>
                        <li class="px-3 py-1 text-muted small" style="font-size: 0.72rem;">
                            <i class="fa-solid fa-info-circle me-1"></i>
                            <?= $isAr ? 'يمكنك تبديل الهوية لاختبار صلاحيات الفنيين والأطباء والإدارة' : 'Switch personas to simulate Admin, Technician, and Doctor RBAC permissions live.' ?>
                        </li>
                    </ul>
                </div>

                <!-- Notifications Dropdown -->
                <div class="dropdown">
                    <button class="btn btn-sm btn-light border position-relative p-2" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="<?= __('Notifications') ?>">
                        <i class="fa-regular fa-bell fs-6 text-secondary"></i>
                        <?php if ($notifCount > 0): ?>
                            <span class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger" style="font-size: 0.65rem;">
                                <?= $notifCount ?>
                            </span>
                        <?php endif; ?>
                    </button>
                    <ul class="dropdown-menu dropdown-menu-end shadow-lg p-2 border-0" style="min-width: 320px;">
                        <li class="d-flex align-items-center justify-content-between pb-2 mb-2 border-bottom">
                            <span class="fw-bold small"><?= __('Notifications') ?></span>
                            <?php if ($notifCount > 0): ?>
                                <a href="<?= url('notifications/markAllAsRead') ?>" class="btn btn-link btn-sm p-0 text-decoration-none small text-primary">
                                    <?= $isAr ? 'تحديد الكل كمقروء' : 'Mark all read' ?>
                                </a>
                            <?php endif; ?>
                        </li>
                        <?php if (empty($recentNotifs)): ?>
                            <li class="text-center py-3 text-muted small"><?= $isAr ? 'لا توجد إشعارات جديدة' : 'No new notifications' ?></li>
                        <?php else: ?>
                            <?php foreach ($recentNotifs as $n): ?>
                                <li class="mb-2 p-2 rounded bg-light-subtle">
                                    <div class="fw-bold small text-dark"><?= e($n['title']) ?></div>
                                    <div class="text-muted small" style="font-size: 0.75rem;"><?= e($n['message']) ?></div>
                                    <div class="text-muted" style="font-size: 0.65rem;"><?= format_date($n['createdAt'], true) ?></div>
                                </li>
                            <?php endforeach; ?>
                        <?php endif; ?>
                    </ul>
                </div>

                <!-- Language Toggle (English / العربية) -->
                <a href="<?= url('language/setLanguage', ['culture' => $isAr ? 'en' : 'ar', 'returnUrl' => $_SERVER['REQUEST_URI'] ?? url()]) ?>" 
                   class="btn btn-sm btn-light border d-flex align-items-center gap-1 px-2 py-1" 
                   title="<?= $isAr ? 'Switch to English' : 'التحويل إلى العربية' ?>">
                    <i class="fa-solid fa-globe text-primary"></i>
                    <span class="fw-bold small"><?= $isAr ? 'English' : 'العربية' ?></span>
                </a>

                <!-- Theme Toggle Button -->
                <div class="dropdown">
                    <button class="btn btn-sm btn-light border p-2" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="Theme Selector">
                        <i class="fa-solid fa-circle-half-stroke text-secondary"></i>
                    </button>
                    <ul class="dropdown-menu dropdown-menu-end shadow border-0 py-1">
                        <li>
                            <button class="dropdown-item d-flex align-items-center gap-2 small" onclick="setAppTheme('light')">
                                <i class="fa-solid fa-sun text-warning"></i> <?= __('Light') ?>
                            </button>
                        </li>
                        <li>
                            <button class="dropdown-item d-flex align-items-center gap-2 small" onclick="setAppTheme('dark')">
                                <i class="fa-solid fa-moon text-info"></i> <?= __('Dark') ?>
                            </button>
                        </li>
                        <li>
                            <button class="dropdown-item d-flex align-items-center gap-2 small" onclick="setAppTheme('mint')">
                                <i class="fa-solid fa-leaf text-success"></i> <?= __('Mint') ?>
                            </button>
                        </li>
                    </ul>
                </div>
            </div>
        </header>

        <!-- Main Page View Content -->
        <main class="page-body p-3 p-md-4">
            <!-- Flash Message Alerts -->
            <?php if ($successMsg = get_flash('success')): ?>
                <div class="alert alert-success alert-dismissible fade show d-flex align-items-center gap-2 shadow-sm border-0 mb-4" role="alert">
                    <i class="fa-solid fa-circle-check fs-5"></i>
                    <div><?= e($successMsg) ?></div>
                    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                </div>
            <?php endif; ?>

            <?php if ($errorMsg = get_flash('error')): ?>
                <div class="alert alert-danger alert-dismissible fade show d-flex align-items-center gap-2 shadow-sm border-0 mb-4" role="alert">
                    <i class="fa-solid fa-circle-exclamation fs-5"></i>
                    <div><?= e($errorMsg) ?></div>
                    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                </div>
            <?php endif; ?>

            <!-- Render Template Body -->
            <?= $content ?? '' ?>
        </main>

        <!-- Footer -->
        <footer class="footer py-3 px-4 border-top text-muted small d-flex flex-wrap align-items-center justify-content-between mt-auto">
            <div>
                &copy; <?= date('Y') ?> <strong>SOLVIS</strong> — Hospital Operations, Asset Inventory & Service Helpdesk Platform.
            </div>
            <div class="d-flex align-items-center gap-3">
                <span class="text-success"><i class="fa-solid fa-shield-check me-1"></i> Shared Hosting Deployment Ready</span>
                <span>Baalbeck, Lebanon</span>
            </div>
        </footer>
    </div>

    <!-- Bootstrap 5 Bundle JS -->
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>

    <!-- SOLVIS Site JS -->
    <script src="<?= asset_url('wwwroot/js/site.js') ?>"></script>

    <!-- Theme Switcher Script -->
    <script>
        function setAppTheme(theme) {
            document.documentElement.setAttribute('data-theme', theme);
            localStorage.setItem('stockportal_theme', theme);
        }
    </script>
</body>
</html>
