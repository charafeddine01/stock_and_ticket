<?php
/**
 * SOLVIS Internationalization & Localization Engine
 * Supports seamless bilingual switching (English & Arabic) with RTL/LTR layouts
 */

class I18n {
    private static ?string $currentLang = null;

    private static array $translations = [
        // App Branding & Navigation
        "SOLVIS" => ["ar" => "سولفيس", "en" => "SOLVIS"],
        "SOLVIS Portal" => ["ar" => "بوابة سولفيس", "en" => "SOLVIS Portal"],
        "SOLVIS - Hospital Operations & Asset Intelligence" => [
            "ar" => "سولفيس - بوابة إدارة العمليات والعهد الطبية",
            "en" => "SOLVIS - Hospital Operations & Asset Intelligence"
        ],
        "Hospital Operations & Asset Intelligence" => [
            "ar" => "إدارة العمليات والعهد الطبية",
            "en" => "Hospital Operations & Asset Intelligence"
        ],
        "Executive IT & Maintenance Dashboard" => [
            "ar" => "لوحة التحكم والتحليلات التنفيذية",
            "en" => "Executive IT & Maintenance Dashboard"
        ],
        "Dashboard" => ["ar" => "لوحة التحكم", "en" => "Dashboard"],
        "Executive Overview" => ["ar" => "نظرة عامة تنفيذية", "en" => "Executive Overview"],
        "Maintenance Team Analytics" => ["ar" => "تحليلات فرق الصيانة", "en" => "Maintenance Team Analytics"],
        "Stock & Assets" => ["ar" => "المخزون والعهد", "en" => "Stock & Assets"],
        "Hospital Assets" => ["ar" => "أصول ومعدات المستشفى", "en" => "Hospital Assets"],
        "My Assigned Equipment" => ["ar" => "أجهزتي وعهدتي المسجلة", "en" => "My Assigned Equipment"],
        "Tickets & Helpdesk" => ["ar" => "التذاكر والدعم الفني", "en" => "Tickets & Helpdesk"],
        "Helpdesk & Support Tickets" => ["ar" => "تذاكر الدعم الفني والصيانة", "en" => "Helpdesk & Support Tickets"],
        "My Service Tickets" => ["ar" => "تذاكر الدعم الخاصة بي", "en" => "My Service Tickets"],
        "Scheduled & Preventive Maintenance" => ["ar" => "الصيانة الوقائية والجدولة", "en" => "Scheduled & Preventive Maintenance"],
        "Preventive Maintenance" => ["ar" => "الصيانة الوقائية", "en" => "Preventive Maintenance"],
        "Reserve Stock & Spare Parts" => ["ar" => "قطع الغيار والمخزون الاحتياطي", "en" => "Reserve Stock & Spare Parts"],
        "Spare Parts" => ["ar" => "قطع الغيار", "en" => "Spare Parts"],
        "Hospital Directory" => ["ar" => "دليل المستشفى", "en" => "Hospital Directory"],
        "Directory" => ["ar" => "الدليل", "en" => "Directory"],
        "Staff Directory" => ["ar" => "دليل الموظفين", "en" => "Staff Directory"],
        "Hospital Rooms" => ["ar" => "الغرف والأقسام", "en" => "Hospital Rooms"],
        "Departments" => ["ar" => "الأقسام الطبية والإدارية", "en" => "Departments"],
        "Suppliers" => ["ar" => "الموردون والشركات", "en" => "Suppliers"],
        "My Profile" => ["ar" => "الملف الشخصي", "en" => "My Profile"],
        "Switch Persona" => ["ar" => "تبديل الهوية للاختبار", "en" => "Switch Persona"],
        "Testing Profile Switcher" => ["ar" => "محاكي تبديل الأدوار", "en" => "Testing Profile Switcher"],
        "Auto-hide" => ["ar" => "إخفاء تلقائي", "en" => "Auto-hide"],
        "Pin Sidebar" => ["ar" => "تثبيت الشريط الجانبي", "en" => "Pin Sidebar"],
        "Close Sidebar" => ["ar" => "إغلاق الشريط", "en" => "Close Sidebar"],
        "Search" => ["ar" => "بحث", "en" => "Search"],
        "Filter" => ["ar" => "تصفية", "en" => "Filter"],
        "Reset" => ["ar" => "إعادة ضبط", "en" => "Reset"],
        "Export CSV" => ["ar" => "تصدير CSV", "en" => "Export CSV"],

        // KPI & Dashboard
        "Total Assets" => ["ar" => "إجمالي العهد والأجهزة", "en" => "Total Assets"],
        "in service" => ["ar" => "في الخدمة", "en" => "in service"],
        "in repair" => ["ar" => "قيد الصيانة", "en" => "in repair"],
        "Total Asset Value" => ["ar" => "إجمالي القيمة التقديرية", "en" => "Total Asset Value"],
        "Open Helpdesk Tickets" => ["ar" => "التذاكر المفتوحة", "en" => "Open Helpdesk Tickets"],
        "requires action" => ["ar" => "تتطلب إجراءً", "en" => "requires action"],
        "urgent fire priority" => ["ar" => "أولوية طارئة قصوى", "en" => "urgent fire priority"],
        "Avg SLA Resolution" => ["ar" => "متوسط وقت الحل", "en" => "Avg SLA Resolution"],
        "Active Dispatches" => ["ar" => "أجهزة لدى الصيانة الخارجية", "en" => "Active Dispatches"],
        "at outside vendors" => ["ar" => "لدى موردين خارجيين", "en" => "at outside vendors"],
        "overdue inspections" => ["ar" => "فحوصات متأخرة", "en" => "overdue inspections"],
        "Expiring Warranties" => ["ar" => "ضمانات تنتهي قريباً", "en" => "Expiring Warranties"],
        "within 30 days" => ["ar" => "خلال 30 يوماً", "en" => "within 30 days"],
        "Department Filter:" => ["ar" => "تصفية حسب القسم:", "en" => "Department Filter:"],
        "All Departments" => ["ar" => "كافة الأقسام", "en" => "All Departments"],
        "IT" => ["ar" => "تقنية المعلومات (IT)", "en" => "IT"],
        "Biomedical" => ["ar" => "الهندسة الطبية الحيوية", "en" => "Biomedical"],
        "General" => ["ar" => "الصيانة العامة والمرافق", "en" => "General"],
        "Recent Urgent Tickets" => ["ar" => "التذاكر الطارئة الأخيرة", "en" => "Recent Urgent Tickets"],
        "Overdue Preventive Inspections" => ["ar" => "فحوصات الصيانة الوقائية المتأخرة", "en" => "Overdue Preventive Inspections"],
        "Recent Asset Activity" => ["ar" => "آخر حركات وسجلات العهد", "en" => "Recent Asset Activity"],
        "System Health" => ["ar" => "حالة وتكامل النظام", "en" => "System Health"],
        "Live Production Online" => ["ar" => "النظام متصل ونشط", "en" => "Live Production Online"],

        // Assets
        "Add New Asset" => ["ar" => "إضافة أصل / جهاز جديد", "en" => "Add New Asset"],
        "Asset Tag" => ["ar" => "رقم العهدة (Asset Tag)", "en" => "Asset Tag"],
        "Serial Number" => ["ar" => "الرقم التسلسلي (S/N)", "en" => "Serial Number"],
        "Brand" => ["ar" => "الشركة المصنعة", "en" => "Brand"],
        "Model" => ["ar" => "الموديل / الطراز", "en" => "Model"],
        "Category" => ["ar" => "فئة الأصل", "en" => "Category"],
        "Type" => ["ar" => "النوع", "en" => "Type"],
        "Status" => ["ar" => "الحالة التشغيلية", "en" => "Status"],
        "Assigned User" => ["ar" => "المستلم / في عهدة", "en" => "Assigned User"],
        "Location / Room" => ["ar" => "الموقع / الغرفة", "en" => "Location / Room"],
        "Purchase Date" => ["ar" => "تاريخ الشراء", "en" => "Purchase Date"],
        "Purchase Price" => ["ar" => "تكلفة الشراء", "en" => "Purchase Price"],
        "Warranty Expiry" => ["ar" => "انتهاء الضمان", "en" => "Warranty Expiry"],
        "Next Calibration" => ["ar" => "المعايرة القادمة", "en" => "Next Calibration"],
        "Print Thermal Label" => ["ar" => "طباعة ملصق الباركود الحراري", "en" => "Print Thermal Label"],
        "Edit Asset" => ["ar" => "تعديل بيانات الأصل", "en" => "Edit Asset"],
        "Asset Details" => ["ar" => "تفاصيل الأصل والعهدة", "en" => "Asset Details"],
        "Custody History" => ["ar" => "سجل حركة العهدة والتحويلات", "en" => "Custody History"],
        "Maintenance Log" => ["ar" => "سجل الصيانة والإصلاحات", "en" => "Maintenance Log"],
        "All Statuses" => ["ar" => "كافة الحالات", "en" => "All Statuses"],
        "In Service" => ["ar" => "في الخدمة والعمل", "en" => "In Service"],
        "In Stock" => ["ar" => "في المستودع (احتياطي)", "en" => "In Stock"],
        "Under Maintenance" => ["ar" => "قيد الصيانة والإصلاح", "en" => "Under Maintenance"],
        "Retired" => ["ar" => "تم التكهين والإخراج", "en" => "Retired"],

        // Tickets
        "Open New Ticket" => ["ar" => "فتح تذكرة صيانة جديدة", "en" => "Open New Ticket"],
        "All Tickets" => ["ar" => "كافة التذاكر", "en" => "All Tickets"],
        "My Assigned Tickets" => ["ar" => "التذاكر المسندة إليّ", "en" => "My Assigned Tickets"],
        "Unassigned" => ["ar" => "غير مسندة بعد", "en" => "Unassigned"],
        "Pending Parts" => ["ar" => "بانتظار قطع الغيار", "en" => "Pending Parts"],
        "Urgent" => ["ar" => "طارئة وعاجلة", "en" => "Urgent"],
        "Ticket #" => ["ar" => "تذكرة رقم", "en" => "Ticket #"],
        "Priority" => ["ar" => "الأولوية", "en" => "Priority"],
        "High" => ["ar" => "مرتفعة", "en" => "High"],
        "Medium" => ["ar" => "متوسطة", "en" => "Medium"],
        "Low" => ["ar" => "منخفضة", "en" => "Low"],
        "Assigned Technician" => ["ar" => "الفني المسؤول", "en" => "Assigned Technician"],
        "Created By" => ["ar" => "مقدم البلاغ", "en" => "Created By"],
        "Created Date" => ["ar" => "تاريخ الفتح", "en" => "Created Date"],
        "Discussion Timeline" => ["ar" => "سجل الملاحظات والتشخيص", "en" => "Discussion Timeline"],
        "Post Update / Diagnostic" => ["ar" => "إضافة ملاحظة أو تحديث فني", "en" => "Post Update / Diagnostic"],
        "Print Work Order" => ["ar" => "طباعة أمر العمل الميداني", "en" => "Print Work Order"],
        "Close Ticket" => ["ar" => "إغلاق التذكرة", "en" => "Close Ticket"],
        "Log Spare Part Usage" => ["ar" => "تسجيل استهلاك قطع غيار", "en" => "Log Spare Part Usage"],
        "Used Spare Parts" => ["ar" => "قطع الغيار المستهلكة في التذكرة", "en" => "Used Spare Parts"],
        "No spare parts used yet for this ticket." => [
            "ar" => "لم يتم تسجيل استهلاك قطع غيار لهذه التذكرة بعد.",
            "en" => "No spare parts used yet for this ticket."
        ],

        // Spare Parts
        "Add Reserve Item" => ["ar" => "إضافة صنف للمخزون الاحتياطي", "en" => "Add Reserve Item"],
        "Restock Item" => ["ar" => "توريد وإعادة تزويد", "en" => "Restock Item"],
        "Stock Transactions" => ["ar" => "سجل حركات المخزون", "en" => "Stock Transactions"],
        "Quantity in Stock" => ["ar" => "الكمية بالمخزون", "en" => "Quantity in Stock"],
        "Minimum Reserve Level" => ["ar" => "الحد الأدنى للاحتياط", "en" => "Minimum Reserve Level"],
        "Unit Cost" => ["ar" => "تكلفة الوحدة", "en" => "Unit Cost"],
        "Shelf / Location" => ["ar" => "مكان التخزين / الرف", "en" => "Shelf / Location"],
        "Low Reserve Alerts" => ["ar" => "تنبيهات نقص المخزون", "en" => "Low Reserve Alerts"],
        "Commission as Asset" => ["ar" => "تحويل وتخصيص كأصل مستقل", "en" => "Commission as Asset"],

        // Maintenance
        "Send to Maintenance" => ["ar" => "إرسال للصيانة الخارجية", "en" => "Send to Maintenance"],
        "At Supplier (Maint.)" => ["ar" => "لدى المورد (صيانة)", "en" => "At Supplier (Maint.)"],
        "Overdue Inspections" => ["ar" => "فحوصات متأخرة", "en" => "Overdue Inspections"],
        "Expiring in 30 Days" => ["ar" => "تستحق خلال 30 يوم", "en" => "Expiring in 30 Days"],
        "Valid / Calibrated" => ["ar" => "معايرة صالحة وسارية", "en" => "Valid / Calibrated"],
        "Dispatch to External Vendor" => ["ar" => "إرسال إلى شركة خارجية", "en" => "Dispatch to External Vendor"],
        "Record Preventive Inspection" => ["ar" => "تسجيل فحص وقائي دوري", "en" => "Record Preventive Inspection"],
        "Return to Hospital" => ["ar" => "استرجاع إلى المستشفى", "en" => "Return to Hospital"],

        // General UI & Notifications
        "Actions" => ["ar" => "الإجراءات", "en" => "Actions"],
        "Details" => ["ar" => "التفاصيل", "en" => "Details"],
        "Edit" => ["ar" => "تعديل", "en" => "Edit"],
        "Delete" => ["ar" => "حذف", "en" => "Delete"],
        "Save" => ["ar" => "حفظ التغييرات", "en" => "Save Changes"],
        "Cancel" => ["ar" => "إلغاء", "en" => "Cancel"],
        "Submit" => ["ar" => "إرسال", "en" => "Submit"],
        "Close" => ["ar" => "إغلاق", "en" => "Close"],
        "Success" => ["ar" => "تم بنجاح", "en" => "Success"],
        "Error" => ["ar" => "حدث خطأ", "en" => "Error"],
        "English" => ["ar" => "English", "en" => "English"],
        "Arabic" => ["ar" => "العربية", "en" => "Arabic"],
        "Light" => ["ar" => "الوضع الفاتح", "en" => "Light Theme"],
        "Dark" => ["ar" => "الوضع الداكن (OLED)", "en" => "Dark Theme"],
        "Mint" => ["ar" => "النمط الطبي (Clinical Mint)", "en" => "Clinical Mint Theme"]
    ];

    public static function init(): void {
        if (self::$currentLang !== null) return;

        // Check query param, cookie, or session
        if (isset($_GET['lang']) && in_array($_GET['lang'], ['ar', 'en'])) {
            self::$currentLang = $_GET['lang'];
            $_SESSION['solvis_lang'] = self::$currentLang;
            setcookie('stockportal_lang', self::$currentLang, time() + 31536000, '/');
        } elseif (!empty($_SESSION['solvis_lang'])) {
            self::$currentLang = $_SESSION['solvis_lang'];
        } elseif (!empty($_COOKIE['stockportal_lang'])) {
            self::$currentLang = $_COOKIE['stockportal_lang'];
        } else {
            self::$currentLang = 'en'; // Default
        }
    }

    public static function getLang(): string {
        self::init();
        return self::$currentLang ?? 'en';
    }

    public static function isArabic(): bool {
        return self::getLang() === 'ar';
    }

    public static function getDirection(): string {
        return self::isArabic() ? 'rtl' : 'ltr';
    }

    public static function get(string $key, array $replace = []): string {
        self::init();
        $lang = self::getLang();

        $text = self::$translations[$key][$lang] ?? self::$translations[$key]['en'] ?? $key;

        foreach ($replace as $placeholder => $value) {
            $text = str_replace(':' . $placeholder, (string)$value, $text);
        }

        return $text;
    }

    public static function targetTeam(string $team): string {
        if (self::isArabic()) {
            return match (strtoupper($team)) {
                'IT' => 'تقنية المعلومات',
                'BIOMEDICAL' => 'الهندسة الطبية',
                'GENERAL' => 'الصيانة العامة',
                default => 'كافة الأقسام'
            };
        }
        return match (strtoupper($team)) {
            'IT' => 'IT & Systems',
            'BIOMEDICAL' => 'Biomedical Engineering',
            'GENERAL' => 'General Maintenance',
            default => 'All Teams'
        };
    }
}

// Global translation shortcut
function __(string $key, array $replace = []): string {
    return I18n::get($key, $replace);
}
