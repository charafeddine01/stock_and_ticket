<?php
/**
 * SOLVIS LanguageController
 * Instant Bilingual Switcher (English / Arabic)
 */

require_once __DIR__ . '/../I18n.php';
require_once __DIR__ . '/../Helpers.php';

class LanguageController {
    public function setLanguage(): void {
        $lang = trim($_GET['culture'] ?? $_GET['lang'] ?? 'en');
        $returnUrl = $_GET['returnUrl'] ?? $_SERVER['HTTP_REFERER'] ?? url();

        if (in_array($lang, ['ar', 'en'])) {
            $_SESSION['solvis_lang'] = $lang;
            setcookie('stockportal_lang', $lang, time() + 31536000, '/');
        }

        header('Location: ' . $returnUrl);
        exit;
    }
}
