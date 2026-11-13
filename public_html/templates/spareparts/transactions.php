<?php
$pageTitle = I18n::isArabic() ? "سجل حركات المخزون وقطع الغيار" : "Spare Parts Stock Transactions Log";
$activeNav = 'spareparts';
ob_start();
?>
<div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
    <div>
        <nav aria-label="breadcrumb">
            <ol class="breadcrumb mb-1 small">
                <li class="breadcrumb-item"><a href="<?= url('spareparts') ?>" class="text-decoration-none"><?= __('Spare Parts') ?></a></li>
                <li class="breadcrumb-item active" aria-current="page"><?= __('Stock Transactions') ?></li>
            </ol>
        </nav>
        <h4 class="fw-bold mb-0 text-dark"><?= __('Stock Transactions') ?></h4>
        <small class="text-muted">Complete audit log of all spare part intakes, usage in service tickets, and independent asset commissioning.</small>
    </div>
    <div>
        <a href="<?= url('spareparts') ?>" class="btn btn-outline-secondary">
            <i class="fa-solid fa-arrow-left me-1"></i> Back to Inventory
        </a>
    </div>
</div>

<div class="card shadow-sm border-0">
    <div class="card-body p-0">
        <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
                <thead class="table-light small">
                    <tr>
                        <th>Date &amp; Time</th>
                        <th>Part SKU</th>
                        <th>Part Description</th>
                        <th>Transaction Type</th>
                        <th>Quantity Changed</th>
                        <th>Service Ticket</th>
                        <th>Authorized Staff</th>
                    </tr>
                </thead>
                <tbody>
                    <?php if (empty($transactions)): ?>
                        <tr><td colspan="7" class="text-center py-4 text-muted small">No transactions recorded.</td></tr>
                    <?php else: ?>
                        <?php foreach ($transactions as $tx): 
                            $isNegative = $tx['quantity'] < 0;
                        ?>
                            <tr>
                                <td class="text-muted small"><?= format_date($tx['createdAt'], true) ?></td>
                                <td><span class="badge bg-light text-dark border font-monospace"><?= e($tx['partNo']) ?></span></td>
                                <td class="fw-semibold text-dark"><?= e($tx['partName']) ?></td>
                                <td>
                                    <span class="badge <?= $isNegative ? 'bg-warning-subtle text-warning-emphasis border' : 'bg-success-subtle text-success border' ?>">
                                        <?= e($tx['type']) ?>
                                    </span>
                                </td>
                                <td>
                                    <span class="fw-bold fs-6 <?= $isNegative ? 'text-danger' : 'text-success' ?>">
                                        <?= $tx['quantity'] > 0 ? '+' : '' ?><?= $tx['quantity'] ?>
                                    </span>
                                </td>
                                <td>
                                    <?php if (!empty($tx['ticketId'])): ?>
                                        <a href="<?= url('tickets/details', ['id' => $tx['ticketId']]) ?>" class="text-decoration-none fw-semibold">
                                            Ticket #<?= $tx['ticketId'] ?>
                                        </a>
                                    <?php else: ?>
                                        <span class="text-muted small"><?= e($tx['notes']) ?></span>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <span class="small text-dark fw-semibold"><i class="fa-regular fa-user me-1 text-primary"></i> <?= e($tx['staffName']) ?></span>
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
