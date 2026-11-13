<?php
$pageTitle = I18n::isArabic() ? "أصول ومعدات المستشفى" : "Stock & Hospital Asset Inventory";
$activeNav = 'assets';
ob_start();
?>
<div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
    <div>
        <h4 class="fw-bold mb-0">
            <?= __('Hospital Assets') ?>
            <span class="badge bg-primary fs-6 align-middle ms-2"><?= count($assets) ?> <?= __('Total Assets') ?></span>
        </h4>
        <small class="text-muted">
            <?= $isAr ? "متابعة شاملة لجميع الأجهزة الطبية، الخوادم، المعدات الحيوية، وحالات الضمان والمعايرة" : "Enterprise tracking for clinical equipment, IT servers, serial numbers, calibration cycles and warranty custody." ?>
        </small>
    </div>
    <div class="d-flex gap-2">
        <a href="<?= url('assets/create') ?>" class="btn btn-primary">
            <i class="fa-solid fa-plus me-1"></i> <?= __('Add New Asset') ?>
        </a>
    </div>
</div>

<!-- Filter & Search Toolbar -->
<div class="card shadow-sm mb-4 border-0">
    <div class="card-body p-3">
        <form method="get" action="<?= url('assets') ?>" class="row g-2 align-items-center">
            <div class="col-md-3">
                <div class="input-group">
                    <span class="input-group-text bg-white border-end-0 text-muted"><i class="fa-solid fa-magnifying-glass"></i></span>
                    <input type="text" name="search" value="<?= e($search) ?>" class="form-control border-start-0 ps-0" placeholder="<?= $isAr ? 'بحث (رقم العهدة، السيريال، الماركة)...' : 'Search (Tag, Serial, Brand)...' ?>">
                </div>
            </div>
            <div class="col-md-2">
                <select name="status" class="form-select" onchange="this.form.submit()">
                    <option value=""><?= __('All Statuses') ?></option>
                    <option value="In Service" <?= $status === 'In Service' ? 'selected' : '' ?>><?= __('In Service') ?></option>
                    <option value="In Stock" <?= $status === 'In Stock' ? 'selected' : '' ?>><?= __('In Stock') ?></option>
                    <option value="Under Maintenance" <?= $status === 'Under Maintenance' ? 'selected' : '' ?>><?= __('Under Maintenance') ?></option>
                    <option value="Retired" <?= $status === 'Retired' ? 'selected' : '' ?>><?= __('Retired') ?></option>
                </select>
            </div>
            <div class="col-md-2">
                <select name="type" class="form-select" onchange="this.form.submit()">
                    <option value=""><?= $isAr ? 'كافة الأنواع' : 'All Equipment Types' ?></option>
                    <?php foreach ($types as $t): ?>
                        <option value="<?= e($t) ?>" <?= $type === $t ? 'selected' : '' ?>><?= e($t) ?></option>
                    <?php endforeach; ?>
                </select>
            </div>
            <div class="col-md-2">
                <select name="roomId" class="form-select" onchange="this.form.submit()">
                    <option value=""><?= $isAr ? 'كافة المواقع' : 'All Locations' ?></option>
                    <?php foreach ($rooms as $r): ?>
                        <option value="<?= $r['id'] ?>" <?= $roomId == $r['id'] ? 'selected' : '' ?>>
                            Room <?= $r['roomNo'] ?> - <?= e($isAr && !empty($r['name_ar']) ? $r['name_ar'] : $r['name']) ?>
                        </option>
                    <?php endforeach; ?>
                </select>
            </div>
            <div class="col-md-2">
                <select name="calibration" class="form-select" onchange="this.form.submit()">
                    <option value=""><?= $isAr ? 'المعايرة والضمان (الكل)' : 'Calibration Status' ?></option>
                    <option value="required" <?= $calibration === 'required' ? 'selected' : '' ?>><?= $isAr ? 'تتطلب معايرة' : 'Requires Calibration' ?></option>
                    <option value="overdue" <?= $calibration === 'overdue' ? 'selected' : '' ?>><?= $isAr ? 'معايرة متأخرة' : 'Overdue' ?></option>
                    <option value="due30" <?= $calibration === 'due30' ? 'selected' : '' ?>><?= $isAr ? 'تستحق خلال 30 يوم' : 'Due in 30 Days' ?></option>
                    <option value="valid" <?= $calibration === 'valid' ? 'selected' : '' ?>><?= $isAr ? 'معايرة صالحة' : 'Valid' ?></option>
                </select>
            </div>
            <div class="col-md-1">
                <a href="<?= url('assets') ?>" class="btn btn-outline-secondary w-100" title="<?= __('Reset') ?>">
                    <i class="fa-solid fa-rotate-left"></i>
                </a>
            </div>
        </form>
    </div>
</div>

<!-- Assets Data Table -->
<div class="card shadow-sm border-0">
    <div class="card-body p-0">
        <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
                <thead class="table-light small">
                    <tr>
                        <th><?= __('Asset Tag') ?></th>
                        <th><?= __('Brand') ?> &amp; <?= __('Model') ?></th>
                        <th><?= __('Category') ?></th>
                        <th><?= __('Status') ?></th>
                        <th><?= __('Assigned User') ?></th>
                        <th><?= __('Location / Room') ?></th>
                        <th><?= __('Purchase Price') ?></th>
                        <th class="text-end"><?= __('Actions') ?></th>
                    </tr>
                </thead>
                <tbody>
                    <?php if (empty($assets)): ?>
                        <tr><td colspan="8" class="text-center py-5 text-muted"><?= $isAr ? 'لا توجد أصول مطابقة للبحث' : 'No assets found matching filters.' ?></td></tr>
                    <?php else: ?>
                        <?php foreach ($assets as $a): ?>
                            <tr>
                                <td>
                                    <a href="<?= url('assets/details', ['id' => $a['id']]) ?>" class="fw-bold text-decoration-none">
                                        <?= e($a['hospitalNo']) ?>
                                    </a>
                                    <div class="text-muted" style="font-size: 0.72rem;"><?= e($a['serialNo']) ?></div>
                                </td>
                                <td>
                                    <div class="fw-semibold text-dark"><?= e($a['brand']) ?> <?= e($a['modelNo']) ?></div>
                                    <div class="text-muted small" style="font-size: 0.75rem;"><?= e($a['type']) ?></div>
                                </td>
                                <td>
                                    <span class="badge bg-light text-dark border">
                                        <?= e($a['categoryName'] ?? 'General') ?>
                                    </span>
                                </td>
                                <td><?= render_asset_status_badge($a['status']) ?></td>
                                <td>
                                    <?php if (!empty($a['assignedUserName'])): ?>
                                        <div class="fw-semibold small text-dark"><i class="fa-regular fa-user me-1 text-primary"></i> <?= e($a['assignedUserName']) ?></div>
                                    <?php else: ?>
                                        <span class="text-muted small">-</span>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <span class="badge bg-light text-dark border">
                                        Room <?= $a['roomNo'] ?> (<?= e($a['roomName']) ?>)
                                    </span>
                                </td>
                                <td><span class="fw-bold text-dark"><?= format_currency($a['price']) ?></span></td>
                                <td class="text-end">
                                    <div class="btn-group btn-group-sm">
                                        <a href="<?= url('assets/details', ['id' => $a['id']]) ?>" class="btn btn-outline-primary" title="<?= __('Details') ?>">
                                            <i class="fa-solid fa-eye"></i>
                                        </a>
                                        <a href="<?= url('assets/label', ['id' => $a['id']]) ?>" target="_blank" class="btn btn-outline-secondary" title="<?= __('Print Thermal Label') ?>">
                                            <i class="fa-solid fa-barcode"></i>
                                        </a>
                                        <a href="<?= url('assets/edit', ['id' => $a['id']]) ?>" class="btn btn-outline-secondary" title="<?= __('Edit') ?>">
                                            <i class="fa-solid fa-pen"></i>
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
