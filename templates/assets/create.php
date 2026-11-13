<?php
$pageTitle = I18n::isArabic() ? "إضافة أصل جديد" : "Add New Hospital Asset";
$activeNav = 'assets';
ob_start();
?>
<div class="card shadow-sm border-0 max-w-4xl mx-auto">
    <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center">
        <h5 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-plus-circle text-primary me-2"></i><?= __('Add New Asset') ?></h5>
        <a href="<?= url('assets') ?>" class="btn btn-sm btn-outline-secondary"><?= __('Cancel') ?></a>
    </div>
    <div class="card-body p-4">
        <form method="post" action="<?= url('assets/create') ?>">
            <div class="row g-3">
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Asset Tag') ?> <span class="text-danger">*</span></label>
                    <input type="text" name="hospitalNo" class="form-control" placeholder="e.g. HOSP-BIO-105" required>
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Serial Number') ?></label>
                    <input type="text" name="serialNo" class="form-control" placeholder="e.g. SN-PHIL-88192">
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Category') ?></label>
                    <select name="categoryId" class="form-select">
                        <?php foreach ($categories as $c): ?>
                            <option value="<?= $c['id'] ?>"><?= e($c['name']) ?></option>
                        <?php endforeach; ?>
                    </select>
                </div>

                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Brand') ?> <span class="text-danger">*</span></label>
                    <input type="text" name="brand" class="form-control" placeholder="e.g. Philips, Dell, GE" required>
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Model') ?></label>
                    <input type="text" name="modelNo" class="form-control" placeholder="e.g. MX750, Optima 660">
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Type') ?> <span class="text-danger">*</span></label>
                    <input type="text" name="type" class="form-control" placeholder="e.g. Patient Monitor, Server" required>
                </div>

                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Location / Room') ?></label>
                    <select name="roomId" class="form-select">
                        <?php foreach ($rooms as $r): ?>
                            <option value="<?= $r['id'] ?>">Room <?= $r['roomNo'] ?> - <?= e($r['name']) ?></option>
                        <?php endforeach; ?>
                    </select>
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Assigned User') ?></label>
                    <select name="userId" class="form-select">
                        <option value="">-- Unassigned / Common Room --</option>
                        <?php foreach ($users as $u): ?>
                            <option value="<?= $u['id'] ?>"><?= e($u['fullName']) ?> (<?= e($u['role']) ?>)</option>
                        <?php endforeach; ?>
                    </select>
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Supplier') ?></label>
                    <select name="supplierId" class="form-select">
                        <option value="">-- Select Supplier --</option>
                        <?php foreach ($suppliers as $s): ?>
                            <option value="<?= $s['id'] ?>"><?= e($s['name']) ?></option>
                        <?php endforeach; ?>
                    </select>
                </div>

                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Purchase Price') ?></label>
                    <div class="input-group">
                        <span class="input-group-text">$</span>
                        <input type="number" step="0.01" name="price" class="form-control" value="0.00">
                    </div>
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Purchase Date') ?></label>
                    <input type="date" name="buyingDate" class="form-control" value="<?= date('Y-m-d') ?>">
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Warranty Expiry') ?></label>
                    <input type="date" name="warrantyTill" class="form-control">
                </div>

                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Status') ?></label>
                    <select name="status" class="form-select">
                        <option value="In Service"><?= __('In Service') ?></option>
                        <option value="In Stock"><?= __('In Stock') ?></option>
                        <option value="Under Maintenance"><?= __('Under Maintenance') ?></option>
                    </select>
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted">Maintenance Team Queue</label>
                    <select name="maintenanceTeam" class="form-select">
                        <option value="IT">IT Infrastructure</option>
                        <option value="BIOMEDICAL">Biomedical Engineering</option>
                        <option value="GENERAL">General Maintenance</option>
                    </select>
                </div>
                <div class="col-md-4 d-flex align-items-center">
                    <div class="form-check mt-3">
                        <input class="form-check-input" type="checkbox" name="needsPeriodic" id="needsPeriodic" value="1">
                        <label class="form-check-label fw-semibold" for="needsPeriodic">
                            Requires Recurring Calibration / PM
                        </label>
                    </div>
                </div>

                <div class="col-12">
                    <label class="form-label fw-bold small text-muted">Technical Specifications / Notes</label>
                    <textarea name="specs" class="form-control" rows="3" placeholder="CPU, RAM, Sensors, Calibration notes..."></textarea>
                </div>

                <div class="col-12 text-end pt-3 border-top">
                    <button type="submit" class="btn btn-primary px-4 fw-bold">
                        <i class="fa-solid fa-check me-1"></i> <?= __('Save') ?>
                    </button>
                </div>
            </div>
        </form>
    </div>
</div>

<?php
$content = ob_get_clean();
require __DIR__ . '/../layout.php';
?>
