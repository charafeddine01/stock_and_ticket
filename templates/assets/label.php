<?php
/**
 * SOLVIS High-Definition Thermal Asset Label (50mm x 30mm standard)
 */
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <title>Thermal Label - <?= e($asset['hospitalNo']) ?></title>
    <style>
        @page {
            size: 58mm 40mm;
            margin: 0;
        }
        body {
            margin: 0;
            padding: 4mm;
            font-family: 'Helvetica Neue', Arial, sans-serif;
            background: #fff;
            color: #000;
            box-sizing: border-box;
            display: flex;
            flex-direction: column;
            justify-content: center;
            height: 100vh;
        }
        .label-container {
            width: 50mm;
            height: 32mm;
            border: 1px dashed #999;
            padding: 2mm;
            margin: auto;
            display: flex;
            flex-direction: column;
            justify-content: space-between;
            box-sizing: border-box;
            background: #fff;
        }
        .label-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            border-bottom: 1.5px solid #000;
            padding-bottom: 1mm;
        }
        .label-header .brand {
            font-size: 8pt;
            font-weight: 900;
            letter-spacing: 0.5px;
        }
        .label-header .dept {
            font-size: 6pt;
            font-weight: 700;
            text-transform: uppercase;
        }
        .label-body {
            display: flex;
            gap: 2mm;
            align-items: center;
            margin: 1mm 0;
        }
        .qr-box {
            width: 14mm;
            height: 14mm;
            flex-shrink: 0;
        }
        .qr-box img {
            width: 100%;
            height: 100%;
            object-fit: contain;
        }
        .info-box {
            flex: 1;
            overflow: hidden;
        }
        .tag-no {
            font-size: 10pt;
            font-weight: 900;
            font-family: monospace;
            line-height: 1.1;
        }
        .model {
            font-size: 7pt;
            font-weight: 700;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }
        .serial {
            font-size: 6pt;
            color: #333;
            font-family: monospace;
        }
        .location {
            font-size: 6pt;
            font-weight: 600;
            margin-top: 1px;
        }
        .label-footer {
            border-top: 1px solid #000;
            padding-top: 1mm;
            display: flex;
            justify-content: space-between;
            font-size: 5.5pt;
            font-weight: 600;
        }
        @media print {
            .no-print { display: none !important; }
            body { padding: 0; }
            .label-container { border: none; }
        }
    </style>
</head>
<body>
    <div class="no-print" style="text-align: center; margin-bottom: 10px;">
        <button onclick="window.print()" style="padding: 8px 16px; font-weight: bold; background: #0066cc; color: #fff; border: none; border-radius: 4px; cursor: pointer;">
            🖨️ Print Label (58mm x 40mm)
        </button>
        <button onclick="window.close()" style="padding: 8px 16px; margin-left: 8px; border: 1px solid #ccc; background: #fff; cursor: pointer;">
            Close
        </button>
    </div>

    <div class="label-container">
        <div class="label-header">
            <span class="brand">SOLVIS HEALTH</span>
            <span class="dept"><?= e($asset['departmentName'] ?? 'HOSPITAL ASSET') ?></span>
        </div>

        <div class="label-body">
            <div class="qr-box">
                <!-- Clean Dynamic QR Code Generator -->
                <img src="https://api.qrserver.com/v1/create-qr-code/?size=100x100&data=<?= urlencode(url('assets/details', ['id' => $asset['id']])) ?>" alt="QR Code" />
            </div>
            <div class="info-box">
                <div class="tag-no"><?= e($asset['hospitalNo']) ?></div>
                <div class="model"><?= e($asset['brand']) ?> <?= e($asset['modelNo']) ?></div>
                <div class="serial">S/N: <?= e($asset['serialNo'] ?: 'N/A') ?></div>
                <div class="location">Loc: Room <?= $asset['roomNo'] ?> (<?= e($asset['roomName']) ?>)</div>
            </div>
        </div>

        <div class="label-footer">
            <span>Custody: <?= e($asset['assignedUserName'] ?: 'General Custody') ?></span>
            <span>SEC-ID #<?= $asset['id'] ?></span>
        </div>
    </div>
</body>
</html>
