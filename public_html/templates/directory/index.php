<?php
$pageTitle = I18n::isArabic() ? "دليل المستشفى والمرافق" : "Hospital Directory & Infrastructure";
$activeNav = 'directory-' . $tab;
ob_start();
?>
<div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
    <div>
        <h4 class="fw-bold mb-0"><?= __('Hospital Directory') ?></h4>
        <small class="text-muted">Explore hospital staff custody directory, room registries, specialized departments, and service vendors.</small>
    </div>
</div>

<!-- Tabs Navigation -->
<ul class="nav nav-tabs nav-tabs-bordered mb-4" id="directoryTabs">
    <li class="nav-item">
        <a class="nav-link <?= $tab === 'users' ? 'active' : '' ?> fw-bold px-4" href="<?= url('directory', ['tab' => 'users']) ?>">
            <i class="fa-solid fa-user-doctor me-2 text-primary"></i> <?= __('Staff Directory') ?> (<?= count($users) ?>)
        </a>
    </li>
    <li class="nav-item">
        <a class="nav-link <?= $tab === 'rooms' ? 'active' : '' ?> fw-bold px-4" href="<?= url('directory', ['tab' => 'rooms']) ?>">
            <i class="fa-solid fa-door-open me-2 text-success"></i> <?= __('Hospital Rooms') ?> (<?= count($rooms) ?>)
        </a>
    </li>
    <li class="nav-item">
        <a class="nav-link <?= $tab === 'departments' ? 'active' : '' ?> fw-bold px-4" href="<?= url('directory', ['tab' => 'departments']) ?>">
            <i class="fa-solid fa-hospital me-2 text-info"></i> <?= __('Departments') ?> (<?= count($departments) ?>)
        </a>
    </li>
    <li class="nav-item">
        <a class="nav-link <?= $tab === 'suppliers' ? 'active' : '' ?> fw-bold px-4" href="<?= url('directory', ['tab' => 'suppliers']) ?>">
            <i class="fa-solid fa-truck-field me-2 text-warning"></i> <?= __('Suppliers') ?> (<?= count($suppliers) ?>)
        </a>
    </li>
</ul>

<!-- Search Bar for active tab -->
<div class="card shadow-sm border-0 mb-4">
    <div class="card-body p-3">
        <form method="get" action="<?= url('directory') ?>" class="row g-2 align-items-center">
            <input type="hidden" name="tab" value="<?= e($tab) ?>">
            <div class="col-md-5">
                <div class="input-group">
                    <span class="input-group-text bg-white border-end-0 text-muted"><i class="fa-solid fa-magnifying-glass"></i></span>
                    <input type="text" name="search" value="<?= e($search) ?>" class="form-control border-start-0 ps-0" placeholder="<?= $isAr ? 'بحث في هذا القسم...' : 'Search in current tab...' ?>">
                </div>
            </div>
            <div class="col-md-2">
                <button type="submit" class="btn btn-primary w-100"><?= __('Search') ?></button>
            </div>
            <?php if (!empty($search)): ?>
                <div class="col-md-2">
                    <a href="<?= url('directory', ['tab' => $tab]) ?>" class="btn btn-outline-secondary w-100"><?= __('Reset') ?></a>
                </div>
            <?php endif; ?>
        </form>
    </div>
</div>

<!-- Tab 1: Staff Users Table -->
<?php if ($tab === 'users'): ?>
    <div class="card shadow-sm border-0">
        <div class="card-body p-0">
            <div class="table-responsive">
                <table class="table table-hover align-middle mb-0">
                    <thead class="table-light small">
                        <tr>
                            <th>Staff Name</th>
                            <th>Role / Designation</th>
                            <th>Department</th>
                            <th>Contact Info</th>
                            <th>Assigned Assets</th>
                            <th>Active Tickets</th>
                            <th class="text-end"><?= __('Actions') ?></th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php foreach ($users as $u): ?>
                            <tr>
                                <td>
                                    <div class="fw-bold text-dark"><i class="fa-regular fa-user me-1 text-primary"></i> <?= e($u['fullName']) ?></div>
                                    <div class="text-muted" style="font-size: 0.72rem;">User ID: <?= $u['domainUser'] ?></div>
                                </td>
                                <td><span class="badge bg-primary-subtle text-primary border"><?= e($u['role']) ?></span></td>
                                <td><?= e($isAr && !empty($u['departmentNameAr']) ? $u['departmentNameAr'] : $u['departmentName']) ?></td>
                                <td>
                                    <div class="small text-muted"><i class="fa-regular fa-envelope me-1"></i> <?= e($u['email']) ?></div>
                                    <div class="small text-muted"><i class="fa-solid fa-phone me-1"></i> <?= e($u['phone']) ?></div>
                                </td>
                                <td><span class="badge bg-light text-dark border"><?= $u['custodyCount'] ?> assets</span></td>
                                <td>
                                    <?php if ($u['activeTicketCount'] > 0): ?>
                                        <span class="badge bg-danger rounded-pill"><?= $u['activeTicketCount'] ?></span>
                                    <?php else: ?>
                                        <span class="badge bg-light text-muted">0</span>
                                    <?php endif; ?>
                                </td>
                                <td class="text-end">
                                    <a href="<?= url('account/profile', ['id' => $u['id']]) ?>" class="btn btn-sm btn-outline-primary">
                                        View Profile
                                    </a>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
<?php endif; ?>

<!-- Tab 2: Hospital Rooms Table -->
<?php if ($tab === 'rooms'): ?>
    <div class="card shadow-sm border-0">
        <div class="card-body p-0">
            <div class="table-responsive">
                <table class="table table-hover align-middle mb-0">
                    <thead class="table-light small">
                        <tr>
                            <th>Room No</th>
                            <th>Room Name</th>
                            <th>Floor</th>
                            <th>Department</th>
                            <th>Deployed Equipment</th>
                            <th class="text-end"><?= __('Actions') ?></th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php foreach ($rooms as $r): ?>
                            <tr>
                                <td><span class="badge bg-light text-dark border font-monospace fs-6">Room <?= $r['roomNo'] ?></span></td>
                                <td class="fw-bold text-dark"><?= e($isAr && !empty($r['name_ar']) ? $r['name_ar'] : $r['name']) ?></td>
                                <td>Floor <?= $r['floor'] ?></td>
                                <td><span class="badge bg-light text-dark border"><?= e($isAr && !empty($r['departmentNameAr']) ? $r['departmentNameAr'] : $r['departmentName']) ?></span></td>
                                <td><span class="badge bg-primary rounded-pill"><?= $r['assetCount'] ?> devices</span></td>
                                <td class="text-end">
                                    <a href="<?= url('assets', ['roomId' => $r['id']]) ?>" class="btn btn-sm btn-outline-primary">
                                        View Devices &rarr;
                                    </a>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
<?php endif; ?>

<!-- Tab 3: Departments Table -->
<?php if ($tab === 'departments'): ?>
    <div class="card shadow-sm border-0">
        <div class="card-body p-0">
            <div class="table-responsive">
                <table class="table table-hover align-middle mb-0">
                    <thead class="table-light small">
                        <tr>
                            <th>Department</th>
                            <th>Department Head</th>
                            <th>Staff Count</th>
                            <th>Allocated Rooms</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php foreach ($departments as $d): ?>
                            <tr>
                                <td class="fw-bold text-dark fs-6"><?= e($isAr && !empty($d['name_ar']) ? $d['name_ar'] : $d['name']) ?></td>
                                <td><i class="fa-regular fa-user me-1 text-primary"></i> <?= e($d['headUserName'] ?: 'Not assigned') ?></td>
                                <td><span class="badge bg-light text-dark border"><?= $d['staffCount'] ?> staff</span></td>
                                <td><span class="badge bg-info-subtle text-info border"><?= $d['roomCount'] ?> clinics / rooms</span></td>
                            </tr>
                        <?php endforeach; ?>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
<?php endif; ?>

<!-- Tab 4: Suppliers Table -->
<?php if ($tab === 'suppliers'): ?>
    <div class="card shadow-sm border-0">
        <div class="card-body p-0">
            <div class="table-responsive">
                <table class="table table-hover align-middle mb-0">
                    <thead class="table-light small">
                        <tr>
                            <th>Supplier / Vendor</th>
                            <th>Primary Contact</th>
                            <th>Phone</th>
                            <th>Email</th>
                            <th>Supplied Assets</th>
                            <th>Active Dispatches</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php foreach ($suppliers as $s): ?>
                            <tr>
                                <td>
                                    <div class="fw-bold text-dark"><?= e($s['name']) ?></div>
                                    <div class="text-muted small"><?= e($s['address']) ?></div>
                                </td>
                                <td><?= e($s['contactPerson']) ?></td>
                                <td><?= e($s['phone']) ?></td>
                                <td><a href="mailto:<?= e($s['email']) ?>" class="text-decoration-none"><?= e($s['email']) ?></a></td>
                                <td><span class="badge bg-light text-dark border"><?= $s['suppliedAssetsCount'] ?> assets</span></td>
                                <td>
                                    <?php if ($s['activeRepairsCount'] > 0): ?>
                                        <span class="badge bg-warning text-dark"><?= $s['activeRepairsCount'] ?> in factory service</span>
                                    <?php else: ?>
                                        <span class="badge bg-light text-muted">0</span>
                                    <?php endif; ?>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
<?php endif; ?>

<?php
$content = ob_get_clean();
require __DIR__ . '/../layout.php';
?>
