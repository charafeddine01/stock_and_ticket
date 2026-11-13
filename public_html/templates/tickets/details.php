<?php
$pageTitle = "Ticket #" . $ticket['id'] . " - " . $ticket['title'];
$activeNav = 'tickets';
ob_start();
?>
<div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
    <div>
        <nav aria-label="breadcrumb">
            <ol class="breadcrumb mb-1 small">
                <li class="breadcrumb-item"><a href="<?= url('tickets') ?>" class="text-decoration-none"><?= __('Tickets & Helpdesk') ?></a></li>
                <li class="breadcrumb-item active" aria-current="page">Ticket #<?= $ticket['id'] ?></li>
            </ol>
        </nav>
        <h4 class="fw-bold mb-0 text-dark d-flex align-items-center gap-2">
            <span>Ticket #<?= $ticket['id'] ?>: <?= e($ticket['title']) ?></span>
            <?= render_ticket_status_badge($ticket['statusId']) ?>
            <?= render_ticket_priority_badge($ticket['priority']) ?>
        </h4>
        <small class="text-muted">
            Opened by <?= e($ticket['createdByName']) ?> on <?= format_date($ticket['createdAt'], true) ?> &bull; Queue: <strong><?= e($ticket['maintenanceTeam']) ?> Engineering</strong>
        </small>
    </div>
    <div class="d-flex gap-2">
        <a href="<?= url('tickets/workorder', ['id' => $ticket['id']]) ?>" target="_blank" class="btn btn-outline-dark">
            <i class="fa-solid fa-print me-1"></i> <?= __('Print Work Order') ?>
        </a>
    </div>
</div>

<div class="row g-4">
    <!-- Main Ticket Timeline & Description -->
    <div class="col-lg-8">
        <!-- Ticket Issue Description -->
        <div class="card shadow-sm border-0 mb-4">
            <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center">
                <h6 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-file-lines text-primary me-2"></i>Problem Description &amp; Clinical Impact</h6>
                <span class="badge bg-light text-dark border"><?= e($ticket['departmentName'] ?? 'General') ?></span>
            </div>
            <div class="card-body">
                <p class="mb-0 text-dark" style="white-space: pre-wrap;"><?= e($ticket['description']) ?></p>
            </div>
        </div>

        <!-- Discussion & Diagnostics Timeline -->
        <div class="card shadow-sm border-0 mb-4">
            <div class="card-header bg-white py-3 border-bottom">
                <h6 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-comments text-primary me-2"></i><?= __('Discussion Timeline') ?></h6>
            </div>
            <div class="card-body">
                <?php if (empty($messages)): ?>
                    <div class="text-center py-4 text-muted small">No diagnostic notes or comments yet.</div>
                <?php else: ?>
                    <div class="timeline">
                        <?php foreach ($messages as $m): ?>
                            <div class="d-flex gap-3 mb-3 pb-3 border-bottom <?= $m['isInternal'] ? 'bg-light-subtle p-2 rounded' : '' ?>">
                                <div class="text-<?= $m['isInternal'] ? 'warning' : 'primary' ?> mt-1">
                                    <i class="fa-solid <?= $m['isInternal'] ? 'fa-shield-halved' : 'fa-circle-dot' ?>"></i>
                                </div>
                                <div class="w-100">
                                    <div class="d-flex justify-content-between align-items-center">
                                        <span class="fw-bold text-dark">
                                            <?= e($m['authorName'] ?: 'Staff User') ?>
                                            <?php if ($m['isInternal']): ?>
                                                <span class="badge bg-warning text-dark ms-1" style="font-size: 0.65rem;">System Log</span>
                                            <?php endif; ?>
                                        </span>
                                        <small class="text-muted"><?= format_date($m['createdAt'], true) ?></small>
                                    </div>
                                    <div class="text-dark mt-1" style="white-space: pre-wrap; font-size: 0.9rem;"><?= e($m['message']) ?></div>
                                </div>
                            </div>
                        <?php endforeach; ?>
                    </div>
                <?php endif; ?>

                <!-- Post Comment Form -->
                <form method="post" action="<?= url('tickets/addComment') ?>" class="mt-4">
                    <input type="hidden" name="ticketId" value="<?= $ticket['id'] ?>">
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted"><?= __('Post Update / Diagnostic') ?></label>
                        <textarea name="message" class="form-control" rows="3" placeholder="Enter diagnostic findings, inspection status, or client response..." required></textarea>
                    </div>
                    <div class="text-end">
                        <button type="submit" class="btn btn-primary px-4 fw-bold">
                            <i class="fa-solid fa-paper-plane me-1"></i> <?= __('Post Update / Diagnostic') ?>
                        </button>
                    </div>
                </form>
            </div>
        </div>

        <!-- Used Spare Parts in this Ticket -->
        <div class="card shadow-sm border-0 mb-4">
            <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center">
                <h6 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-gears text-warning me-2"></i><?= __('Used Spare Parts') ?></h6>
                <button type="button" class="btn btn-sm btn-outline-warning text-dark fw-bold" data-bs-toggle="modal" data-bs-target="#useSparePartModal">
                    <i class="fa-solid fa-plus me-1"></i> <?= __('Log Spare Part Usage') ?>
                </button>
            </div>
            <div class="card-body p-0">
                <?php if (empty($usedParts)): ?>
                    <div class="text-center py-4 text-muted small"><?= __('No spare parts used yet for this ticket.') ?></div>
                <?php else: ?>
                    <div class="table-responsive">
                        <table class="table table-hover align-middle mb-0">
                            <thead class="table-light small">
                                <tr>
                                    <th>Part SKU</th>
                                    <th>Item Name</th>
                                    <th>Quantity</th>
                                    <th>Unit Cost</th>
                                    <th>Total</th>
                                    <th>Logged By</th>
                                </tr>
                            </thead>
                            <tbody>
                                <?php 
                                $totalPartCost = 0;
                                foreach ($usedParts as $up): 
                                    $qty = abs($up['quantity']);
                                    $cost = $qty * (float)$up['unitCost'];
                                    $totalPartCost += $cost;
                                ?>
                                    <tr>
                                        <td><span class="badge bg-light text-dark border font-monospace"><?= e($up['partNo']) ?></span></td>
                                        <td class="fw-semibold text-dark"><?= e($up['partName']) ?></td>
                                        <td><span class="badge bg-primary"><?= $qty ?></span></td>
                                        <td><?= format_currency($up['unitCost']) ?></td>
                                        <td class="fw-bold text-success"><?= format_currency($cost) ?></td>
                                        <td class="text-muted small"><?= e($up['technicianName']) ?></td>
                                    </tr>
                                <?php endforeach; ?>
                            </tbody>
                            <tfoot class="table-light">
                                <tr>
                                    <th colspan="4" class="text-end">Total Parts Incurred:</th>
                                    <th colspan="2" class="fw-bold text-success fs-6"><?= format_currency($totalPartCost) ?></th>
                                </tr>
                            </tfoot>
                        </table>
                    </div>
                <?php endif; ?>
            </div>
        </div>
    </div>

    <!-- Sidebar: Ticket Controls, Status, Asset details -->
    <div class="col-lg-4">
        <!-- Ticket Management Controls (Status & Technician) -->
        <div class="card shadow-sm border-0 mb-4">
            <div class="card-header bg-white py-3 border-bottom">
                <h6 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-sliders text-primary me-2"></i>Technician &amp; Status Controls</h6>
            </div>
            <div class="card-body">
                <form method="post" action="<?= url('tickets/updateStatus') ?>">
                    <input type="hidden" name="ticketId" value="<?= $ticket['id'] ?>">
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted"><?= __('Status') ?></label>
                        <select name="statusId" class="form-select">
                            <?php foreach ($statuses as $s): ?>
                                <option value="<?= $s['id'] ?>" <?= $ticket['statusId'] == $s['id'] ? 'selected' : '' ?>>
                                    <?= e($s['status']) ?>
                                </option>
                            <?php endforeach; ?>
                        </select>
                    </div>

                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted"><?= __('Assigned Technician') ?></label>
                        <select name="assignedToUserId" class="form-select">
                            <option value="">-- Unassigned --</option>
                            <?php foreach ($allTechnicians as $tech): ?>
                                <option value="<?= $tech['id'] ?>" <?= $ticket['assignedToUserId'] == $tech['id'] ? 'selected' : '' ?>>
                                    <?= e($tech['fullName']) ?> (<?= e($tech['role']) ?>)
                                </option>
                            <?php endforeach; ?>
                        </select>
                    </div>

                    <button type="submit" class="btn btn-primary w-100 fw-bold">
                        <i class="fa-solid fa-check me-1"></i> Update Ticket Status
                    </button>
                </form>
            </div>
        </div>

        <!-- Related Hospital Asset -->
        <div class="card shadow-sm border-0 mb-4">
            <div class="card-header bg-white py-3 border-bottom">
                <h6 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-boxes-stacked text-primary me-2"></i>Related Hospital Equipment</h6>
            </div>
            <div class="card-body">
                <?php if (!empty($ticket['assetId'])): ?>
                    <div class="d-flex justify-content-between align-items-center mb-2">
                        <a href="<?= url('assets/details', ['id' => $ticket['assetId']]) ?>" class="fw-bold fs-6 text-decoration-none">
                            <?= e($ticket['assetTag']) ?>
                        </a>
                        <?= render_asset_status_badge($ticket['assetStatus']) ?>
                    </div>
                    <div class="fw-semibold text-dark"><?= e($ticket['assetBrand']) ?> <?= e($ticket['assetModel']) ?></div>
                    <div class="text-muted small mb-2"><?= e($ticket['assetType']) ?></div>
                    <div class="small text-muted"><i class="fa-solid fa-location-dot me-1 text-danger"></i> Room <?= $ticket['roomNo'] ?> (<?= e($ticket['roomName']) ?>)</div>
                    <div class="mt-3 pt-2 border-top">
                        <a href="<?= url('assets/details', ['id' => $ticket['assetId']]) ?>" class="btn btn-sm btn-outline-primary w-100">
                            View Equipment Record &rarr;
                        </a>
                    </div>
                <?php else: ?>
                    <div class="text-muted small">No specific asset linked to this ticket.</div>
                <?php endif; ?>
            </div>
        </div>

        <!-- Requester Information -->
        <div class="card shadow-sm border-0 mb-4">
            <div class="card-header bg-white py-3 border-bottom">
                <h6 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-user-doctor text-info me-2"></i>Requester / Department</h6>
            </div>
            <div class="card-body">
                <div class="fw-bold text-dark"><?= e($ticket['createdByName']) ?></div>
                <div class="text-muted small mb-2"><?= e($ticket['departmentName']) ?></div>
                <?php if (!empty($ticket['creatorPhone'])): ?>
                    <div class="small text-muted"><i class="fa-solid fa-phone me-1"></i> <?= e($ticket['creatorPhone']) ?></div>
                <?php endif; ?>
                <?php if (!empty($ticket['creatorEmail'])): ?>
                    <div class="small text-muted"><i class="fa-solid fa-envelope me-1"></i> <?= e($ticket['creatorEmail']) ?></div>
                <?php endif; ?>
            </div>
        </div>
    </div>
</div>

<!-- Modal: Log Spare Part Usage -->
<div class="modal fade" id="useSparePartModal" tabindex="-1" aria-labelledby="useSparePartModalLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content border-0 shadow-lg">
            <form method="post" action="<?= url('tickets/logSparePart') ?>">
                <input type="hidden" name="ticketId" value="<?= $ticket['id'] ?>">
                <div class="modal-header">
                    <h5 class="modal-title fw-bold" id="useSparePartModalLabel"><?= __('Log Spare Part Usage') ?></h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted"><?= __('Select Spare Part') ?> <span class="text-danger">*</span></label>
                        <select name="sparePartId" class="form-select" required>
                            <option value="">-- Choose from available reserve stock --</option>
                            <?php foreach ($availableParts as $p): ?>
                                <option value="<?= $p['id'] ?>">
                                    <?= e($p['name']) ?> (SKU: <?= e($p['partNo']) ?>) &bull; Avail: <?= $p['quantity'] ?> &bull; $<?= number_format($p['unitCost'], 2) ?>
                                </option>
                            <?php endforeach; ?>
                        </select>
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted">Quantity Consumed</label>
                        <input type="number" name="quantity" class="form-control" value="1" min="1" required>
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-bold small text-muted">Technician Notes / Work Performed</label>
                        <input type="text" name="notes" class="form-control" value="Fitted during ticket #<?= $ticket['id'] ?> repair" required>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-light" data-bs-dismiss="modal"><?= __('Cancel') ?></button>
                    <button type="submit" class="btn btn-warning text-dark fw-bold">
                        <i class="fa-solid fa-check me-1"></i> Confirm &amp; Deduct Stock
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
