using System;
using System.Collections.Generic;
using System.Globalization;

namespace StockPortal.Services
{
    public class LocalizationService : ILocalizationService
    {
        private static readonly Dictionary<string, string> ArTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            // App & Layout
            ["Stock & Helpdesk"] = "المخزون والدعم الفني",
            ["IT Asset & Service Portal"] = "بوابة إدارة الأصول والخدمات التقنية",
            ["IT Stock & Helpdesk Portal"] = "بوابة إدارة العهد والدعم الفني للمستشفى",
            ["Operations"] = "العمليات والتشغيل",
            ["Engineering Queue"] = "قائمة مهام الهندسة والصيانة",
            ["My Workspace"] = "مساحة عملي",
            ["Administration"] = "الإدارة والإعدادات",
            ["Account"] = "الحساب",
            ["Dashboard"] = "لوحة التحكم",
            ["Notifications"] = "الإشعارات",
            ["Stock & Assets"] = "المخزون والعهد",
            ["Hospital Assets"] = "أصول ومعدات المستشفى",
            ["My Equipment"] = "أجهزتي وعهدتي",
            ["Tickets & Helpdesk"] = "التذاكر والدعم الفني",
            ["My Service Tickets"] = "تذاكر الدعم الخاصة بي",
            ["Open New Ticket"] = "فتح تذكرة جديدة",
            ["Maintenance"] = "الصيانة الدورية",
            ["Maintenance Logs"] = "سجلات الصيانة",
            ["Device Forms & Templates"] = "نماذج ومواصفات الأجهزة",
            ["Device Forms & Specs"] = "نماذج الأجهزة والمواصفات",
            ["Master Directory & RBAC"] = "الدليل الشامل والصلاحيات",
            ["My Domain Profile"] = "ملفي الشخصي والجهاز",
            ["Windows Domain SSO"] = "الدخول الموحد لشبكة ويندوز",
            ["Switch User (Testing)"] = "تبديل الحساب (للاختبار)",
            ["My Profile & Equipment"] = "ملفي الشخصي ومعداتي",
            ["New Ticket"] = "تذكرة جديدة",
            ["Add Asset"] = "إضافة أصل",
            ["Auto-hide"] = "إخفاء تلقائي",
            ["Auto-hide Sidebar"] = "إخفاء تلقائي للشريط الجانبي",
            ["Pin Sidebar"] = "تثبيت الشريط الجانبي",
            ["Close Sidebar"] = "إغلاق القائمة",
            ["Toggle Navigation"] = "تبديل القائمة",
            ["Stock & Helpdesk Management System"] = "نظام إدارة العهد وتذاكر الدعم الفني",
            ["Windows Domain Authentication Active"] = "المصادقة عبر نطاق ويندوز مفعلة",
            ["Host"] = "المضيف",

            // Common Actions
            ["Search"] = "بحث",
            ["Filter"] = "تصفية",
            ["Reset"] = "إعادة تعيين",
            ["Reset Filters"] = "إعادة ضبط التصفية",
            ["Save"] = "حفظ",
            ["Save Changes"] = "حفظ التغييرات",
            ["Cancel"] = "إلغاء",
            ["Back"] = "رجوع",
            ["Back to List"] = "العودة إلى القائمة",
            ["Edit"] = "تعديل",
            ["Delete"] = "حذف",
            ["View Details"] = "عرض التفاصيل",
            ["Actions"] = "الإجراءات",
            ["Submit"] = "إرسال",
            ["Create"] = "إنشاء",
            ["Register New Asset"] = "تسجيل أصل جديد",
            ["Export CSV"] = "تصدير كملف CSV",
            ["Print"] = "طباعة",
            ["Print Tag"] = "طباعة الملصق",
            ["Print Work Order"] = "طباعة أمر العمل",
            ["Print Ticket"] = "طباعة التذكرة",
            ["Dismiss"] = "إغلاق",
            ["Mark All as Read"] = "تحديد الكل كمقروء",
            ["Mark as Read"] = "تحديد كمقروء",
            ["Preview Top Popup"] = "معاينة الإشعار العلوي",
            ["Close"] = "إغلاق",
            ["Close Ticket"] = "إغلاق التذكرة",
            ["Assign"] = "تعيين",
            ["Assign Technician"] = "تعيين فني",
            ["Transfer Custody"] = "نقل العهدة",
            ["Send to Maintenance"] = "إرسال للصيانة",
            ["Return from Maintenance"] = "استلام من الصيانة",
            ["Post Update"] = "نشر التحديث",
            ["Post Reply"] = "إرسال رد",

            // Dashboard
            ["Executive IT & Stock Dashboard"] = "لوحة التحكم التنفيذية للعهد والتقنية",
            ["Total Assets"] = "إجمالي الأصول والعهد",
            ["in service"] = "في الخدمة",
            ["in repair"] = "قيد الإصلاح",
            ["Total Asset Value"] = "القيمة الإجمالية للأصول",
            ["Open & Active Tickets"] = "التذاكر المفتوحة والنشطة",
            ["unassigned"] = "غير معينة",
            ["in progress"] = "قيد الإجراء",
            ["Closed Resolved"] = "تم حلها وإغلاقها",
            ["Maintenance Alerts"] = "تنبيهات الصيانة الوقائية",
            ["Overdue Inspections"] = "فحوصات دورية متأخرة",
            ["All Inspections Current"] = "جميع الفحوصات منتظمة",
            ["Requires Immediate Service"] = "يتطلب تدخلاً عاجلاً",
            ["Scheduled Maintenance Up-to-date"] = "جدول الصيانة محدث بالكامل",
            ["Expiring Warranties"] = "ضمانات تنتهي قريباً",
            ["Expiring in 30 Days"] = "تنتهي خلال 30 يوماً",
            ["Assets Near Expiration"] = "أصول شارف ضمانها على الانتهاء",
            ["Active Hospital Tickets Queue"] = "قائمة تذاكر المستشفى النشطة",
            ["Assets by Department & Category"] = "توزيع الأصول حسب الأقسام والفئات",
            ["Recent Asset Audit History"] = "سجل حركة ونقل الأصول الأخير",
            ["Quick Action Center"] = "مركز الإجراءات السريعة",
            ["Schedule Preventive Maintenance"] = "جدولة صيانة وقائية",
            ["Manage Device Forms"] = "إدارة نماذج الأجهزة",
            ["Hospital Directory & Users"] = "دليل المستشفى والمستخدمين",

            // Assets & Stock
            ["Stock & IT Asset Inventory"] = "سجل الأصول والعهد التقنية",
            ["My Assigned Equipment"] = "أجهزتي ومعداتي المسندة",
            ["My Custody Equipment"] = "العهدة في عهدتي الشخصية",
            ["Inventory Assets List"] = "قائمة الأصول المسجلة",
            ["Showing"] = "عرض",
            ["registered items"] = "أصل مسجل",
            ["Asset Tag / Hospital No"] = "رقم الأصل / كود المستشفى",
            ["Asset Tag"] = "رقم الأصل",
            ["Brand & Model"] = "العلامة والموديل",
            ["Brand / Manufacturer"] = "العلامة / الشركة المصنعة",
            ["Model / Version"] = "الموديل / الإصدار",
            ["Equipment Type"] = "نوع الجهاز / الأصل",
            ["Type"] = "النوع",
            ["Location / Room"] = "الموقع / الغرفة",
            ["Location"] = "الموقع",
            ["Floor"] = "الطابق",
            ["Room Number"] = "رقم الغرفة",
            ["Assigned Custodian"] = "المسؤول عن العهدة",
            ["Custodian"] = "المسؤول",
            ["Purchase Price"] = "سعر الشراء",
            ["Serial Number"] = "الرقم التسلسلي (Serial No)",
            ["Operational Status"] = "الحالة التشغيلية",
            ["Warranty Expiration"] = "انتهاء فترة الضمان",
            ["Purchase Date"] = "تاريخ الشراء",
            ["Technical Specifications"] = "المواصفات الفنية",
            ["Requires Periodic Maintenance"] = "يتطلب صيانة دورية وقائية",
            ["Interval (Days)"] = "الفترة الدورية (بالأيام)",
            ["Last Periodic Check"] = "آخر فحص دوري",
            ["Next Periodic Due"] = "موعد الفحص القادم",
            ["Custody History"] = "سجل حركة العهدة",
            ["Decommission Asset"] = "إسقاط / إتلاف الأصل",

            // Tickets & Helpdesk
            ["Service & Support Tickets"] = "تذاكر الدعم الفني والخدمات",
            ["Helpdesk & Support Tickets"] = "تذاكر المساعدة والدعم الفني",
            ["Manage service tickets, technician dispatches, and issue resolutions across teams"] = "إدارة بلاغات الأعطال، وتوجيه الفنيين، ومتابعة حلول التذاكر",
            ["All Tickets"] = "جميع التذاكر",
            ["My Assigned Tickets"] = "التذاكر المعينة لي",
            ["Opened by Me"] = "التذاكر التي فتحتها",
            ["Unassigned"] = "تذاكر غير معينة",
            ["In Maintenance"] = "قيد الصيانة",
            ["All Statuses"] = "جميع الحالات",
            ["All Priorities"] = "جميع الأولويات",
            ["All Equipment Types"] = "جميع أنواع الأجهزة",
            ["All Locations"] = "جميع المواقع",
            ["All Warranties"] = "جميع الضمانات",
            ["Issue Category"] = "فئة البلاغ / العطل",
            ["Priority Level"] = "مستوى الأولوية",
            ["Target Maintenance Team"] = "فريق الصيانة المختص",
            ["Assigned Technician"] = "الفني المسؤول",
            ["Resolution Details"] = "تفاصيل وتوثيق الحل",
            ["Date Opened"] = "تاريخ البلاغ",
            ["Date Closed"] = "تاريخ الإغلاق",
            ["Diagnostic Discussion & Updates"] = "المحادثة التشخيصية وتحديثات الفنيين",
            ["Write a message or diagnostic update..."] = "اكتب رسالة أو ملاحظة تشخيصية...",
            ["Post Diagnostic Note"] = "إرسال التحديث التشخيصي",

            // Maintenance & Work Orders
            ["Scheduled & Preventive Maintenance"] = "برنامج الصيانة الدورية والوقائية",
            ["Maintenance Work Orders"] = "أوامر عمل الصيانة",
            ["Inspection Schedule"] = "جدول الفحوصات الدورية",
            ["External Maintenance Log"] = "سجل الصيانة الخارجية والموردين",
            ["Sent to Maintenance Date"] = "تاريخ الإرسال للصيانة",
            ["Expected Return Date"] = "تاريخ الإعادة المتوقع",
            ["Work Summary / Actions Taken"] = "ملخص الأعمال والإجراءات المنفذة",
            ["Cost"] = "التكلفة الإجمالية",
            ["Replaced Parts & Components"] = "قطع الغيار والمكونات المستبدلة",
            ["Part Name"] = "اسم القطعة",
            ["Part Number"] = "رقم القطعة",
            ["Quantity"] = "الكمية",
            ["Under Warranty"] = "مشمولة بالضمان",

            // Master Directory
            ["Master Directory & Access Control"] = "الدليل الشامل وإدارة الوصول",
            ["Hospital Staff & Users"] = "موظفو ومستخدمو المستشفى",
            ["Hospital Rooms & Floors"] = "الغرف والأقسام المكانية",
            ["Approved Suppliers & Vendors"] = "الموردون المعتمدون والشركات",
            ["Hospital Departments"] = "أقسام ومراكز المستشفى",
            ["Full Name"] = "الاسم بالكامل",
            ["Username / Domain"] = "اسم المستخدم / حساب النطاق",
            ["Role / Title"] = "الدور الوظيفي / المسمى",
            ["Department Name"] = "اسم القسم",
            ["Department Head"] = "رئيس القسم",
            ["Supplier Name"] = "اسم المورد / الشركة",
            ["Supplier Type"] = "نوع المورد",
            ["Supported Departments"] = "الأقسام المعتمدة",
            ["Active Status"] = "حالة التفعيل",
            ["Active"] = "نشط",
            ["Inactive"] = "معطل",

            // Calibration (Task 2)
            ["Periodic Calibration"] = "المعايرة الدورية والقياس",
            ["Periodic Calibration & Metrology"] = "المعايرة الدورية والقياس الطبي",
            ["Periodic Calibration Status"] = "حالة وتاريخ المعايرة الدورية",
            ["Requires Periodic Calibration"] = "يتطلب معايرة دورية",
            ["Requires Calibration"] = "تتطلب معايرة",
            ["All Calibration"] = "جميع حالات المعايرة",
            ["Calibration Overdue"] = "معايرة متأخرة",
            ["Calibrated & Valid"] = "معايرة صالحة ومعتمدة",
            ["Due in 30 Days"] = "تستحق خلال 30 يوم",
            ["Log / Renew Calibration"] = "تسجيل / تجديد شهادة المعايرة",
            ["Calibration Certificate / Protocol No"] = "رقم شهادة المعايرة / المحضر",
            ["Calibrated By / Certified Body"] = "الجهة المعتمدة للمعايرة",
            ["Calibration Standards / Verification Notes"] = "معايير وملاحظات المعايرة",
            ["Calibration Interval"] = "فترة المعايرة",
            ["Last Calibration Date"] = "تاريخ آخر معايرة",
            ["Next Calibration Due"] = "تاريخ استحقاق المعايرة القادمة",
            ["Record Calibration Verification"] = "تسجيل محضر فحص ومعايرة الأصل",

            // Theme Selector & Clarity UI (Task 4)
            ["Change UI Theme"] = "تغيير مظهر وسمة النظام",
            ["Select Theme"] = "اختر سمة الألوان",
            ["Select Color Theme"] = "اختر سمة وتصميم الواجهة",
            ["Clarity Pro"] = "كلاريتي لايت (واضح)",
            ["Clarity Pro (Clean Light)"] = "كلاريتي لايت (أبيض وواضح)",
            ["Clinical Healthcare"] = "الرعاية الطبية (مينت)",
            ["Clinical Healthcare (Mint)"] = "الرعاية الطبية (مينت ونظيف)",
            ["Executive Navy"] = "الكحلي التنفيذي (أزرق داكن)",
            ["Dark Clarity"] = "الداكن النقي (Dark Mode)",

            // Customizable Asset Tag & QR Code (Tasks 1 & 3)
            ["Print QR Tags"] = "طباعة ملصقات QR",
            ["Print QR Tag Label"] = "طباعة ملصق الأصل ورمز QR",
            ["Print Asset Tag Label"] = "طباعة ملصق وبطاقة الأصل",
            ["Print All Labels"] = "طباعة جميع الملصقات",
            ["Include Real Dynamic QR Code"] = "تضمين رمز QR حقيقي على الملصق",
            ["Save as Default (All Cards)"] = "حفظ كافتراضي لجميع البطاقات",
            ["Reset to Defaults"] = "استعادة الإعدادات الافتراضية",
            ["Card Header & Footer Texts"] = "نصوص البطاقة العلوية والسفلية",
            ["Data Fields (Columns) to Include on Card"] = "البيانات والأعمدة المراد تضمينها على البطاقة",
            ["QR Code & Layout Options"] = "خيارات رمز QR وتصميم البطاقة",
            ["Hospital Tag / ID"] = "رقم الأصل / عهدة المستشفى",
            ["Commissioning Date"] = "تاريخ التشغيل والتوريد",

            // Device Forms
            ["Device Specification Forms"] = "نماذج المواصفات الفنية للأجهزة",
            ["Custom Field Templates"] = "قوالب الحقول المخصصة",
            ["Template Name"] = "اسم النموذج / القالب",
            ["Form Fields"] = "حقول النموذج",
            ["Field Name"] = "اسم الحقل",
            ["Field Label"] = "عنوان الحقل",
            ["Field Type"] = "نوع الإدخال",
            ["Required"] = "إلزامي",

            // Notifications
            ["System & Department Notifications"] = "إشعارات النظام والأقسام",
            ["Department & Ticket Updates"] = "تحديثات الأقسام وتذاكر الدعم",
            ["Live activity stream for your department tickets, discussions, and system updates"] = "موجز الأنشطة المباشر لبلاغات القسم، والمحادثات، وتحديثات النظام",
            ["All Notifications"] = "جميع الإشعارات",
            ["Unread Only"] = "غير المقروءة فقط",
            ["No notifications found"] = "لا توجد إشعارات حالياً",
            ["You are all caught up on department and ticket activity."] = "أنت على اطلاع كامل بجميع أنشطة القسم والتذاكر.",
            ["New"] = "جديد",
            ["View Ticket"] = "عرض التذكرة",
            ["Open Details"] = "عرض التفاصيل",
            ["Ticket Alert"] = "تنبيه تذكرة",
            ["Status Update"] = "تحديث الحالة",
            ["New Message"] = "رسالة جديدة",
            ["Urgent Notice"] = "إشعار عاجل",
            ["Resolved"] = "تم الحل بنجاح",

            // Domain Profile
            ["Windows Domain Single Sign-On Active"] = "الدخول الموحد لشبكة ويندوز مفعل",
            ["Domain Username"] = "اسم مستخدم النطاق",
            ["Assigned Department"] = "القسم التابع له",
            ["Permission Level"] = "مستوى الصلاحيات",
            ["Switch Identity for Testing"] = "تبديل الهوية لأغراض التجربة",

            // Priorities
            ["Low"] = "منخفضة",
            ["Medium"] = "متوسطة",
            ["High"] = "مرتفعة",
            ["Urgent"] = "طارئة جداً",

            // Statuses
            ["opened"] = "مفتوحة",
            ["Opened"] = "مفتوحة",
            ["pending"] = "قيد الإجراء",
            ["Pending"] = "قيد الإجراء",
            ["delayed"] = "مؤجلة",
            ["Delayed"] = "مؤجلة",
            ["canceled"] = "ملغاة",
            ["Canceled"] = "ملغاة",
            ["closed"] = "مغلقة",
            ["Closed"] = "مغلقة",
            ["maintenance"] = "قيد الصيانة",
            ["In Service"] = "في الخدمة",
            ["In Stock"] = "في المخزن (احتياطي)",
            ["Under Maintenance"] = "قيد الصيانة",
            ["Retired"] = "خارج الخدمة / تالف",

            // Teams
            ["IT"] = "تقنية المعلومات (IT)",
            ["BIOMEDICAL"] = "الأجهزة الطبية الحيوية",
            ["GENERAL"] = "الصيانة العامة والمرافق",
            ["IT Maintenance"] = "صيانة تقنية المعلومات",
            ["Biomedical Engineering"] = "الهندسة الطبية الحيوية",
            ["General Facility Maintenance"] = "الصيانة العامة والمرافق",

            // Categories
            ["Hardware"] = "عتاد وأجهزة",
            ["Software"] = "برمجيات ونظم",
            ["Network"] = "شبكات واتصالات",

            // Profile & Directory & Administration
            ["Domain Account & Custody Profile"] = "حساب النطاق وسجل العهدة الشخصية",
            ["Single Sign-On authenticated domain profile & assigned equipment"] = "الملف الشخصي المعتمد بالدخول الموحد والأجهزة المسندة",
            ["Domain Username:"] = "اسم مستخدم النطاق:",
            ["Machine / Host:"] = "اسم الجهاز / المضيف:",
            ["Authentication:"] = "طريقة المصادقة:",
            ["Windows SSO (No Password)"] = "دخول ويندوز الموحد (تلقائي بدون كلمة مرور)",
            ["Switch Identity (Testing Mode)"] = "تبديل الهوية (وضع التجربة والاختبار)",
            ["Switch active profile to test views from another technician, doctor, or staff member's perspective."] = "تبديل الملف الشخصي لمعاينة النظام من منظور فني، طبيب، أو موظف آخر.",
            ["Reset to Default Windows User"] = "إعادة التعيين إلى حساب ويندوز الافتراضي",
            ["Equipment in My Custody"] = "الأجهزة والعهد في عهدتي الشخصية",
            ["Browse All Assets"] = "استعراض جميع الأصول",
            ["No equipment currently assigned to your custody."] = "لا توجد أجهزة مسندة لعهدتك حالياً.",
            ["Report Issue"] = "الإبلاغ عن عطل",
            ["My Support Requests"] = "طلبات الدعم الخاصة بي",
            ["Open Ticket"] = "فتح تذكرة",
            ["You have not submitted any service tickets."] = "لم تقم بتقديم أي تذاكر دعم فني حتى الآن.",
            ["Master Directory & Configuration"] = "الدليل الشامل والإعدادات المركزية",
            ["Manage hospital departments, staff users, physical room locations, and authorized vendors"] = "إدارة أقسام المستشفى، والمستخدمين، ومواقع الغرف، والموردين المعتمدين",
            ["Staff & Users"] = "الموظفون والمستخدمون",
            ["Rooms & Locations"] = "الغرف والمواقع",
            ["Suppliers & Vendors"] = "الموردون والشركات",
            ["Departments"] = "أقسام المستشفى",
            ["Add User"] = "إضافة مستخدم",
            ["Add Room"] = "إضافة غرفة",
            ["Add Vendor / Supplier"] = "إضافة مورد / شركة",
            ["Add Department"] = "إضافة قسم",
            ["PC / Windows Username"] = "اسم المستخدم في ويندوز",
            ["System Role"] = "الدور في النظام",
            ["Maintenance Team"] = "فريق الصيانة",
            ["Auto-Login Status"] = "حالة الدخول التلقائي",
            ["Active (Auto-Login)"] = "نشط (دخول تلقائي)",
            ["Inactive (Rejected)"] = "معطل (مرفوض)",
            ["Deactivate"] = "تعطيل",
            ["Activate"] = "تفعيل",
            ["Manage Role & Team"] = "إدارة الدور وفريق الصيانة",
            ["Display Reference"] = "المسمى المرجعي",
            ["Vendor / Company Name"] = "اسم الشركة / المورد",
            ["Service Scope"] = "نطاق وتخصص الخدمة",
            ["Authorized Maintenance Departments"] = "أقسام الصيانة المصرح لها",
            ["Status"] = "الحالة",
            ["All Vendors"] = "جميع الموردين",
            ["IT Support"] = "دعم تقنية المعلومات",
            ["Biomedical"] = "الهندسة الطبية",
            ["General"] = "الصيانة العامة",
            ["Common / Shared"] = "مشترك بين الأقسام",
            ["Search vendor name..."] = "بحث عن اسم المورد...",
            ["Common to All Departments"] = "معتمد لجميع الأقسام",
            ["Active Vendor"] = "مورد نشط",
            ["Department Head"] = "رئيس القسم",
            ["None assigned"] = "غير محدد",
            ["Remove"] = "حذف",
            ["Save User"] = "حفظ المستخدم",
            ["Save Room"] = "حفظ الغرفة",
            ["Save Supplier"] = "حفظ المورد",
            ["Save Department"] = "حفظ القسم",
            ["Edit Vendor / Supplier"] = "تعديل بيانات المورد",
            ["Save Vendor Changes"] = "حفظ تعديلات المورد",
            ["Save Role & Team"] = "حفظ الدور وفريق الصيانة",

            // Tickets Form & Details
            ["Open New Technical Support Ticket"] = "فتح تذكرة دعم فني جديدة",
            ["Reported By"] = "مقدم البلاغ",
            ["On Behalf Of"] = "نيابة عن",
            ["Your Assigned Hardware Asset"] = "جهازك المسند في العهدة الشخصية",
            ["Target Asset / Workstation Tag (Manual Entry)"] = "رقم الأصل / كود الجهاز (إدخال يدوي)",
            ["Responsible Department"] = "القسم المسؤول عن المعالجة",
            ["Priority Level"] = "مستوى الأولوية",
            ["Assign to Specialist in Responsible Department (Optional)"] = "إسناد إلى أخصائي في القسم المسؤول (اختياري)",
            ["Issue Description & Symptoms"] = "وصف المشكلة والأعراض الفنية",
            ["Submit Ticket"] = "إرسال التذكرة",
            ["Activity & Discussion Timeline"] = "سجل الأنشطة والمناقشات",
            ["Ticket Details & Management"] = "تفاصيل وإدارة التذكرة",
            ["Discussion Locked"] = "المحادثة مغلقة",
            ["This ticket has been resolved and closed. New messages cannot be submitted."] = "تم حل هذه التذكرة وإغلاقها نهائياً. لا يمكن إضافة ردود جديدة.",
            ["Post As"] = "النشر باسم",
            ["Add Note / Progress Update"] = "إضافة ملاحظة / تحديث سير العمل",
            ["Send"] = "إرسال",
            ["Resolve & Close"] = "حل وإغلاق التذكرة",
            ["Log Maintenance Work"] = "تسجيل أعمال الصيانة",
            ["Back to Queue"] = "العودة لقائمة التذاكر",
            ["Print Work Order"] = "طباعة أمر العمل",
            ["Linked Hardware Asset"] = "الأصل والجهاز المرتبط",
            ["Requester Information"] = "معلومات مقدم البلاغ",
            ["Resolve & Close Ticket"] = "حل وإغلاق التذكرة",
            ["Resolution Summary & Solution"] = "ملخص وتوثيق الحل",
            ["Confirm & Close Ticket"] = "تأكيد وإغلاق التذكرة",
            ["Save Maintenance Record"] = "حفظ سجل الصيانة",
            ["Log Maintenance Work Order"] = "تسجيل أمر عمل صيانة",
            ["Target Hardware Asset"] = "الجهاز / الأصل المستهدف",
            ["Responsible Technician / Staff"] = "الفني / المهندس المسؤول",
            ["Third-Party Supplier / Vendor (Optional)"] = "المورد الخارجي / الشركة (اختياري)",
            ["Maintenance Action & Work Performed"] = "الإجراءات وأعمال الصيانة المنفذة",
            ["Parts Replaced / Materials (JSON or Text)"] = "قطع الغيار المستبدلة / المواد (JSON أو نص)",
            ["Record Work Order"] = "تسجيل أمر العمل",
            ["Automated PC Ticket Generator"] = "المولد الآلي لتذاكر الحاسوب",
            ["Workstation & Location Telemetry"] = "بيانات وموقع جهاز العمل",
            ["Generate Automated Support Ticket"] = "إنشاء تذكرة دعم فني آلية",
            ["Submit Support Ticket to IT"] = "إرسال تذكرة الدعم إلى تقنية المعلومات",
            ["Access Restricted"] = "تم تقييد الوصول",
            ["Account Deactivated"] = "تم تعطيل الحساب",
            ["403 - Access Forbidden"] = "403 - غير مصرح بالدخول",
            ["Return to Main Dashboard"] = "العودة إلى لوحة التحكم الرئيسية",
            ["Back to Hospital Assets"] = "العودة إلى سجل أصول المستشفى",
            ["Retry Login"] = "إعادة محاولة الدخول",
            ["Print Asset Tag Label"] = "طباعة ملصق تعريف الأصل",
            ["Print Field Work Order"] = "طباعة أمر العمل الميداني",
            ["Permanently Delete"] = "حذف نهائي",
            ["Cancel & Return"] = "إلغاء والعودة",
            ["Delete Asset Confirmation"] = "تأكيد حذف الأصل من النظام",
            ["Register Hospital Equipment Asset"] = "تسجيل أصل ومعدة مستشفى جديدة",
            ["Select Universal Device Template"] = "اختيار النموذج الموحد لمواصفات الجهاز",
            ["Choose Equipment Type to Generate Specifications Form"] = "اختر نوع الجهاز لتوليد نموذج المواصفات الفنية",
            ["Manage Device Forms"] = "إدارة نماذج ومواصفات الأجهزة",
            ["Technical Specifications Form"] = "نموذج المواصفات الفنية",
            ["Identification & Serial Numbers"] = "التعريف والأرقام التسلسلية",
            ["Hospital Asset Tag / Tag No"] = "رقم الأصل / كود المستشفى",
            ["Initial Custody & Placement"] = "العهدة والموقع الأولي",
            ["Assigned Staff / Custodian"] = "المسؤول عن العهدة / الموظف",
            ["Purchase, Warranty & Periodic Inspection"] = "الشراء، الضمان والفحص الدوري",
            ["Warranty Expiration"] = "تاريخ انتهاء الضمان",
            ["Send Asset to Maintenance (Work Order)"] = "إرسال الأصل للصيانة (أمر عمل)",
            ["Phase 1: Dispatch"] = "المرحلة الأولى: إرسال الأصل",
            ["Receive Asset & Complete Work Order"] = "استلام الأصل وإكمال أمر العمل",
            ["Phase 2: Return & Receiving"] = "المرحلة الثانية: الاستلام والإغلاق",
            ["Replaced Parts & Components (JSON Specification)"] = "قطع الغيار والمكونات المستبدلة (مواصفة JSON)",
            ["Add Replaced Part"] = "إضافة قطعة مستبدلة",
            ["Custody Reassignment & Placement"] = "إعادة تعيين العهدة والموقع",
            ["Receive Asset & Update Custody"] = "استلام الأصل وتحديث العهدة",
            ["Universal Device Forms & Specifications"] = "النماذج الموحدة لمواصفات الأجهزة",
            ["Device Specification Forms & Templates"] = "نماذج وقوالب المواصفات الفنية للأجهزة",
            ["Add New Device Form"] = "إضافة نموذج جهاز جديد",
            ["Build Universal Device Specification Form"] = "بناء وتصميم نموذج مواصفات فنية",
            ["Dynamic Specification Fields"] = "حقول المواصفات الديناميكية المخصصة",
            ["Add Field"] = "إضافة حقل",
            ["Save Device Form"] = "حفظ نموذج الجهاز",
            ["Edit Device Form"] = "تعديل نموذج الجهاز",
            ["Update Device Form"] = "تحديث نموذج الجهاز",
            ["Field Display Label"] = "عنوان الحقل في النموذج",
            ["Variable Key"] = "المفتاح البرمجي للحقل",
            ["Field Type"] = "نوع الحقل",
            ["Dropdown Options (Comma-separated)"] = "خيارات القائمة المنسدلة (مفصولة بفواصل)",
            ["Required?"] = "إلزامي؟",
            ["Return from Maintenance"] = "استلام من الصيانة",
            ["Send to Vendor"] = "إرسال للمورد / الصيانة",
            ["Retire"] = "إسقاط / إتلاف",
            ["Transfer Custody"] = "نقل العهدة",
            ["System Architecture & Security Policy"] = "بنية النظام وسياسات الأمان",
            ["System Information & Security Compliance"] = "معلومات النظام والامتثال الأمني",
            ["Data Governance & Asset Protection"] = "حوكمة البيانات وحماية الأصول",
            ["Return to Dashboard"] = "العودة للوحة التحكم",
        };

        public bool IsArabic => CultureInfo.CurrentUICulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

        public string CurrentCulture => IsArabic ? "ar" : "en";

        public string Direction => IsArabic ? "rtl" : "ltr";

        public string this[string key] => Get(key);

        public string Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            if (IsArabic && ArTranslations.TryGetValue(key.Trim(), out var translation))
            {
                return translation;
            }
            return key;
        }

        public string Get(string key, params object[] args)
        {
            var text = Get(key);
            try
            {
                return string.Format(text, args);
            }
            catch
            {
                return text;
            }
        }

        public string Status(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return "";
            if (!IsArabic) return status;
            return status.ToLowerInvariant() switch
            {
                "opened" => "مفتوحة",
                "pending" => "قيد الإجراء",
                "delayed" => "مؤجلة",
                "canceled" => "ملغاة",
                "closed" => "مغلقة",
                "maintenance" => "قيد الصيانة",
                "in service" => "في الخدمة",
                "in stock" => "في المخزن (احتياطي)",
                "under maintenance" => "قيد الصيانة",
                "retired" => "خارج الخدمة / تالف",
                "completed" => "مكتمل",
                _ => status
            };
        }

        public string Priority(int? priority)
        {
            if (!IsArabic)
            {
                return priority switch
                {
                    1 => "Low",
                    2 => "Medium",
                    3 => "High",
                    4 => "Urgent",
                    _ => "Medium"
                };
            }
            return priority switch
            {
                1 => "منخفضة",
                2 => "متوسطة",
                3 => "مرتفعة",
                4 => "طارئة جداً",
                _ => "متوسطة"
            };
        }

        public string TicketType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type)) return "Hardware";
            if (!IsArabic) return type;
            return type.ToLowerInvariant() switch
            {
                "hardware" => "عتاد وأجهزة",
                "software" => "برمجيات ونظم",
                "network" => "شبكات واتصالات",
                "maintenance" => "صيانة دورية",
                _ => type
            };
        }

        public string TargetTeam(string? team)
        {
            if (string.IsNullOrWhiteSpace(team)) return "IT";
            if (!IsArabic) return team;
            return team.ToUpperInvariant() switch
            {
                "IT" => "تقنية المعلومات",
                "BIOMEDICAL" => "الهندسة الطبية",
                "GENERAL" => "الصيانة العامة",
                _ => team
            };
        }

        public string CustodyType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type)) return "";
            if (!IsArabic) return type;
            return type.ToLowerInvariant() switch
            {
                "personal" => "عهدة شخصية",
                "shared" => "عهدة مشتركة",
                "supplier / external maintenance" => "صيانة خارجية / المورد",
                _ => type
            };
        }

        public string SupplierType(int? type)
        {
            if (!IsArabic)
            {
                return type switch
                {
                    1 => "Equipment & Items",
                    2 => "Maintenance Provider",
                    3 => "Full Service (Items & Maintenance)",
                    _ => "General Vendor"
                };
            }
            return type switch
            {
                1 => "الأجهزة والمعدات",
                2 => "مزود خدمات صيانة",
                3 => "خدمة متكاملة (أجهزة وصيانة)",
                _ => "مورد عام"
            };
        }

        public string MaintenanceType(int? type)
        {
            if (!IsArabic)
            {
                return type switch
                {
                    2 => "Preventive / Periodic",
                    3 => "Supplier Warranty Repair",
                    4 => "External Overhaul / Calibration",
                    _ => "Breakdown / Repair"
                };
            }
            return type switch
            {
                2 => "صيانة وقائية / دورية",
                3 => "إصلاح تحت ضمان المورد",
                4 => "معايرة وفحص خارجي",
                _ => "إصلاح أعطال طارئة"
            };
        }

        public string Role(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return "";
            if (!IsArabic) return role;
            if (role.IndexOf("MAINTENANCE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "فني / مهندس صيانة";
            }
            if (role.IndexOf("ADMIN", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "مدير النظام (Admin)";
            }
            return role;
        }
    }

    public static class LocHelper
    {
        public static string TranslateStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return "";
            return status.ToLowerInvariant() switch
            {
                "in service" => "في الخدمة",
                "in stock" => "في المخزن (احتياطي)",
                "under maintenance" => "قيد الصيانة",
                "retired" => "خارج الخدمة / تالف",
                "opened" => "مفتوحة",
                "pending" => "قيد الإجراء",
                "delayed" => "مؤجلة",
                "canceled" => "ملغاة",
                "closed" => "مغلقة",
                "completed" => "مكتمل",
                _ => status
            };
        }

        public static string TranslateType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type)) return "";
            return type.ToLowerInvariant() switch
            {
                "laptop" => "حاسوب محمول",
                "desktop" => "حاسوب مكتبي",
                "server" => "خادم بيانات",
                "monitor" => "شاشة عرض",
                "printer" => "طابعة شبكية",
                "scanner" => "ماسح ضوئي",
                "medical device" => "جهاز طبي",
                "network switch" => "مقسم شبكة",
                "ups" => "مزود طاقة احتياطي",
                _ => type
            };
        }

        public static string TranslateTicketType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type)) return "Hardware";
            return type.ToLowerInvariant() switch
            {
                "hardware" => "عتاد وأجهزة",
                "software" => "برمجيات ونظم",
                "network" => "شبكات واتصالات",
                "maintenance" => "صيانة دورية",
                _ => type
            };
        }

        public static string TranslateTargetTeam(string? team)
        {
            if (string.IsNullOrWhiteSpace(team)) return "IT";
            return team.ToUpperInvariant() switch
            {
                "IT" => "تقنية المعلومات",
                "BIOMEDICAL" => "الهندسة الطبية",
                "GENERAL" => "الصيانة العامة",
                _ => team
            };
        }
    }
}
