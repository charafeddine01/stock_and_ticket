<?php
$pageTitle = $asset['hospitalNo'] . " - " . $asset['brand'] . " " . $asset['modelNo'];
$activeNav = 'assets';
ob_start();
?>
<div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
    <div>
        <nav aria-label="breadcrumb">
            <ol class="breadcrumb mb-1 small">
                <li class="breadcrumb-item"><a href="<?= url('assets') ?>" class="text-decoration-none"><?= __('Hospital Assets') ?></a></li>
                <li class="breadcrumb-item active" aria-current="page"><?= e($asset['hospitalNo']) ?></li>
            </ol>
        </nav>
        <h4 class="fw-bold mb-0 text-dark d-flex align-items-center gap-2">
            <span><?= e($asset['hospitalNo']) ?></span>
            <?= render_asset_status_badge($asset['status']) ?>
        </h4>
        <small class="text-muted"><?= e($asset['brand']) ?> <?= e($asset['modelNo']) ?> &bull; <?= e($asset['type']) ?></small>
    </div>
    <div class="d-flex gap-2">
        <a href="<?= url('assets/label', ['id' => $asset['id']]) ?>" target="_blank" class="btn btn-outline-dark">
            <i class="fa-solid fa-barcode me-1"></i> <?= __('Print Thermal Label') ?>
        </a>
        <a href="<?= url('assets/edit', ['id' => $asset['id']]) ?>" class="btn btn-outline-primary">
            <i class="fa-solid fa-pen me-1"></i> <?= __('Edit Asset') ?>
        </a>
    </div>
</div>

<div class="row g-4">
    <!-- Main Asset Card & Technical Specs -->
    <div class="col-lg-8">
        <div class="card shadow-sm border-0 mb-4">
            <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center">
                <h6 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-microchip text-primary me-2"></i><?= __('Asset Details') ?></h6>
                <span class="badge bg-light text-dark border"><?= e($asset['categoryName'] ?? 'General') ?></span>
            </div>
            <div class="card-body">
                <div class="row g-3">
                    <div class="col-md-4">
                        <div class="text-muted small"><?= __('Asset Tag') ?></div>
                        <div class="fw-bold fs-6 text-dark"><?= e($asset['hospitalNo']) ?></div>
                    </div>
                    <div class="col-md-4">
                        <div class="text-muted small"><?= __('Serial Number') ?></div>
                        <div class="fw-bold fs-6 text-dark"><?= e($asset['serialNo'] ?: '-') ?></div>
                    </div>
                    <div class="col-md-4">
                        <div class="text-muted small"><?= __('Equipment Type') ?></div>
                        <div class="fw-bold fs-6 text-dark"><?= e($asset['type']) ?></div>
                    </div>
                    <div class="col-md-4">
                        <div class="text-muted small"><?= __('Manufacturer / Brand') ?></div>
                        <div class="fw-semibold text-dark"><?= e($asset['brand']) ?></div>
                    </div>
                    <div class="col-md-4">
                        <div class="text-muted small"><?= __('Model Number') ?></div>
                        <div class="fw-semibold text-dark"><?= e($asset['modelNo'] ?: '-') ?></div>
                    </div>
                    <div class="col-md-4">
                        <div class="text-muted small"><?= __('Maintenance Queue') ?></div>
                        <span class="badge bg-primary-subtle text-primary border"><?= e($asset['maintenanceTeam'] ?? 'IT') ?> Team</span>
                    </div>
                    <div class="col-md-4">
                        <div class="text-muted small"><?= __('Purchase Cost') ?></div>
                        <div class="fw-bold text-success fs-6"><?= format_currency($asset['price']) ?></div>
                    </div>
                    <div class="col-md-4">
                        <div class="text-muted small"><?= __('Purchase Date') ?></div>
                        <div><?= format_date($asset['buyingDate']) ?></div>
                    </div>
                    <div class="col-md-4">
                        <div class="text-muted small"><?= __('Warranty Expiry') ?></div>
                        <div>
                            <?= format_date($asset['warrantyTill']) ?>
                            <?php if (!empty($asset['warrantyTill']) && strtotime($asset['warrantyTill']) < time()): ?>
                                <span class="badge bg-danger ms-1"><?= $isAr ? 'منتهي' : 'Expired' ?></span>
                            <?php endif; ?>
                        </div>
                    </div>
                </div>

                <?php if (!empty($asset['specs'])): ?>
                    <hr class="my-3">
                    <h6 class="fw-bold small text-muted text-uppercase mb-2">Technical Specifications &amp; Hardware Attributes</h6>
                    <div class="p-3 bg-light rounded font-monospace small text-dark border">
                        <?= nl2br(e($asset['specs'])) ?>
                    </div>
                <?php endif; ?>
            </div>
        </div>

        <!-- Custody History Timeline -->
        <div class="card shadow-sm border-0 mb-4">
            <div class="card-header bg-white py-3 border-bottom">
                <h6 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-clock-rotate-left text-primary me-2"></i><?= __('Custody History') ?></h6>
            </div>
            <div class="card-body">
                <?php if (empty($history)): ?>
                    <div class="text-muted small py-3 text-center">No custody changes recorded.</div>
                <?php else: ?>
                    <div class="timeline">
                        <?php foreach ($history as $h): ?>
                            <div class="d-flex gap-3 mb-3 pb-3 border-bottom">
                                <div class="text-primary mt-1"><i class="fa-solid fa-circle-dot"></i></div>
                                <div class="w-100">
                                    <div class="d-flex justify-content-between align-items-center">
                                        <span class="fw-bold text-dark"><?= e($h['action']) ?></span>
                                        <small class="text-muted"><?= format_date($h['timestamp'], true) ?></small>
                                    </div>
                                    <div class="small text-muted mb-1"><?= e($h['notes']) ?></div>
                                    <div class="small text-secondary"><i class="fa-regular fa-user me-1"></i> <?= e($h['changedBy']) ?></div>
                                </div>
                            </div>
                        <?php endforeach; ?>
                    </div>
                <?php endif; ?>
            </div>
        </div>
    </div>

    <!-- Sidebar: Custody, Supplier & Calibration Status -->
    <div class="col-lg-4">
        <!-- Current Custody Card -->
        <div class="card shadow-sm border-0 mb-4">
            <div class="card-header bg-white py-3 border-bottom">
                <h6 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-user-check text-success me-2"></i>Current Custody &amp; Location</h6>
            </div>
            <div class="card-body">
                <div class="mb-3">
                    <div class="text-muted small"><?= __('Assigned User') ?>:</div>
                    <div class="fw-bold text-dark"><?= e($asset['assignedUserName'] ?: 'Unassigned / Common Room') ?></div>
                    <?php if (!empty($asset['assignedUserEmail'])): ?>
                        <small class="text-muted"><?= e($asset['assignedUserEmail']) ?></small>
                    <?php endif; ?>
                </div>
                <div class="mb-3">
                    <div class="text-muted small"><?= __('Location / Room') ?>:</div>
                    <div class="fw-bold text-dark">Room <?= $asset['roomNo'] ?> - <?= e($asset['roomName']) ?></div>
                    <small class="text-muted">Floor <?= $asset['floor'] ?></small>
                </div>
                <div>
                    <div class="text-muted small">Custody Classification:</div>
                    <span class="badge bg-light text-dark border"><?= e($asset['custodyName'] ?? 'Personal') ?></span>
                </div>
            </div>
        </div>

        <!-- Calibration & PM Status Card -->
        <div class="card shadow-sm border-0 mb-4">
            <div class="card-header bg-white py-3 border-bottom">
                <h6 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-award text-warning me-2"></i>Calibration &amp; PM</h6>
            </div>
            <div class="card-body">
                <div class="mb-3">
                    <div class="text-muted small"><?= __('Next Calibration') ?>:</div>
                    <div class="fw-bold text-dark"><?= format_date($asset['nextPeriodic']) ?></div>
                    <?php if ($asset['needsPeriodic']): ?>
                        <div class="small text-muted mt-1">
                            Cycle Interval: <strong><?= $asset['periodicInterval'] ?> days</strong>
                        </div>
                    <?php endif; ?>
                </div>
                <?php if (!empty($asset['calibrationCertificate'])): ?>
                    <div class="mb-2">
                        <div class="text-muted small">Certificate:</div>
                        <span class="badge bg-success-subtle text-success border border-success"><?= e($asset['calibrationCertificate']) ?></span>
                    </div>
                <?php endif; ?>
            </div>
        </div>

        <!-- Supplier & Service Vendor -->
        <div class="card shadow-sm border-0 mb-4">
            <div class="card-header bg-white py-3 border-bottom">
                <h6 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-truck-field text-primary me-2"></i>Supplier &amp; Vendor</h6>
            </div>
            <div class="card-body">
                <div class="fw-bold text-dark"><?= e($asset['supplierName'] ?: 'Not recorded') ?></div>
                <?php if (!empty($asset['supplierPhone'])): ?>
                    <div class="small text-muted"><i class="fa-solid fa-phone me-1"></i> <?= e($asset['supplierPhone']) ?></div>
                <?php endif; ?>
                <?php if (!empty($asset['supplierEmail'])): ?>
                    <div class="small text-muted"><i class="fa-solid fa-envelope me-1"></i> <?= e($asset['supplierEmail']) ?></div>
                <?php endif; ?>
            </div>
        </div>
    </div>
</div>

<?php
$content = ob_get_clean();
require __DIR__ . '/../layout.php';
?>
