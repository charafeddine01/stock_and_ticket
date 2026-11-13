<?php
$pageTitle = "Edit " . $asset['hospitalNo'];
$activeNav = 'assets';
ob_start();
?>
<div class="card shadow-sm border-0 max-w-4xl mx-auto">
    <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center">
        <h5 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-pen text-primary me-2"></i><?= __('Edit Asset') ?>: <?= e($asset['hospitalNo']) ?></h5>
        <a href="<?= url('assets/details', ['id' => $asset['id']]) ?>" class="btn btn-sm btn-outline-secondary"><?= __('Cancel') ?></a>
    </div>
    <div class="card-body p-4">
        <form method="post" action="<?= url('assets/edit', ['id' => $asset['id']]) ?>">
            <div class="row g-3">
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Asset Tag') ?></label>
                    <input type="text" class="form-control bg-light" value="<?= e($asset['hospitalNo']) ?>" readonly>
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Serial Number') ?></label>
                    <input type="text" name="serialNo" class="form-control" value="<?= e($asset['serialNo']) ?>">
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Category') ?></label>
                    <select name="categoryId" class="form-select">
                        <?php foreach ($categories as $c): ?>
                            <option value="<?= $c['id'] ?>" <?= $asset['categoryId'] == $c['id'] ? 'selected' : '' ?>><?= e($c['name']) ?></option>
                        <?php endforeach; ?>
                    </select>
                </div>

                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Brand') ?> <span class="text-danger">*</span></label>
                    <input type="text" name="brand" class="form-control" value="<?= e($asset['brand']) ?>" required>
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Model') ?></label>
                    <input type="text" name="modelNo" class="form-control" value="<?= e($asset['modelNo']) ?>">
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Type') ?> <span class="text-danger">*</span></label>
                    <input type="text" name="type" class="form-control" value="<?= e($asset['type']) ?>" required>
                </div>

                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Location / Room') ?></label>
                    <select name="roomId" class="form-select">
                        <?php foreach ($rooms as $r): ?>
                            <option value="<?= $r['id'] ?>" <?= $asset['roomId'] == $r['id'] ? 'selected' : '' ?>>Room <?= $r['roomNo'] ?> - <?= e($r['name']) ?></option>
                        <?php endforeach; ?>
                    </select>
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Assigned User') ?></label>
                    <select name="userId" class="form-select">
                        <option value="">-- Unassigned / Common Room --</option>
                        <?php foreach ($users as $u): ?>
                            <option value="<?= $u['id'] ?>" <?= $asset['userId'] == $u['id'] ? 'selected' : '' ?>><?= e($u['fullName']) ?></option>
                        <?php endforeach; ?>
                    </select>
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Supplier') ?></label>
                    <select name="supplierId" class="form-select">
                        <option value="">-- Select Supplier --</option>
                        <?php foreach ($suppliers as $s): ?>
                            <option value="<?= $s['id'] ?>" <?= $asset['supplierId'] == $s['id'] ? 'selected' : '' ?>><?= e($s['name']) ?></option>
                        <?php endforeach; ?>
                    </select>
                </div>

                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Purchase Price') ?></label>
                    <div class="input-group">
                        <span class="input-group-text">$</span>
                        <input type="number" step="0.01" name="price" class="form-control" value="<?= e($asset['price']) ?>">
                    </div>
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Purchase Date') ?></label>
                    <input type="date" name="buyingDate" class="form-control" value="<?= e($asset['buyingDate']) ?>">
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Warranty Expiry') ?></label>
                    <input type="date" name="warrantyTill" class="form-control" value="<?= e($asset['warrantyTill']) ?>">
                </div>

                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted"><?= __('Status') ?></label>
                    <select name="status" class="form-select">
                        <option value="In Service" <?= $asset['status'] === 'In Service' ? 'selected' : '' ?>><?= __('In Service') ?></option>
                        <option value="In Stock" <?= $asset['status'] === 'In Stock' ? 'selected' : '' ?>><?= __('In Stock') ?></option>
                        <option value="Under Maintenance" <?= $asset['status'] === 'Under Maintenance' ? 'selected' : '' ?>><?= __('Under Maintenance') ?></option>
                        <option value="Retired" <?= $asset['status'] === 'Retired' ? 'selected' : '' ?>><?= __('Retired') ?></option>
                    </select>
                </div>
                <div class="col-md-4">
                    <label class="form-label fw-bold small text-muted">Maintenance Team</label>
                    <select name="maintenanceTeam" class="form-select">
                        <option value="IT" <?= ($asset['maintenanceTeam'] ?? '') === 'IT' ? 'selected' : '' ?>>IT Infrastructure</option>
                        <option value="BIOMEDICAL" <?= ($asset['maintenanceTeam'] ?? '') === 'BIOMEDICAL' ? 'selected' : '' ?>>Biomedical Engineering</option>
                        <option value="GENERAL" <?= ($asset['maintenanceTeam'] ?? '') === 'GENERAL' ? 'selected' : '' ?>>General Maintenance</option>
                    </select>
                </div>

                <div class="col-12">
                    <label class="form-label fw-bold small text-muted">Technical Specifications / Notes</label>
                    <textarea name="specs" class="form-control" rows="3"><?= e($asset['specs']) ?></textarea>
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
