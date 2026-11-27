# دليل توزيع شهادة SOLVIS عبر Windows Active Directory GPO و DNS
# SOLVIS Domain Trust & DNS Deployment Guide (GPO)

هذا الدليل مخصص لمسؤول نطاق ويندوز (Windows Domain Administrator / Active Directory SysAdmin) لربط خادم SOLVIS بالشبكة الداخلية وتوثيق شهادة الأمان لجميع أجهزة الكمبيوتر في المستشفى تلقائياً وبضغطة زر واحدة دون الحاجة للذهاب إلى كل جهاز على حدة.

---

## الخطوة 1: ربط اسم النطاق `solvis.local` في سيرفر الـ DNS (Active Directory DNS)

لكي تتمكن جميع أجهزة المستشفى من الوصول للنظام عبر الرابط `https://solvis.local` أو `https://solvis`:

1. سجّل الدخول إلى خادم الدومين الرئيسي (**Windows Server Domain Controller**).
2. افتح مدير الـ DNS:
   - اضغط `Win + R` واكتب: `dnsmgmt.msc` ثم اضغط **Enter**.
3. من القائمة الجانبية، وسّع اسم الخادم ثم وسّع **Forward Lookup Zones**.
4. اختر إحدى الطريقتين التاليتين:
   - **الخيار أ (مستحسن):** اضغط بالزر الأيمن على **Forward Lookup Zones** واختر **New Zone**:
     - اختر **Primary Zone** وتأكد من تحديد **Store the zone in Active Directory**.
     - في اسم النطاق (Zone Name) اكتب: `solvis.local`.
     - واصل بالضغط على **Next** ثم **Finish**.
     - اضغط بالزر الأيمن داخل منطقة `solvis.local` الجديدة واختر **New Host (A or AAAA)...**:
       - اترك خانة Name فارغة (لتمثيل نفس النطاق `solvis.local`)، أو اكتب `solvis`.
       - في خانة **IP address** اكتب الـ Static IP الخاص بخادم أوبونتو (مثال: `192.168.10.50`).
       - اضغط **Add Host**.
   - **الخيار ب (ضمن دومين المستشفى الحالي، مثلاً `hospital.local`):**
     - افتح نطاق المستشفى الموجود حالياً واضغط بالزر الأيمن ثم **New Host (A)**.
     - في خانة Name اكتب: `solvis`.
     - في خانة IP Address اكتب الـ Static IP لخادم أوبونتو.
     - اضغط **Add Host**.
5. **الاختبار من أي جهاز عميل:**
   - افتح موجه الأوامر `cmd` وجرب الأمر:
     ```cmd
     ping solvis.local
     ```
   - يجب أن يرد الـ Ping بعنوان الـ IP الخاص بخادم أوبونتو مباشرة.

---

## الخطوة 2: توزيع شهادة الـ Root CA عبر الجروب بوليسي (Group Policy - GPO)

هذه الخطوة تضمن أن جميع أجهزة ويندوز المنضمة للدومين (Domain-Joined Computers) ستثق في شهادة SOLVIS فوراً، وسيظهر القفل الأخضر الآمن (`https://solvis.local`) في متصفحات Chrome و Edge و Firefox بدون أي تحذيرات أمنية.

### الملف المطلوب:
انسخ الملف `solvis-root-ca.crt` (الموجود في هذا المجلد) إلى خادم الـ Domain Controller.

### خطوات إنشاء وتطبيق الـ GPO:
1. على خادم الدومين، افتح مدير سياسات المجموعة (**Group Policy Management**):
   - اضغط `Win + R` واكتب: `gpmc.msc` ثم اضغط **Enter**.
2. وسّع **Forest** -> **Domains** -> اسم دومين المستشفى (مثال: `hospital.local`).
3. اضغط بالزر الأيمن على اسم الدومين واختر:
   - **Create a GPO in this domain, and Link it here...**
4. سمّ السياسة الجديدة:
   ```text
   SOLVIS-Trusted-Root-CA-Policy
   ```
5. اضغط بالزر الأيمن على السياسة الجديدة `SOLVIS-Trusted-Root-CA-Policy` واختر **Edit...**.
6. في نافذة محرر السياسات (Group Policy Management Editor)، انتقل للمسار التالي:
   ```text
   Computer Configuration
     └── Policies
           └── Windows Settings
                 └── Security Settings
                       └── Public Key Policies
                             └── Trusted Root Certification Authorities
   ```
7. اضغط بالزر الأيمن في المساحة الفارغة داخل **Trusted Root Certification Authorities** واختر:
   - **Import...**
8. اضغط **Next** في معالج الاستيراد، ثم اضغط **Browse...** واختر ملف:
   `solvis-root-ca.crt` (أو `solvis-root-ca.cer`).
9. تأكد من تحديد:
   - **Place all certificates in the following store: Trusted Root Certification Authorities**.
10. اضغط **Next** ثم **Finish**. ستظهر رسالة:
    *(The import was successful)*.

---

## الخطوة 3: التفعيل الفوري على أجهزة الموظفين (Verification)

بشكل افتراضي، تقوم أنظمة ويندوز بتحديث سياسات الدومين كل 90 دقيقة. وللتطبيق الفوري على أي جهاز:

1. افتح موجه الأوامر كمسؤول (**CMD as Administrator**) على جهاز الموظف ونفّذ:
   ```cmd
   gpupdate /force
   ```
2. افتح المتصفح (Google Chrome أو Microsoft Edge) وادخل إلى:
   ```text
   https://solvis.local
   ```
3. ستلاحظ مباشرة:
   - ظهور **رمز القفل المشفر بالكامل (Connection is secure)**.
   - اتصال مشفر وآمن 100% عبر TLS 1.3 / TLS 1.2.
   - تسجيل الدخول التلقائي بالنطاق (Windows SSO) يعمل بسلاسة تامة.

---

## ملاحظة للأجهزة غير المنضمة للدومين (Workgroup / Standalone PCs)
إذا كان هناك جهاز كمبيوتر في المستشفى غير منضم للدومين:
- انسخ مجلد `windows_domain_setup` إلى ذلك الجهاز.
- اضغط بالزر الأيمن على ملف `install-root-ca.bat` واختر **Run as administrator**.
- سينتهي التثبيت خلال ثانية واحدة وسيصبح الجهاز موثقاً للشهادة مباشرة.
