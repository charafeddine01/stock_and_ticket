<?php
/**
 * SOLVIS NotificationsController
 */

require_once __DIR__ . '/../Database.php';
require_once __DIR__ . '/../Auth.php';
require_once __DIR__ . '/../Helpers.php';

class NotificationsController {
    public function markAllAsRead(): void {
        $db = Database::getConnection();
        $uid = Auth::id();
        $db->prepare("UPDATE notifications SET isRead = 1 WHERE userID = ? OR userID = 1")->execute([$uid]);
        
        $returnUrl = $_SERVER['HTTP_REFERER'] ?? url();
        header('Location: ' . $returnUrl);
        exit;
    }
}
