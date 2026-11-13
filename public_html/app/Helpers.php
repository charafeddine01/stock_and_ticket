<?php
/**
 * SOLVIS Core Helpers, URL Generators, and UI Renderers
 */

require_once __DIR__ . '/I18n.php';

function url(string $route = '', array $params = []): string {
    $base = rtrim(BASE_URL, '/');
    $route = trim($route, '/');
    
    // Check if Apache rewrite is used or fallback
    // We construct path-based URL by default: /base/route
    $url = $base . '/' . $route;
    
    if (!empty($params)) {
        $url .= (strpos($url, '?') === false ? '?' : '&') . http_build_query($params);
    }
    
    return $url === '' ? '/' : $url;
}

function asset_url(string $path): string {
    $base = rtrim(BASE_URL, '/');
    $path = ltrim($path, '/');
    return $base . '/' . $path;
}

function e(?string $value): string {
    return htmlspecialchars($value ?? '', ENT_QUOTES, 'UTF-8');
}

function format_currency(float|int|string|null $amount): string {
    $val = (float)($amount ?? 0);
    return '$' . number_format($val, 2);
}

function format_date(?string $date, bool $includeTime = false): string {
    if (empty($date)) return '-';
    $ts = strtotime($date);
    if (!$ts) return '-';
    return date($includeTime ? 'M d, Y H:i' : 'M d, Y', $ts);
}

function flash(string $type, string $message): void {
    if (!isset($_SESSION['solvis_flash'])) {
        $_SESSION['solvis_flash'] = [];
    }
    $_SESSION['solvis_flash'][$type] = $message;
}

function get_flash(string $type): ?string {
    if (isset($_SESSION['solvis_flash'][$type])) {
        $msg = $_SESSION['solvis_flash'][$type];
        unset($_SESSION['solvis_flash'][$type]);
        return $msg;
    }
    return null;
}

function render_ticket_status_badge(int|string|null $status): string {
    $str = is_numeric($status) ? match ((int)$status) {
        1 => 'New',
        2 => 'In Progress',
        3 => 'Pending Spare Parts',
        4 => 'Resolved',
        5 => 'Closed',
        6 => 'Under External Maintenance',
        default => 'New'
    } : (string)$status;

    $class = match (strtolower(trim($str))) {
        'new' => 'bg-primary',
        'in progress' => 'bg-info text-dark',
        'pending spare parts', 'pending parts' => 'bg-warning text-dark',
        'resolved' => 'bg-success',
        'closed' => 'bg-secondary',
        'under external maintenance', 'maintenance' => 'bg-danger',
        default => 'bg-secondary'
    };

    $label = I18n::isArabic() ? match (strtolower(trim($str))) {
        'new' => 'جديدة',
        'in progress' => 'قيد المعالجة',
        'pending spare parts', 'pending parts' => 'بانتظار قطع الغيار',
        'resolved' => 'تم الحل والإصلاح',
        'closed' => 'مغلقة',
        'under external maintenance', 'maintenance' => 'صيانة خارجية',
        default => $str
    } : $str;

    return sprintf('<span class="badge %s px-2 py-1 rounded-pill">%s</span>', $class, e($label));
}

function render_ticket_priority_badge(string $priority): string {
    $p = strtolower(trim($priority));
    $class = match ($p) {
        'urgent', 'fire' => 'badge-priority-urgent',
        'high' => 'badge-priority-high',
        'medium' => 'badge-priority-medium',
        'low' => 'badge-priority-low',
        default => 'badge-priority-medium'
    };

    $label = I18n::isArabic() ? match ($p) {
        'urgent', 'fire' => 'طارئة وعاجلة',
        'high' => 'مرتفعة',
        'medium' => 'متوسطة',
        'low' => 'منخفضة',
        default => $priority
    } : ucfirst($priority);

    $icon = match ($p) {
        'urgent', 'fire' => 'fa-fire-flame-curved fa-fade text-danger',
        'high' => 'fa-arrow-trend-up text-danger',
        'medium' => 'fa-equals text-warning',
        'low' => 'fa-arrow-down text-info',
        default => 'fa-circle-dot'
    };

    return sprintf(
        '<span class="badge %s d-inline-flex align-items-center gap-1 px-2 py-1"><i class="fa-solid %s"></i> %s</span>',
        $class,
        $icon,
        e($label)
    );
}

function render_asset_status_badge(string $status): string {
    $s = strtolower(trim($status));
    $class = match ($s) {
        'in service' => 'bg-success',
        'in stock' => 'bg-primary',
        'under maintenance' => 'bg-warning text-dark',
        'retired' => 'bg-secondary',
        default => 'bg-info'
    };

    $label = I18n::isArabic() ? match ($s) {
        'in service' => 'في الخدمة',
        'in stock' => 'في المستودع',
        'under maintenance' => 'قيد الصيانة',
        'retired' => 'مكهن ومخرج',
        default => $status
    } : $status;

    return sprintf('<span class="badge %s px-2 py-1 rounded-pill shadow-xs">%s</span>', $class, e($label));
}
