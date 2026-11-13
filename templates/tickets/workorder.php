<?php
/**
 * SOLVIS Printable Field Service Work Order
 */
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <title>Work Order #<?= $ticket['id'] ?> - SOLVIS</title>
    <style>
        @page { size: A4; margin: 15mm; }
        body {
            font-family: Arial, sans-serif;
            color: #111;
            background: #fff;
            margin: 0;
            padding: 20px;
            font-size: 10pt;
        }
        .header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            border-bottom: 2px solid #000;
            padding-bottom: 12px;
            margin-bottom: 20px;
        }
        .logo-title {
            font-size: 18pt;
            font-weight: 900;
            letter-spacing: 1px;
        }
        .order-meta {
            text-align: right;
        }
        .grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 15px;
            margin-bottom: 20px;
        }
        .box {
            border: 1px solid #ccc;
            padding: 10px;
            border-radius: 4px;
        }
        .box-title {
            font-weight: bold;
            font-size: 9pt;
            text-transform: uppercase;
            color: #555;
            margin-bottom: 6px;
            border-bottom: 1px solid #eee;
            padding-bottom: 4px;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 15px;
            margin-bottom: 20px;
        }
        th, td {
            border: 1px solid #ddd;
            padding: 8px;
            text-align: left;
        }
        th {
            background: #f5f5f5;
            font-weight: bold;
        }
        .signatures {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 40px;
            margin-top: 50px;
        }
        .sig-line {
            border-top: 1px dashed #000;
            margin-top: 40px;
            padding-top: 5px;
            font-size: 9pt;
            text-align: center;
        }
        @media print {
            .no-print { display: none !important; }
            body { padding: 0; }
        }
    </style>
</head>
<body>
    <div class="no-print" style="margin-bottom: 20px; text-align: center;">
        <button onclick="window.print()" style="padding: 10px 20px; background: #0066cc; color: #fff; font-weight: bold; border: none; border-radius: 4px; cursor: pointer;">
            🖨️ Print Field Work Order
        </button>
        <button onclick="window.close()" style="padding: 10px 20px; margin-left: 10px; border: 1px solid #ccc; background: #fff; cursor: pointer;">
            Close Window
        </button>
    </div>

    <div class="header">
        <div>
            <div class="logo-title">SOLVIS HOSPITAL OPERATIONS</div>
            <div style="font-size: 9pt; color: #555;">Clinical Engineering &amp; Facility Helpdesk Work Order</div>
        </div>
        <div class="order-meta">
            <div style="font-size: 14pt; font-weight: bold; color: #0066cc;">WORK ORDER #<?= $ticket['id'] ?></div>
            <div>Date: <?= date('Y-m-d H:i') ?></div>
            <div>Priority: <strong><?= strtoupper($ticket['priority']) ?></strong></div>
        </div>
    </div>

    <div class="grid">
        <div class="box">
            <div class="box-title">Requester &amp; Location</div>
            <div><strong>Staff:</strong> <?= e($ticket['createdByName']) ?></div>
            <div><strong>Department:</strong> <?= e($ticket['departmentName']) ?></div>
            <div><strong>Phone:</strong> <?= e($ticket['creatorPhone'] ?: 'N/A') ?></div>
            <div><strong>Location:</strong> Room <?= $ticket['roomNo'] ?> (<?= e($ticket['roomName']) ?>)</div>
        </div>
        <div class="box">
            <div class="box-title">Assigned Engineering Technician</div>
            <div><strong>Technician:</strong> <?= e($ticket['assignedName'] ?: 'Unassigned / Field Dispatch') ?></div>
            <div><strong>Team:</strong> <?= e($ticket['maintenanceTeam']) ?> Engineering</div>
            <div><strong>Phone:</strong> <?= e($ticket['assignedPhone'] ?: 'N/A') ?></div>
            <div><strong>Status:</strong> <?= strtoupper($ticket['statusName']) ?></div>
        </div>
    </div>

    <div class="box" style="margin-bottom: 20px;">
        <div class="box-title">Hospital Equipment &amp; Problem Diagnosis</div>
        <div><strong>Asset Tag:</strong> <?= e($ticket['assetTag'] ?: 'General Facility') ?></div>
        <div><strong>Equipment:</strong> <?= e($ticket['assetBrand']) ?> <?= e($ticket['assetModel']) ?> (S/N: <?= e($ticket['assetSerial'] ?: 'N/A') ?>)</div>
        <div style="margin-top: 10px;"><strong>Issue Summary:</strong> <?= e($ticket['title']) ?></div>
        <div style="margin-top: 6px; padding: 10px; background: #fafafa; border: 1px solid #eee; font-family: monospace;">
            <?= nl2br(e($ticket['description'])) ?>
        </div>
    </div>

    <?php if (!empty($usedParts)): ?>
        <div class="box-title">Parts &amp; Components Fitted</div>
        <table>
            <thead>
                <tr>
                    <th>Part SKU</th>
                    <th>Part Description</th>
                    <th>Quantity</th>
                    <th>Unit Cost</th>
                </tr>
            </thead>
            <tbody>
                <?php foreach ($usedParts as $up): ?>
                    <tr>
                        <td><?= e($up['partNo']) ?></td>
                        <td><?= e($up['partName']) ?></td>
                        <td><?= abs($up['quantity']) ?></td>
                        <td><?= format_currency($up['unitCost']) ?></td>
                    </tr>
                <?php endforeach; ?>
            </tbody>
        </table>
    <?php endif; ?>

    <div class="box-title" style="margin-top: 30px;">Technician Clinical Testing &amp; Sign-off</div>
    <div style="min-height: 80px; border: 1px dashed #ccc; padding: 10px; font-size: 9pt; color: #777;">
        Technician Notes: Electrical safety test passed / Sensor calibration verified / Restored to operational clinical custody.
    </div>

    <div class="signatures">
        <div>
            <div class="sig-line">
                Lead Technician Signature &amp; Date
            </div>
        </div>
        <div>
            <div class="sig-line">
                Department Head / Nurse in Charge Acceptance
            </div>
        </div>
    </div>
</body>
</html>
