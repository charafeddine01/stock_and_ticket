<?php
$pageTitle = I18n::isArabic() ? "فتح تذكرة صيانة جديدة" : "Open Service Ticket";
$activeNav = 'tickets';
ob_start();
?>
<div class="card shadow-sm border-0 max-w-4xl mx-auto">
    <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center">
        <h5 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-plus-circle text-primary me-2"></i><?= __('Open New Ticket') ?></h5>
        <a href="<?= url('tickets') ?>" class="btn btn-sm btn-outline-secondary"><?= __('Cancel') ?></a>
    </div>
    <div class="card-body p-4">
        <form method="post" action="<?= url('tickets/create') ?>">
            <div class="row g-3">
                <div class="col-12">
                    <label class="form-label fw-bold small text-muted">Problem Summary / Title <span class="text-danger">*</span></label>
                    <input type="text" name="title" class="form-control" placeholder="e.g. ICU Bed 2 Vital Signs Monitor not displaying ECG waveform" required>
                </div>

                <div class="col-md-6">
                    <label class="form-label fw-bold small text-muted"><?= __('Priority') ?> <span class="text-danger">*</span></label>
                    <select name="priority" class="form-select">
                        <option value="Low"><?= __('Low') ?> (Routine query / minor check)</option>
                        <option value="Medium" selected><?= __('Medium') ?> (Standard operational issue)</option>
                        <option value="High"><?= __('High') ?> (Urgent patient or diagnostic disruption)</option>
                        <option value="Urgent"><?= __('Urgent') ?> 🔥 (Critical life-support / ICU emergency)</option>
                    </select>
                </div>

                <div class="col-md-6">
                    <label class="form-label fw-bold small text-muted">Engineering Queue / Team</label>
                    <select name="maintenanceTeam" class="form-select">
                        <option value="BIOMEDICAL">Biomedical Engineering (Clinical Devices)</option>
                        <option value="IT" selected>IT &amp; Systems (Servers, Workstations, PACS)</option>
                        <option value="GENERAL">General Facilities (HVAC, Power, Gas)</option>
                    </select>
                </div>

                <div class="col-md-6">
                    <label class="form-label fw-bold small text-muted">Related Hospital Equipment (Optional)</label>
                    <select name="assetId" class="form-select">
                        <option value="">-- No specific device / General Department issue --</option>
                        <?php foreach ($assets as $a): ?>
                            <option value="<?= $a['id'] ?>">
                                <?= e($a['hospitalNo']) ?> &bull; <?= e($a['brand']) ?> <?= e($a['modelNo']) ?> (<?= e($a['type']) ?>)
                            </option>
                        <?php endforeach; ?>
                    </select>
                </div>

                <div class="col-md-6">
                    <label class="form-label fw-bold small text-muted">Department</label>
                    <select name="departmentId" class="form-select">
                        <?php foreach ($departments as $d): ?>
                            <option value="<?= $d['id'] ?>"><?= e($d['name']) ?></option>
                        <?php endforeach; ?>
                    </select>
                </div>

                <div class="col-12">
                    <label class="form-label fw-bold small text-muted">Detailed Clinical / Diagnostic Description <span class="text-danger">*</span></label>
                    <textarea name="description" class="form-control" rows="4" placeholder="Describe symptoms, error codes, clinical alarms, or steps to reproduce..." required></textarea>
                </div>

                <div class="col-12 text-end pt-3 border-top">
                    <button type="submit" class="btn btn-primary px-4 fw-bold">
                        <i class="fa-solid fa-paper-plane me-1"></i> Submit Service Ticket
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
