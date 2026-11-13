<?php
$pageTitle = I18n::isArabic() ? "الصيانة الوقائية والجدولة" : "Scheduled & Preventive Maintenance";
$activeNav = 'maintenance';
ob_start();
?>
<div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
    <div>
        <h4 class="fw-bold mb-0"><?= __('Scheduled & Preventive Maintenance') ?></h4>
        <small class="text-muted">
            <?= $isAr ? "متابعة الأجهزة المرسلة للصيانة الخارجية، وتتبع حركة العهد، وجدولة الفحوصات الوقائية الدورية" : "Monitor equipment sent to suppliers, track custody transfers, and record recurring preventive inspections." ?>
        </small>
    </div>
    <div class="d-flex gap-2">
        <button type="button" class="btn btn-warning text-dark fw-bold" data-bs-toggle="modal" data-bs-target="#sendToVendorModal">
            <i class="fa-solid fa-truck-ramp-box me-1"></i> <?= __('Send to Maintenance') ?>
        </button>
    </div>
</div>

<!-- 4 Overview Metric Cards -->
<div class="row g-3 mb-4">
    <div class="col-md-3">
        <div class="card stat-card stat-card-amber shadow-sm p-3">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="text-muted small text-uppercase fw-bold"><?= __('At Supplier (Maint.)') ?></div>
                    <div class="h3 fw-bold text-warning mb-0"><?= count($activeWorkOrders) ?></div>
                </div>
                <div class="stat-icon bg-warning bg-opacity-10 text-warning"><i class="fa-solid fa-truck-ramp-box"></i></div>
            </div>
        </div>
    </div>
    <div class="col-md-3">
        <div class="card stat-card stat-card-rose shadow-sm p-3">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="text-muted small text-uppercase fw-bold"><?= __('Overdue Inspections') ?></div>
                    <div class="h3 fw-bold text-danger mb-0"><?= count($overdueAssets) ?></div>
                </div>
                <div class="stat-icon bg-danger bg-opacity-10 text-danger"><i class="fa-solid fa-triangle-exclamation"></i></div>
            </div>
        </div>
    </div>
    <div class="col-md-3">
        <div class="card stat-card stat-card-orange shadow-sm p-3">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="text-muted small text-uppercase fw-bold"><?= __('Expiring in 30 Days') ?></div>
                    <div class="h3 fw-bold text-warning-emphasis mb-0"><?= count($dueWithin30DaysAssets) ?></div>
                </div>
                <div class="stat-icon bg-warning bg-opacity-10 text-warning"><i class="fa-solid fa-clock"></i></div>
            </div>
        </div>
    </div>
    <div class="col-md-3">
        <div class="card stat-card stat-card-emerald shadow-sm p-3">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="text-muted small text-uppercase fw-bold"><?= __('Valid / Calibrated') ?></div>
                    <div class="h3 fw-bold text-success mb-0"><?= count($validCalibratedAssets) ?></div>
                </div>
                <div class="stat-icon bg-success bg-opacity-10 text-success"><i class="fa-solid fa-check-double"></i></div>
            </div>
        </div>
    </div>
</div>

<!-- Active External Vendor Dispatches -->
<div class="card shadow-sm border-0 mb-4">
    <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center">
        <h6 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-truck text-warning me-2"></i>Active External Vendor Dispatches (At Factory Service)</h6>
        <span class="badge bg-warning text-dark"><?= count($activeWorkOrders) ?> Active</span>
    </div>
    <div class="card-body p-0">
        <?php if (empty($activeWorkOrders)): ?>
            <div class="text-center py-4 text-muted small">No equipment currently dispatched to external vendors.</div>
        <?php else: ?>
            <div class="table-responsive">
                <table class="table table-hover align-middle mb-0">
                    <thead class="table-light small">
                        <tr>
                            <th>Asset Tag</th>
                            <th>Equipment</th>
                            <th>Vendor / Supplier</th>
                            <th>Dispatched Date</th>
                            <th>Reason / Maintenance Type</th>
                            <th>Est. Cost</th>
                            <th class="text-end"><?= __('Actions') ?></th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php foreach ($activeWorkOrders as $wo): ?>
                            <tr>
                                <td>
                                    <a href="<?= url('assets/details', ['id' => $wo['assetId']]) ?>" class="fw-bold text-decoration-none">
                                        <?= e($wo['hospitalNo']) ?>
                                    </a>
                                </td>
                                <td>
                                    <div class="fw-semibold text-dark"><?= e($wo['brand']) ?> <?= e($wo['modelNo']) ?></div>
                                    <div class="text-muted small">S/N: <?= e($wo['serialNo']) ?></div>
                                </td>
                                <td>
                                    <div class="fw-semibold text-dark"><?= e($wo['vendorName']) ?></div>
                                    <div class="text-muted small"><?= e($wo['vendorPhone']) ?></div>
                                </td>
                                <td><span class="badge bg-light text-dark border"><?= format_date($wo['dispatchedDate']) ?></span></td>
                                <td class="small text-muted" style="max-width: 300px;"><?= e($wo['description']) ?></td>
                                <td><span class="fw-bold text-dark"><?= format_currency($wo['cost']) ?></span></td>
                                <td class="text-end">
                                    <form method="post" action="<?= url('maintenance/returnFromSupplier') ?>" class="d-inline">
                                        <input type="hidden" name="maintenanceId" value="<?= $wo['id'] ?>">
                                        <button type="submit" class="btn btn-sm btn-success fw-bold">
                                            <i class="fa-solid fa-arrow-turn-down me-1"></i> <?= __('Return to Hospital') ?>
                                        </button>
                                    </form>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    </tbody>
                </table>
            </div>
        <?php endif; ?>
    </div>
</div>

<!-- Overdue Preventive Maintenance Inspections -->
<div class="card shadow-sm border-0 mb-4">
    <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center">
        <h6 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-triangle-exclamation text-danger me-2"></i>Overdue Preventive Inspections</h6>
        <span class="badge bg-danger"><?= count($overdueAssets) ?> Overdue</span>
    </div>
    <div class="card-body p-0">
        <?php if (empty($overdueAssets)): ?>
            <div class="text-center py-4 text-muted small">All clinical equipment inspections are currently valid.</div>
        <?php else: ?>
            <div class="table-responsive">
                <table class="table table-hover align-middle mb-0">
                    <thead class="table-light small">
                        <tr>
                            <th>Asset Tag</th>
                            <th>Equipment</th>
                            <th>Location</th>
                            <th>Assigned Custody</th>
                            <th>Scheduled Date</th>
                            <th class="text-end"><?= __('Actions') ?></th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php foreach ($overdueAssets as $oa): ?>
                            <tr>
                                <td>
                                    <a href="<?= url('assets/details', ['id' => $oa['id']]) ?>" class="fw-bold text-decoration-none">
                                        <?= e($oa['hospitalNo']) ?>
                                    </a>
                                </td>
                                <td>
                                    <div class="fw-semibold text-dark"><?= e($oa['brand']) ?> <?= e($oa['modelNo']) ?></div>
                                    <div class="text-muted small"><?= e($oa['type']) ?></div>
                                </td>
                                <td><span class="badge bg-light text-dark border">Room <?= $oa['roomNo'] ?> (<?= e($oa['roomName']) ?>)</span></td>
                                <td><?= e($oa['assignedUserName'] ?: 'General Custody') ?></td>
                                <td><span class="badge bg-danger"><?= format_date($oa['nextPeriodic']) ?></span></td>
                                <td class="text-end">
                                    <form method="post" action="<?= url('maintenance/logInspection') ?>" class="d-inline">
                                        <input type="hidden" name="assetId" value="<?= $oa['id'] ?>">
                                        <input type="hidden" name="notes" value="Overdue clinical safety inspection completed and certified">
                                        <button type="submit" class="btn btn-sm btn-outline-success fw-bold">
                                            <i class="fa-solid fa-check me-1"></i> Certify Inspection
                                        </button>
                                    </form>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    </tbody>
                </table>
            </div>
        <?php endif; ?>
    </div>
</div>

<!-- Modal: Send to External Vendor -->
<div class="modal fade" id="sendToVendorModal" tabindex="-1" aria-labelledby="sendToVendorModalLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content border-0 shadow-lg">
            <form method="post" action="<?= url('maintenance/sendToSupplier') ?>">
                <div class="modal-header">
                    <h5 class="modal-title fw-bold" id="sendToVendorModalLabel"><?= __('Dispatch to External Vendor') ?></h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted">Select Equipment <span class="text-danger">*</span></label>
                        <select name="assetId" class="form-select" required>
                            <option value="">-- Choose asset to dispatch --</option>
                            <?php foreach ($allAssets as $a): ?>
                                <option value="<?= $a['id'] ?>"><?= e($a['hospitalNo']) ?> &bull; <?= e($a['brand']) ?> <?= e($a['modelNo']) ?></option>
                            <?php endforeach; ?>
                        </select>
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted">Authorized Service Vendor <span class="text-danger">*</span></label>
                        <select name="vendorId" class="form-select" required>
                            <?php foreach ($suppliers as $s): ?>
                                <option value="<?= $s['id'] ?>"><?= e($s['name']) ?></option>
                            <?php endforeach; ?>
                        </select>
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted">Estimated Repair Cost ($)</label>
                        <input type="number" step="0.01" name="cost" class="form-control" value="0.00">
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted">Work Order / Fault Details <span class="text-danger">*</span></label>
                        <textarea name="description" class="form-control" rows="3" placeholder="Describe component fault, required calibration, or warranty repair..." required></textarea>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-light" data-bs-dismiss="modal"><?= __('Cancel') ?></button>
                    <button type="submit" class="btn btn-warning text-dark fw-bold">
                        <i class="fa-solid fa-truck-ramp-box me-1"></i> Confirm Dispatch
                    </button>
                </div>
            </form>
        </div>
    </div>
</div>

<?php
$content = ob_get_clean();
require __DIR__ . '/../layout.php';
?>
