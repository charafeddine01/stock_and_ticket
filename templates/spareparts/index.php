<?php
$pageTitle = I18n::isArabic() ? "قطع الغيار والمخزون الاحتياطي" : "Reserve Stock & Spare Parts Inventory";
$activeNav = 'spareparts';
ob_start();
?>
<div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
    <div>
        <h4 class="fw-bold mb-0">
            <?= __('Reserve Stock & Spare Parts') ?>
            <span class="badge bg-primary fs-6 align-middle ms-2"><?= $totalItems ?> <?= __('Total Items') ?></span>
        </h4>
        <small class="text-muted">
            <?= $isAr ? "إدارة المخزون الاحتياطي لفرق الصيانة: المستهلكات، قطع الغيار، ومخزون الصيانة الدورية، والقطع مزدوجة الاستخدام" : "Manage department reserve stock for periodic maintenance, consumables, spare parts, and dual-function items." ?>
        </small>
    </div>
    <div class="d-flex gap-2">
        <a href="<?= url('spareparts/transactions') ?>" class="btn btn-outline-secondary">
            <i class="fa-solid fa-clock-rotate-left me-1"></i> <?= __('Stock Transactions') ?>
        </a>
        <button type="button" class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#addPartModal">
            <i class="fa-solid fa-plus me-1"></i> <?= __('Add Reserve Item') ?>
        </button>
    </div>
</div>

<!-- 4 KPI Summary Cards -->
<div class="row g-3 mb-4">
    <div class="col-md-3">
        <div class="card stat-card stat-card-blue shadow-sm p-3">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="text-muted small text-uppercase fw-bold"><?= $isAr ? 'إجمالي الأصناف' : 'Total Reserve SKUs' ?></div>
                    <div class="h3 fw-bold text-primary mb-0"><?= $totalItems ?></div>
                </div>
                <div class="stat-icon bg-primary bg-opacity-10 text-primary"><i class="fa-solid fa-layer-group"></i></div>
            </div>
        </div>
    </div>
    <div class="col-md-3">
        <div class="card stat-card stat-card-emerald shadow-sm p-3">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="text-muted small text-uppercase fw-bold"><?= $isAr ? 'الكمية الإجمالية' : 'Total Units on Shelf' ?></div>
                    <div class="h3 fw-bold text-success mb-0"><?= $totalUnits ?></div>
                </div>
                <div class="stat-icon bg-success bg-opacity-10 text-success"><i class="fa-solid fa-boxes-stacked"></i></div>
            </div>
        </div>
    </div>
    <div class="col-md-3">
        <div class="card stat-card stat-card-rose shadow-sm p-3">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="text-muted small text-uppercase fw-bold"><?= __('Low Reserve Alerts') ?></div>
                    <div class="h3 fw-bold text-danger mb-0"><?= $lowStockCount ?></div>
                </div>
                <div class="stat-icon bg-danger bg-opacity-10 text-danger"><i class="fa-solid fa-triangle-exclamation"></i></div>
            </div>
        </div>
    </div>
    <div class="col-md-3">
        <div class="card stat-card stat-card-purple shadow-sm p-3">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="text-muted small text-uppercase fw-bold"><?= $isAr ? 'قيمة المخزون الاحتياطي' : 'Reserve Stock Value' ?></div>
                    <div class="h3 fw-bold text-info mb-0"><?= format_currency($totalValue) ?></div>
                </div>
                <div class="stat-icon bg-info bg-opacity-10 text-info"><i class="fa-solid fa-dollar-sign"></i></div>
            </div>
        </div>
    </div>
</div>

<!-- Filter Toolbar -->
<div class="card shadow-sm border-0 mb-4">
    <div class="card-body p-3">
        <form method="get" action="<?= url('spareparts') ?>" class="row g-2 align-items-center">
            <div class="col-md-4">
                <div class="input-group">
                    <span class="input-group-text bg-white border-end-0 text-muted"><i class="fa-solid fa-magnifying-glass"></i></span>
                    <input type="text" name="search" value="<?= e($search) ?>" class="form-control border-start-0 ps-0" placeholder="<?= $isAr ? 'بحث بالباركود أو اسم القطعة أو الموقع...' : 'Search by SKU, Part Name, Location...' ?>">
                </div>
            </div>
            <div class="col-md-3">
                <select name="category" class="form-select" onchange="this.form.submit()">
                    <option value=""><?= $isAr ? 'كافة التصنيفات' : 'All Categories' ?></option>
                    <?php foreach ($categories as $cat): ?>
                        <option value="<?= e($cat) ?>" <?= $category === $cat ? 'selected' : '' ?>><?= e($cat) ?></option>
                    <?php endforeach; ?>
                </select>
            </div>
            <div class="col-md-3">
                <select name="filter" class="form-select" onchange="this.form.submit()">
                    <option value=""><?= $isAr ? 'كافة العناصر' : 'All Stock Levels' ?></option>
                    <option value="low_stock" <?= $filter === 'low_stock' ? 'selected' : '' ?>><?= $isAr ? 'تنبيه نقص المخزون فقط' : 'Low Stock Alert Only' ?></option>
                    <option value="dual_nature" <?= $filter === 'dual_nature' ? 'selected' : '' ?>><?= $isAr ? 'قطع قابلة للتخصيص كأصل مستقل' : 'Dual-Nature (Convertible to Asset)' ?></option>
                </select>
            </div>
            <div class="col-md-2">
                <a href="<?= url('spareparts') ?>" class="btn btn-outline-secondary w-100"><?= __('Reset') ?></a>
            </div>
        </form>
    </div>
</div>

<!-- Spare Parts Inventory Table -->
<div class="card shadow-sm border-0 mb-4">
    <div class="card-body p-0">
        <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
                <thead class="table-light small">
                    <tr>
                        <th>Part Number / SKU</th>
                        <th><?= __('Part / Item Name') ?></th>
                        <th>Category</th>
                        <th>Team</th>
                        <th><?= __('Quantity in Stock') ?></th>
                        <th><?= __('Unit Cost') ?></th>
                        <th><?= __('Shelf / Location') ?></th>
                        <th class="text-end"><?= __('Actions') ?></th>
                    </tr>
                </thead>
                <tbody>
                    <?php if (empty($parts)): ?>
                        <tr><td colspan="8" class="text-center py-5 text-muted"><?= $isAr ? 'لا توجد قطع غيار مطابقة للبحث' : 'No spare parts found.' ?></td></tr>
                    <?php else: ?>
                        <?php foreach ($parts as $p): 
                            $isLow = $p['quantity'] <= $p['minThreshold'];
                        ?>
                            <tr class="<?= $isLow ? 'table-warning-subtle' : '' ?>">
                                <td>
                                    <span class="badge bg-light text-dark border font-monospace"><?= e($p['partNo']) ?></span>
                                    <?php if ($p['canBeAsset']): ?>
                                        <span class="badge bg-info-subtle text-info border" title="Can be commissioned as independent hospital asset">Dual-Nature</span>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <div class="fw-bold text-dark"><?= e($p['name']) ?></div>
                                    <?php if (!empty($p['name_ar'])): ?>
                                        <div class="text-muted small"><?= e($p['name_ar']) ?></div>
                                    <?php endif; ?>
                                </td>
                                <td><span class="badge bg-light text-dark border"><?= e($p['category']) ?></span></td>
                                <td><span class="badge bg-primary-subtle text-primary border"><?= e($p['maintenanceTeam']) ?></span></td>
                                <td>
                                    <span class="fs-6 fw-bold <?= $isLow ? 'text-danger' : 'text-success' ?>">
                                        <?= $p['quantity'] ?>
                                    </span>
                                    <span class="text-muted small">/ min <?= $p['minThreshold'] ?></span>
                                    <?php if ($isLow): ?>
                                        <i class="fa-solid fa-triangle-exclamation text-danger ms-1" title="Below minimum threshold"></i>
                                    <?php endif; ?>
                                </td>
                                <td><span class="fw-bold text-dark"><?= format_currency($p['unitCost']) ?></span></td>
                                <td><span class="badge bg-light text-dark border"><i class="fa-solid fa-warehouse me-1 text-muted"></i><?= e($p['location'] ?: 'Unassigned') ?></span></td>
                                <td class="text-end">
                                    <div class="btn-group btn-group-sm">
                                        <button type="button" class="btn btn-outline-success" onclick="openRestockModal(<?= $p['id'] ?>, '<?= addslashes($p['name']) ?>')" title="<?= __('Restock') ?>">
                                            <i class="fa-solid fa-cart-plus me-1"></i> <?= __('Restock') ?>
                                        </button>
                                        <?php if ($p['canBeAsset'] && $p['quantity'] > 0): ?>
                                            <button type="button" class="btn btn-outline-primary" onclick="openCommissionModal(<?= $p['id'] ?>, '<?= addslashes($p['name']) ?>')" title="<?= __('Commission as Asset') ?>">
                                                <i class="fa-solid fa-wand-magic-sparkles"></i>
                                            </button>
                                        <?php endif; ?>
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

<!-- Modal: Restock -->
<div class="modal fade" id="restockModal" tabindex="-1" aria-labelledby="restockModalLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content border-0 shadow-lg">
            <form method="post" action="<?= url('spareparts/restock') ?>">
                <input type="hidden" name="sparePartId" id="restockPartId">
                <div class="modal-header">
                    <h5 class="modal-title fw-bold" id="restockModalLabel"><?= __('Restock Item') ?></h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted">Item Name</label>
                        <input type="text" id="restockPartName" class="form-control bg-light" readonly>
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted">Units to Add <span class="text-danger">*</span></label>
                        <input type="number" name="quantity" class="form-control" value="5" min="1" required>
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted">Purchase / PO Reference Note</label>
                        <input type="text" name="notes" class="form-control" value="Replenishment delivery batch">
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-light" data-bs-dismiss="modal"><?= __('Cancel') ?></button>
                    <button type="submit" class="btn btn-success fw-bold">
                        <i class="fa-solid fa-check me-1"></i> Confirm Restock
                    </button>
                </div>
            </form>
        </div>
    </div>
</div>

<!-- Modal: Commission as Independent Asset -->
<div class="modal fade" id="commissionModal" tabindex="-1" aria-labelledby="commissionModalLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content border-0 shadow-lg">
            <form method="post" action="<?= url('spareparts/commissionAsAsset') ?>">
                <input type="hidden" name="sparePartId" id="commissionPartId">
                <div class="modal-header">
                    <h5 class="modal-title fw-bold" id="commissionModalLabel"><?= __('Commission as Asset') ?></h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <p class="small text-muted mb-3">
                        <?= $isAr ? 'سيتم خصم وحدة واحدة من المخزون الاحتياطي وتسجيل أصل مستقل برقم عهدة جديد في سجل أصول المستشفى.' : 'This action deducts 1 unit from reserve stock and creates a new independent hospital equipment asset record with its own asset tag.' ?>
                    </p>
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted">Item Description</label>
                        <input type="text" id="commissionPartName" class="form-control bg-light" readonly>
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted">New Hospital Asset Tag <span class="text-danger">*</span></label>
                        <input type="text" name="hospitalNo" class="form-control" value="HOSP-CMP-<?= rand(100, 999) ?>" required>
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted">Serial Number (S/N)</label>
                        <input type="text" name="serialNo" class="form-control" placeholder="Manufacturer serial number">
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted">Initial Location / Room</label>
                        <select name="roomId" class="form-select">
                            <?php foreach ($rooms as $r): ?>
                                <option value="<?= $r['id'] ?>">Room <?= $r['roomNo'] ?> - <?= e($r['name']) ?></option>
                            <?php endforeach; ?>
                        </select>
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted">Assigned User Custody</label>
                        <select name="userId" class="form-select">
                            <?php foreach ($users as $u): ?>
                                <option value="<?= $u['id'] ?>"><?= e($u['fullName']) ?> (<?= e($u['role']) ?>)</option>
                            <?php endforeach; ?>
                        </select>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-light" data-bs-dismiss="modal"><?= __('Cancel') ?></button>
                    <button type="submit" class="btn btn-primary fw-bold">
                        <i class="fa-solid fa-wand-magic-sparkles me-1"></i> Commission Asset
                    </button>
                </div>
            </form>
        </div>
    </div>
</div>

<!-- Modal: Add New Reserve Part -->
<div class="modal fade" id="addPartModal" tabindex="-1" aria-labelledby="addPartModalLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content border-0 shadow-lg">
            <form method="post" action="<?= url('spareparts/create') ?>">
                <div class="modal-header">
                    <h5 class="modal-title fw-bold" id="addPartModalLabel"><?= __('Add Reserve Item') ?></h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <div class="row g-2">
                        <div class="col-md-6">
                            <label class="form-label fw-bold small text-muted">Part SKU <span class="text-danger">*</span></label>
                            <input type="text" name="partNo" class="form-control" placeholder="e.g. SP-IT-RAM-32GB" required>
                        </div>
                        <div class="col-md-6">
                            <label class="form-label fw-bold small text-muted">Category</label>
                            <select name="category" class="form-select">
                                <option value="Spare Parts">Spare Parts</option>
                                <option value="Consumables">Consumables</option>
                                <option value="Periodic Maintenance Reserve">Periodic Maintenance Reserve</option>
                            </select>
                        </div>
                        <div class="col-12">
                            <label class="form-label fw-bold small text-muted">Part Name (English) <span class="text-danger">*</span></label>
                            <input type="text" name="name" class="form-control" placeholder="e.g. Kingston 32GB DDR5 Desktop RAM" required>
                        </div>
                        <div class="col-12">
                            <label class="form-label fw-bold small text-muted">Part Name (Arabic)</label>
                            <input type="text" name="name_ar" class="form-control" placeholder="e.g. ذاكرة رام كينغستون 32 جيجابايت">
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-bold small text-muted">Initial Quantity</label>
                            <input type="number" name="quantity" class="form-control" value="10" min="0">
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-bold small text-muted">Min Threshold</label>
                            <input type="number" name="minThreshold" class="form-control" value="3" min="1">
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-bold small text-muted">Unit Cost ($)</label>
                            <input type="number" step="0.01" name="unitCost" class="form-control" value="50.00">
                        </div>
                        <div class="col-md-6">
                            <label class="form-label fw-bold small text-muted">Storage Shelf / Location</label>
                            <input type="text" name="location" class="form-control" placeholder="e.g. Shelf A-02">
                        </div>
                        <div class="col-md-6">
                            <label class="form-label fw-bold small text-muted">Team Queue</label>
                            <select name="maintenanceTeam" class="form-select">
                                <option value="IT">IT Infrastructure</option>
                                <option value="BIOMEDICAL">Biomedical</option>
                                <option value="GENERAL">General Maintenance</option>
                            </select>
                        </div>
                        <div class="col-12">
                            <div class="form-check mt-2">
                                <input class="form-check-input" type="checkbox" name="canBeAsset" id="canBeAssetCheck" value="1">
                                <label class="form-check-label fw-semibold small" for="canBeAssetCheck">
                                    Dual-Nature (Can function and be commissioned as independent hospital asset)
                                </label>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-light" data-bs-dismiss="modal"><?= __('Cancel') ?></button>
                    <button type="submit" class="btn btn-primary fw-bold"><?= __('Save') ?></button>
                </div>
            </form>
        </div>
    </div>
</div>

<script>
function openRestockModal(id, name) {
    document.getElementById('restockPartId').value = id;
    document.getElementById('restockPartName').value = name;
    new bootstrap.Modal(document.getElementById('restockModal')).show();
}

function openCommissionModal(id, name) {
    document.getElementById('commissionPartId').value = id;
    document.getElementById('commissionPartName').value = name;
    new bootstrap.Modal(document.getElementById('commissionModal')).show();
}
</script>

<?php
$content = ob_get_clean();
require __DIR__ . '/../layout.php';
?>
