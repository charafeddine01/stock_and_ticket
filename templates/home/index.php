<?php
$pageTitle = I18n::isArabic() ? "لوحة التحكم والتحليلات التنفيذية" : "Executive IT & Maintenance Dashboard";
$activeNav = 'home';
ob_start();
?>
<!-- Navigation Tabs (Overview vs Analytics) -->
<ul class="nav nav-tabs nav-tabs-bordered mb-4" id="dashboardTabs" role="tablist">
    <li class="nav-item" role="presentation">
        <a class="nav-link <?= $data['tab'] !== 'analytics' ? 'active' : '' ?> fw-bold px-4 py-2" href="<?= url('', ['tab' => 'overview', 'teamFilter' => $data['teamFilter']]) ?>">
            <i class="fa-solid fa-chart-line me-2 text-primary"></i> <?= __('Executive Overview') ?>
        </a>
    </li>
    <li class="nav-item" role="presentation">
        <a class="nav-link <?= $data['tab'] === 'analytics' ? 'active' : '' ?> fw-bold px-4 py-2" href="<?= url('', ['tab' => 'analytics', 'teamFilter' => $data['teamFilter']]) ?>">
            <i class="fa-solid fa-users-gear me-2 text-info"></i> <?= __('Maintenance Team Analytics') ?>
            <?php if ($data['closedTickets'] > 0): ?>
                <span class="badge bg-success ms-1"><?= $data['closedTickets'] ?> <?= $isAr ? 'تم الحل والإغلاق' : 'Closed Resolved' ?></span>
            <?php endif; ?>
        </a>
    </li>
</ul>

<!-- Department Filter Toolbar -->
<div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-4 pb-2 border-bottom">
    <div class="d-flex align-items-center gap-2">
        <span class="small fw-bold text-muted"><i class="fa-solid fa-filter me-1 text-primary"></i> <?= __('Department Filter:') ?></span>
        <div class="btn-group btn-group-sm" role="group">
            <a href="<?= url('', ['tab' => $data['tab'], 'teamFilter' => 'ALL']) ?>" 
               class="btn <?= $data['teamFilter'] === 'ALL' ? 'btn-primary fw-bold' : 'btn-outline-secondary bg-white' ?>">
                <i class="fa-solid fa-hospital me-1"></i> <?= __('All Departments') ?>
            </a>
            <a href="<?= url('', ['tab' => $data['tab'], 'teamFilter' => 'IT']) ?>" 
               class="btn <?= $data['teamFilter'] === 'IT' ? 'btn-primary fw-bold' : 'btn-outline-secondary bg-white' ?>">
                <i class="fa-solid fa-laptop me-1"></i> <?= __('IT') ?>
            </a>
            <a href="<?= url('', ['tab' => $data['tab'], 'teamFilter' => 'BIOMEDICAL']) ?>" 
               class="btn <?= $data['teamFilter'] === 'BIOMEDICAL' ? 'btn-success fw-bold text-white' : 'btn-outline-secondary bg-white' ?>">
                <i class="fa-solid fa-heart-pulse me-1"></i> <?= __('Biomedical') ?>
            </a>
            <a href="<?= url('', ['tab' => $data['tab'], 'teamFilter' => 'GENERAL']) ?>" 
               class="btn <?= $data['teamFilter'] === 'GENERAL' ? 'btn-warning fw-bold text-dark' : 'btn-outline-secondary bg-white' ?>">
                <i class="fa-solid fa-wrench me-1"></i> <?= __('General') ?>
            </a>
        </div>
    </div>
    <?php if ($data['teamFilter'] !== 'ALL'): ?>
        <div>
            <span class="badge bg-info-subtle text-dark border">
                <?= $isAr ? "المعاينة الحالية: " . I18n::targetTeam($data['teamFilter']) : "Filtered by: " . I18n::targetTeam($data['teamFilter']) ?>
            </span>
            <a href="<?= url('', ['tab' => $data['tab'], 'teamFilter' => 'ALL']) ?>" class="btn btn-sm btn-link text-decoration-none py-0 text-muted">
                <i class="fa-solid fa-xmark me-1"></i> <?= $isAr ? "إلغاء التصفية" : "Reset" ?>
            </a>
        </div>
    <?php endif; ?>
</div>

<!-- 4 Key Executive Stat Metric Cards -->
<div class="row g-3 mb-4">
    <!-- Assets Card -->
    <div class="col-xl-3 col-md-6">
        <a href="<?= url('assets') ?>" class="text-decoration-none d-block h-100">
            <div class="card stat-card stat-card-blue shadow-sm h-100">
                <div class="d-flex align-items-center justify-content-between">
                    <div>
                        <div class="text-uppercase fw-bold text-muted small"><?= __('Total Assets') ?></div>
                        <div class="h2 fw-bold text-primary mb-1"><?= $data['totalAssets'] ?></div>
                        <div class="small text-muted">
                            <span class="text-success fw-semibold"><?= $data['inServiceAssets'] ?></span> <?= __('in service') ?> &bull; 
                            <span class="text-warning fw-semibold"><?= $data['underMaintAssets'] ?></span> <?= __('in repair') ?>
                        </div>
                    </div>
                    <div class="stat-icon bg-primary bg-opacity-10 text-primary">
                        <i class="fa-solid fa-boxes-stacked"></i>
                    </div>
                </div>
                <div class="mt-3 pt-2 border-top border-light-subtle d-flex justify-content-between small">
                    <span class="text-muted"><?= __('Total Asset Value') ?>:</span>
                    <span class="fw-bold text-dark"><?= format_currency($data['totalInventoryValue']) ?></span>
                </div>
            </div>
        </a>
    </div>

    <!-- Helpdesk Tickets Card -->
    <div class="col-xl-3 col-md-6">
        <a href="<?= url('tickets') ?>" class="text-decoration-none d-block h-100">
            <div class="card stat-card stat-card-orange shadow-sm h-100">
                <div class="d-flex align-items-center justify-content-between">
                    <div>
                        <div class="text-uppercase fw-bold text-muted small"><?= __('Open Helpdesk Tickets') ?></div>
                        <div class="h2 fw-bold text-warning mb-1"><?= $data['openTickets'] ?></div>
                        <div class="small text-muted">
                            <span class="text-danger fw-semibold"><?= $data['urgentTicketsCount'] ?></span> <?= __('urgent fire priority') ?>
                        </div>
                    </div>
                    <div class="stat-icon bg-warning bg-opacity-10 text-warning">
                        <i class="fa-solid fa-ticket"></i>
                    </div>
                </div>
                <div class="mt-3 pt-2 border-top border-light-subtle d-flex justify-content-between small">
                    <span class="text-muted"><?= __('Avg SLA Resolution') ?>:</span>
                    <span class="fw-bold text-success">&lt; 3.2 <?= $isAr ? 'ساعات' : 'hours' ?></span>
                </div>
            </div>
        </a>
    </div>

    <!-- Maintenance Dispatches Card -->
    <div class="col-xl-3 col-md-6">
        <a href="<?= url('maintenance') ?>" class="text-decoration-none d-block h-100">
            <div class="card stat-card stat-card-amber shadow-sm h-100">
                <div class="d-flex align-items-center justify-content-between">
                    <div>
                        <div class="text-uppercase fw-bold text-muted small"><?= __('Active Dispatches') ?></div>
                        <div class="h2 fw-bold text-warning mb-1"><?= $data['activeDispatches'] ?></div>
                        <div class="small text-muted">
                            <span class="text-dark fw-semibold"><?= $data['activeDispatches'] ?></span> <?= __('at outside vendors') ?>
                        </div>
                    </div>
                    <div class="stat-icon bg-warning bg-opacity-10 text-warning">
                        <i class="fa-solid fa-truck-ramp-box"></i>
                    </div>
                </div>
                <div class="mt-3 pt-2 border-top border-light-subtle d-flex justify-content-between small">
                    <span class="text-muted"><?= __('Overdue Inspections') ?>:</span>
                    <span class="badge bg-danger text-white rounded-pill px-2"><?= $data['overduePMCount'] ?></span>
                </div>
            </div>
        </a>
    </div>

    <!-- Expiring Warranties Card -->
    <div class="col-xl-3 col-md-6">
        <a href="<?= url('assets', ['calibration' => 'due30']) ?>" class="text-decoration-none d-block h-100">
            <div class="card stat-card stat-card-purple shadow-sm h-100">
                <div class="d-flex align-items-center justify-content-between">
                    <div>
                        <div class="text-uppercase fw-bold text-muted small"><?= __('Expiring Warranties') ?></div>
                        <div class="h2 fw-bold text-info mb-1"><?= $data['expiringWarrantiesCount'] ?></div>
                        <div class="small text-muted">
                            <span><?= __('within 30 days') ?></span>
                        </div>
                    </div>
                    <div class="stat-icon bg-info bg-opacity-10 text-info">
                        <i class="fa-solid fa-calendar-xmark"></i>
                    </div>
                </div>
                <div class="mt-3 pt-2 border-top border-light-subtle d-flex justify-content-between small">
                    <span class="text-muted"><?= __('Reserve Low Stock') ?>:</span>
                    <span class="fw-bold <?= $data['lowStockCount'] > 0 ? 'text-danger' : 'text-success' ?>">
                        <?= $data['lowStockCount'] ?> <?= $isAr ? 'أصناف بحاجة توريد' : 'items need restock' ?>
                    </span>
                </div>
            </div>
        </a>
    </div>
</div>

<!-- Urgent Tickets Alert & Quick Action Bar -->
<?php if ($data['urgentTicketsCount'] > 0): ?>
    <div class="alert alert-danger d-flex align-items-center justify-content-between mb-4 shadow-sm border-0">
        <div class="d-flex align-items-center gap-3">
            <i class="fa-solid fa-fire-flame-curved fa-beat-fade fs-3 text-danger"></i>
            <div>
                <strong><?= $isAr ? "تنبيه طارئ: يوجد {$data['urgentTicketsCount']} تذاكر صيانة عاجلة بحاجة استجابة فورية!" : "Urgent Alert: {$data['urgentTicketsCount']} fire-priority tickets require immediate technician dispatch!" ?></strong>
                <div class="small text-muted"><?= $isAr ? "الأجهزة الحيوية وأجنحة العناية المركزة تحظى بالأولوية القصوى في النظام." : "Life-support clinical equipment and patient monitoring queues have prioritized dispatch." ?></div>
            </div>
        </div>
        <a href="<?= url('tickets', ['filter' => 'urgent']) ?>" class="btn btn-danger btn-sm text-white fw-bold px-3">
            <?= $isAr ? "معاينة التذاكر العاجلة" : "View Urgent Queue" ?> &rarr;
        </a>
    </div>
<?php endif; ?>

<!-- Two Columns: Recent Urgent Tickets & Overdue PM Inspections -->
<div class="row g-4 mb-4">
    <!-- Urgent Tickets Table -->
    <div class="col-lg-6">
        <div class="card shadow-sm border-0 h-100">
            <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center border-bottom">
                <h6 class="fw-bold mb-0 text-dark">
                    <i class="fa-solid fa-fire text-danger me-2"></i><?= __('Recent Urgent Tickets') ?>
                </h6>
                <a href="<?= url('tickets') ?>" class="btn btn-sm btn-outline-primary"><?= $isAr ? 'عرض الكل' : 'View All' ?></a>
            </div>
            <div class="card-body p-0">
                <div class="table-responsive">
                    <table class="table table-hover align-middle mb-0">
                        <thead class="table-light small">
                            <tr>
                                <th>#</th>
                                <th><?= __('Title') ?></th>
                                <th><?= __('Asset') ?></th>
                                <th><?= __('Status') ?></th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php if (empty($data['recentUrgentTickets'])): ?>
                                <tr><td colspan="4" class="text-center py-4 text-muted small"><?= $isAr ? 'لا توجد تذاكر عاجلة مفتوحة حالياً' : 'No urgent tickets open.' ?></td></tr>
                            <?php else: ?>
                                <?php foreach ($data['recentUrgentTickets'] as $t): ?>
                                    <tr>
                                        <td><a href="<?= url('tickets/details', ['id' => $t['id']]) ?>" class="fw-bold text-decoration-none">#<?= $t['id'] ?></a></td>
                                        <td>
                                            <a href="<?= url('tickets/details', ['id' => $t['id']]) ?>" class="text-dark text-decoration-none fw-semibold">
                                                <?= e($t['title']) ?>
                                            </a>
                                            <div class="text-muted" style="font-size: 0.72rem;"><?= e($t['createdByName']) ?> &bull; <?= format_date($t['createdAt']) ?></div>
                                        </td>
                                        <td>
                                            <?php if (!empty($t['assetTag'])): ?>
                                                <span class="badge bg-light text-dark border"><?= e($t['assetTag']) ?></span>
                                            <?php else: ?>
                                                <span class="text-muted">-</span>
                                            <?php endif; ?>
                                        </td>
                                        <td><?= render_ticket_status_badge($t['statusId']) ?></td>
                                    </tr>
                                <?php endforeach; ?>
                            <?php endif; ?>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>

    <!-- Overdue Preventive Maintenance -->
    <div class="col-lg-6">
        <div class="card shadow-sm border-0 h-100">
            <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center border-bottom">
                <h6 class="fw-bold mb-0 text-dark">
                    <i class="fa-solid fa-triangle-exclamation text-warning me-2"></i><?= __('Overdue Preventive Inspections') ?>
                </h6>
                <a href="<?= url('maintenance') ?>" class="btn btn-sm btn-outline-warning text-dark"><?= $isAr ? 'جدول الصيانة' : 'PM Schedule' ?></a>
            </div>
            <div class="card-body p-0">
                <div class="table-responsive">
                    <table class="table table-hover align-middle mb-0">
                        <thead class="table-light small">
                            <tr>
                                <th><?= __('Asset Tag') ?></th>
                                <th><?= __('Equipment') ?></th>
                                <th><?= __('Location / Room') ?></th>
                                <th><?= __('Due Date') ?></th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php if (empty($data['overdueAssets'])): ?>
                                <tr><td colspan="4" class="text-center py-4 text-muted small"><?= $isAr ? 'كافة المعدات الطبية والأنظمة تخضع لمعايرة صالحة ومحدثة' : 'All equipment inspections are up to date.' ?></td></tr>
                            <?php else: ?>
                                <?php foreach ($data['overdueAssets'] as $oa): ?>
                                    <tr>
                                        <td>
                                            <a href="<?= url('assets/details', ['id' => $oa['id']]) ?>" class="fw-bold text-decoration-none">
                                                <?= e($oa['hospitalNo']) ?>
                                            </a>
                                        </td>
                                        <td>
                                            <div class="fw-semibold text-dark"><?= e($oa['brand']) ?> <?= e($oa['modelNo']) ?></div>
                                            <div class="text-muted" style="font-size: 0.72rem;"><?= e($oa['type']) ?></div>
                                        </td>
                                        <td><span class="badge bg-light text-dark border"><?= e($oa['roomName'] ?? 'Room ' . $oa['roomNo']) ?></span></td>
                                        <td><span class="badge bg-danger"><?= format_date($oa['nextPeriodic']) ?></span></td>
                                    </tr>
                                <?php endforeach; ?>
                            <?php endif; ?>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- Recent Asset Custody & System Activity -->
<div class="card shadow-sm border-0 mb-4">
    <div class="card-header bg-white py-3 border-bottom">
        <h6 class="fw-bold mb-0 text-dark">
            <i class="fa-solid fa-clock-rotate-left text-primary me-2"></i><?= __('Recent Asset Activity') ?>
        </h6>
    </div>
    <div class="card-body p-0">
        <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
                <thead class="table-light small">
                    <tr>
                        <th><?= __('Timestamp') ?></th>
                        <th><?= __('Asset') ?></th>
                        <th><?= __('Action') ?></th>
                        <th><?= __('Notes') ?></th>
                        <th><?= __('Performed By') ?></th>
                    </tr>
                </thead>
                <tbody>
                    <?php if (empty($data['recentActivity'])): ?>
                        <tr><td colspan="5" class="text-center py-4 text-muted small">No recent activity.</td></tr>
                    <?php else: ?>
                        <?php foreach ($data['recentActivity'] as $act): ?>
                            <tr>
                                <td class="text-muted small" style="font-size: 0.75rem;"><?= format_date($act['timestamp'], true) ?></td>
                                <td>
                                    <a href="<?= url('assets/details', ['id' => $act['assetId']]) ?>" class="fw-bold text-decoration-none">
                                        <?= e($act['hospitalNo']) ?>
                                    </a>
                                    <span class="text-muted small ms-1">(<?= e($act['brand']) ?>)</span>
                                </td>
                                <td><span class="badge bg-primary-subtle text-primary border"><?= e($act['action']) ?></span></td>
                                <td class="small text-muted"><?= e($act['notes']) ?></td>
                                <td><span class="fw-semibold small text-dark"><?= e($act['changedBy']) ?></span></td>
                            </tr>
                        <?php endforeach; ?>
                    <?php endif; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<?php
$content = ob_get_clean();
require __DIR__ . '/../layout.php';
?>
