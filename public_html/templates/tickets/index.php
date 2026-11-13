<?php
$pageTitle = I18n::isArabic() ? "تذاكر الدعم الفني والصيانة" : "Helpdesk & Service Tickets";
$activeNav = 'tickets';
ob_start();
?>
<div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
    <div>
        <h4 class="fw-bold mb-0">
            <?= __('Helpdesk & Support Tickets') ?>
            <span class="badge bg-primary fs-6 align-middle ms-2"><?= count($tickets) ?> <?= __('All Tickets') ?></span>
        </h4>
        <small class="text-muted">
            <?= $isAr ? "متابعة وإدارة بلاغات الأعطال، تكليف الفنيين، وجدولة الإصلاحات الميدانية" : "Track service tickets, assign engineering technicians, and record diagnostic work orders." ?>
        </small>
    </div>
    <div class="d-flex gap-2">
        <a href="<?= url('tickets/exportCsv') ?>" class="btn btn-outline-success">
            <i class="fa-solid fa-file-csv me-1"></i> <?= __('Export CSV') ?>
        </a>
        <a href="<?= url('tickets/create') ?>" class="btn btn-primary">
            <i class="fa-solid fa-plus me-1"></i> <?= __('Open New Ticket') ?>
        </a>
    </div>
</div>

<!-- Quick Assignment View Tabs & Filter Toolbar -->
<div class="card shadow-sm border-0 mb-4">
    <div class="card-body p-3">
        <!-- Quick View Tabs -->
        <div class="d-flex flex-wrap gap-2 mb-3 pb-3 border-bottom">
            <a href="<?= url('tickets') ?>" class="btn btn-sm <?= empty($filter) && $statusId === null ? 'btn-primary' : 'btn-light border' ?>">
                <i class="fa-solid fa-inbox me-1"></i> <?= __('All Tickets') ?>
                <span class="badge bg-secondary ms-1 rounded-pill"><?= $allCount ?></span>
            </a>
            <a href="<?= url('tickets', ['filter' => 'my_assigned']) ?>" class="btn btn-sm <?= $filter === 'my_assigned' ? 'btn-primary' : 'btn-light border' ?>">
                <i class="fa-solid fa-user-check me-1"></i> <?= __('My Assigned Tickets') ?>
                <?php if ($myAssignedCount > 0): ?>
                    <span class="badge bg-primary ms-1 rounded-pill"><?= $myAssignedCount ?></span>
                <?php endif; ?>
            </a>
            <a href="<?= url('tickets', ['filter' => 'unassigned']) ?>" class="btn btn-sm <?= $filter === 'unassigned' ? 'btn-warning text-dark fw-bold' : 'btn-light border' ?>">
                <i class="fa-solid fa-user-clock me-1"></i> <?= __('Unassigned') ?>
                <?php if ($unassignedCount > 0): ?>
                    <span class="badge bg-danger ms-1 rounded-pill"><?= $unassignedCount ?></span>
                <?php endif; ?>
            </a>
            <a href="<?= url('tickets', ['filter' => 'pending_parts']) ?>" class="btn btn-sm <?= $filter === 'pending_parts' ? 'btn-primary' : 'btn-light border' ?>">
                <i class="fa-solid fa-gear me-1"></i> <?= __('Pending Parts') ?>
                <?php if ($pendingPartsCount > 0): ?>
                    <span class="badge bg-warning text-dark ms-1 rounded-pill"><?= $pendingPartsCount ?></span>
                <?php endif; ?>
            </a>
            <a href="<?= url('tickets', ['filter' => 'urgent']) ?>" class="btn btn-sm <?= $filter === 'urgent' ? 'btn-danger text-white fw-bold' : 'btn-light border text-danger' ?>">
                <i class="fa-solid fa-fire me-1"></i> <?= __('Urgent') ?>
                <?php if ($urgentCount > 0): ?>
                    <span class="badge bg-danger ms-1 rounded-pill"><?= $urgentCount ?></span>
                <?php endif; ?>
            </a>
        </div>

        <!-- Filter Form -->
        <form method="get" action="<?= url('tickets') ?>" class="row g-2 align-items-center">
            <input type="hidden" name="filter" value="<?= e($filter) ?>">
            <div class="col-md-4">
                <div class="input-group">
                    <span class="input-group-text bg-white border-end-0 text-muted"><i class="fa-solid fa-magnifying-glass"></i></span>
                    <input type="text" name="search" value="<?= e($search) ?>" class="form-control border-start-0 ps-0" placeholder="<?= $isAr ? 'بحث (العنوان، الجهاز، الموظف)...' : 'Search (Title, Asset, Reporter)...' ?>">
                </div>
            </div>
            <div class="col-md-2">
                <select name="statusId" class="form-select" onchange="this.form.submit()">
                    <option value=""><?= __('All Statuses') ?></option>
                    <?php foreach ($statuses as $s): ?>
                        <option value="<?= $s['id'] ?>" <?= $statusId == $s['id'] ? 'selected' : '' ?>>
                            <?= e($isAr && !empty($s['status_ar']) ? $s['status_ar'] : $s['status']) ?>
                        </option>
                    <?php endforeach; ?>
                </select>
            </div>
            <div class="col-md-2">
                <select name="priority" class="form-select" onchange="this.form.submit()">
                    <option value=""><?= $isAr ? 'كافة الأولويات' : 'All Priorities' ?></option>
                    <option value="Urgent" <?= $priority === 'Urgent' ? 'selected' : '' ?>><?= __('Urgent') ?></option>
                    <option value="High" <?= $priority === 'High' ? 'selected' : '' ?>><?= __('High') ?></option>
                    <option value="Medium" <?= $priority === 'Medium' ? 'selected' : '' ?>><?= __('Medium') ?></option>
                    <option value="Low" <?= $priority === 'Low' ? 'selected' : '' ?>><?= __('Low') ?></option>
                </select>
            </div>
            <div class="col-md-2">
                <select name="team" class="form-select" onchange="this.form.submit()">
                    <option value=""><?= $isAr ? 'كافة الفرق الهندسية' : 'All Teams' ?></option>
                    <option value="IT" <?= $team === 'IT' ? 'selected' : '' ?>>IT Team</option>
                    <option value="BIOMEDICAL" <?= $team === 'BIOMEDICAL' ? 'selected' : '' ?>>Biomedical</option>
                    <option value="GENERAL" <?= $team === 'GENERAL' ? 'selected' : '' ?>>General Maint.</option>
                </select>
            </div>
            <div class="col-md-2">
                <a href="<?= url('tickets') ?>" class="btn btn-outline-secondary w-100"><?= __('Reset') ?></a>
            </div>
        </form>
    </div>
</div>

<!-- Tickets Table -->
<div class="card shadow-sm border-0">
    <div class="card-body p-0">
        <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
                <thead class="table-light small">
                    <tr>
                        <th style="width: 70px;">#</th>
                        <th><?= __('Title') ?> &amp; Description</th>
                        <th><?= __('Priority') ?></th>
                        <th><?= __('Status') ?></th>
                        <th><?= __('Assigned Technician') ?></th>
                        <th><?= __('Asset') ?></th>
                        <th><?= __('Created Date') ?></th>
                        <th class="text-end"><?= __('Actions') ?></th>
                    </tr>
                </thead>
                <tbody>
                    <?php if (empty($tickets)): ?>
                        <tr><td colspan="8" class="text-center py-5 text-muted"><?= $isAr ? 'لا توجد تذاكر تطابق هذا الفلتر' : 'No service tickets found.' ?></td></tr>
                    <?php else: ?>
                        <?php foreach ($tickets as $t): ?>
                            <tr>
                                <td>
                                    <a href="<?= url('tickets/details', ['id' => $t['id']]) ?>" class="fw-bold text-decoration-none">
                                        #<?= $t['id'] ?>
                                    </a>
                                </td>
                                <td>
                                    <a href="<?= url('tickets/details', ['id' => $t['id']]) ?>" class="text-dark fw-bold text-decoration-none">
                                        <?= e($t['title']) ?>
                                    </a>
                                    <div class="text-muted small text-truncate" style="max-width: 380px;">
                                        <?= e($t['description']) ?>
                                    </div>
                                    <div class="text-muted" style="font-size: 0.72rem;">
                                        <i class="fa-regular fa-user me-1"></i> <?= e($t['createdByName']) ?> &bull; <?= e($t['departmentName'] ?? 'General') ?>
                                    </div>
                                </td>
                                <td><?= render_ticket_priority_badge($t['priority']) ?></td>
                                <td><?= render_ticket_status_badge($t['statusId']) ?></td>
                                <td>
                                    <?php if (!empty($t['assignedName'])): ?>
                                        <div class="fw-semibold small text-dark"><i class="fa-solid fa-wrench me-1 text-primary"></i> <?= e($t['assignedName']) ?></div>
                                        <span class="badge bg-light text-dark border" style="font-size: 0.65rem;"><?= e($t['maintenanceTeam']) ?></span>
                                    <?php else: ?>
                                        <span class="badge bg-warning-subtle text-warning-emphasis border border-warning small">
                                            <i class="fa-solid fa-user-xmark me-1"></i> <?= __('Unassigned') ?>
                                        </span>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <?php if (!empty($t['assetTag'])): ?>
                                        <a href="<?= url('assets/details', ['id' => $t['assetId']]) ?>" class="badge bg-light text-primary border text-decoration-none">
                                            <?= e($t['assetTag']) ?>
                                        </a>
                                        <div class="text-muted" style="font-size: 0.7rem;"><?= e($t['assetBrand']) ?></div>
                                    <?php else: ?>
                                        <span class="text-muted small">-</span>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <div class="text-muted small"><?= format_date($t['createdAt']) ?></div>
                                </td>
                                <td class="text-end">
                                    <div class="btn-group btn-group-sm">
                                        <a href="<?= url('tickets/details', ['id' => $t['id']]) ?>" class="btn btn-outline-primary" title="<?= __('Details') ?>">
                                            <i class="fa-solid fa-eye"></i>
                                        </a>
                                        <a href="<?= url('tickets/workorder', ['id' => $t['id']]) ?>" target="_blank" class="btn btn-outline-secondary" title="<?= __('Print Work Order') ?>">
                                            <i class="fa-solid fa-print"></i>
                                        </a>
                                    </div>
                                </td>
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
