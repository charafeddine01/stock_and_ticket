<?php
$pageTitle = $profileUser['fullName'] . " - Profile";
$activeNav = 'profile';
ob_start();
?>
<div class="row g-4">
    <!-- User Overview Card -->
    <div class="col-lg-4">
        <div class="card shadow-sm border-0 mb-4 text-center p-4">
            <div class="mx-auto mb-3 bg-primary bg-opacity-10 text-primary rounded-circle d-flex align-items-center justify-content-center" style="width: 80px; height: 80px;">
                <i class="fa-solid fa-user-doctor fs-1"></i>
            </div>
            <h5 class="fw-bold text-dark mb-1"><?= e($profileUser['fullName']) ?></h5>
            <div class="badge bg-primary-subtle text-primary border mb-2"><?= e($profileUser['role']) ?></div>
            <div class="text-muted small"><?= e($isAr && !empty($profileUser['departmentNameAr']) ? $profileUser['departmentNameAr'] : $profileUser['departmentName']) ?></div>

            <hr class="my-3">

            <div class="text-start small">
                <div class="mb-2"><strong>Domain User:</strong> <span class="font-monospace text-muted"><?= e($profileUser['domainUser']) ?></span></div>
                <div class="mb-2"><strong>Email:</strong> <span class="text-muted"><?= e($profileUser['email']) ?></span></div>
                <div class="mb-2"><strong>Phone:</strong> <span class="text-muted"><?= e($profileUser['phone']) ?></span></div>
                <div><strong>Assigned Team:</strong> <span class="badge bg-light text-dark border"><?= e($profileUser['maintenanceTeam'] ?? 'IT') ?></span></div>
            </div>

            <!-- Quick switch button if not active -->
            <?php if ($profileUser['id'] != Auth::id()): ?>
                <form method="post" action="<?= url('account/switchUser') ?>" class="mt-4">
                    <input type="hidden" name="id" value="<?= $profileUser['id'] ?>">
                    <input type="hidden" name="returnUrl" value="<?= e($_SERVER['REQUEST_URI'] ?? url()) ?>">
                    <button type="submit" class="btn btn-primary w-100 fw-bold">
                        <i class="fa-solid fa-mask me-1"></i> Switch to this Persona
                    </button>
                </form>
            <?php endif; ?>
        </div>
    </div>

    <!-- Assigned Hospital Assets & Tickets -->
    <div class="col-lg-8">
        <!-- Assigned Assets in Custody -->
        <div class="card shadow-sm border-0 mb-4">
            <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center">
                <h6 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-boxes-stacked text-primary me-2"></i>Assigned Equipment in Personal Custody</h6>
                <span class="badge bg-primary rounded-pill"><?= count($assignedAssets) ?> Assets</span>
            </div>
            <div class="card-body p-0">
                <?php if (empty($assignedAssets)): ?>
                    <div class="text-center py-4 text-muted small">No equipment assigned directly to this user.</div>
                <?php else: ?>
                    <div class="table-responsive">
                        <table class="table table-hover align-middle mb-0">
                            <thead class="table-light small">
                                <tr>
                                    <th>Asset Tag</th>
                                    <th>Equipment</th>
                                    <th>Status</th>
                                    <th>Location</th>
                                    <th class="text-end">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                <?php foreach ($assignedAssets as $a): ?>
                                    <tr>
                                        <td>
                                            <a href="<?= url('assets/details', ['id' => $a['id']]) ?>" class="fw-bold text-decoration-none">
                                                <?= e($a['hospitalNo']) ?>
                                            </a>
                                        </td>
                                        <td>
                                            <div class="fw-semibold text-dark"><?= e($a['brand']) ?> <?= e($a['modelNo']) ?></div>
                                            <div class="text-muted small"><?= e($a['type']) ?></div>
                                        </td>
                                        <td><?= render_asset_status_badge($a['status']) ?></td>
                                        <td><span class="badge bg-light text-dark border">Room <?= $a['roomNo'] ?> (<?= e($a['roomName']) ?>)</span></td>
                                        <td class="text-end">
                                            <a href="<?= url('assets/details', ['id' => $a['id']]) ?>" class="btn btn-sm btn-outline-primary">
                                                View &rarr;
                                            </a>
                                        </td>
                                    </tr>
                                <?php endforeach; ?>
                            </tbody>
                        </table>
                    </div>
                <?php endif; ?>
            </div>
        </div>

        <!-- Recent Tickets -->
        <div class="card shadow-sm border-0 mb-4">
            <div class="card-header bg-white py-3 border-bottom">
                <h6 class="fw-bold mb-0 text-dark"><i class="fa-solid fa-ticket text-info me-2"></i>Recent Service Tickets</h6>
            </div>
            <div class="card-body p-0">
                <?php if (empty($userTickets)): ?>
                    <div class="text-center py-4 text-muted small">No tickets found for this user.</div>
                <?php else: ?>
                    <div class="table-responsive">
                        <table class="table table-hover align-middle mb-0">
                            <thead class="table-light small">
                                <tr>
                                    <th>Ticket #</th>
                                    <th>Title</th>
                                    <th>Priority</th>
                                    <th>Status</th>
                                    <th>Created</th>
                                </tr>
                            </thead>
                            <tbody>
                                <?php foreach ($userTickets as $t): ?>
                                    <tr>
                                        <td><a href="<?= url('tickets/details', ['id' => $t['id']]) ?>" class="fw-bold text-decoration-none">#<?= $t['id'] ?></a></td>
                                        <td><a href="<?= url('tickets/details', ['id' => $t['id']]) ?>" class="text-dark fw-semibold text-decoration-none"><?= e($t['title']) ?></a></td>
                                        <td><?= render_ticket_priority_badge($t['priority']) ?></td>
                                        <td><?= render_ticket_status_badge($t['statusId']) ?></td>
                                        <td class="text-muted small"><?= format_date($t['createdAt']) ?></td>
                                    </tr>
                                <?php endforeach; ?>
                            </tbody>
                        </table>
                    </div>
                <?php endif; ?>
            </div>
        </div>
    </div>
</div>

<?php
$content = ob_get_clean();
require __DIR__ . '/../layout.php';
?>
