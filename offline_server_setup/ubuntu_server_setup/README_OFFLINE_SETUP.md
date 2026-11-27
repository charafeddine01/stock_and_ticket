# دليل الإعداد الشامل لخادم SOLVIS على شبكة داخلية معزولة (Offline Network)
# SOLVIS Offline Server Setup, Static IP, Domain SSL & Windows Trust Guide

> [!NOTE]
> هذا الدليل مصمم لتمكين أي مهندس أو فني تقنية معلومات من إعداد خادم **SOLVIS** وتشغيله وتوثيقه بالكامل على أي شبكة داخلية معزولة بدون اتصال إنترنت، وضمان ظهور القفل الأخضر الموثق (`https://solvis.local`) على جميع أجهزة المستشفى.
> 
> This guide is designed so that any IT administrator or technician can deploy, configure, and trust the **SOLVIS** server in an offline hospital network without internet access.

---

## 📁 محتويات مجلد الإعداد (Package Contents)

| الملف / المجلد (File) | الوظيفة (Purpose) |
|---|---|
| **`setup-all-offline.sh`** | السكربت الرئيسي الشامل (ينفذ الخطوات 1 و 2 تلقائياً وبشكل تفاعلي) |
| **`set-static-ip.sh`** | سكربت ضبط الـ Static IP وكرت الشبكة والـ DNS عبر Netplan |
| **`setup-domain-ssl.sh`** | سكربت إنشاء شهادة الـ Root CA وشهادة SAN وضبط Nginx لـ HTTPS |
| **`windows_domain_setup/`** | مجلد حزمة التوثيق لويندوز دومين وأجهزة الموظفين: |
| ├── `install-root-ca.bat` | ملف تثبيت الشهادة للأجهزة الفردية كمسؤول (Run as administrator) |
| ├── `install-root-ca.ps1` | سكربت PowerShell لتثبيت واختبار الشهادة برمجياً |
| ├── `GPO-Deployment-Guide.md` | دليل تفصيلي لتوزيع الشهادة عبر الجروب بوليسي (Active Directory GPO) |
| └── `solvis-root-ca.crt` | شهادة الـ Root CA المصدرة (تتولد تلقائياً وتُنسخ هنا) |

---

## 🚀 طريقة التنفيذ السريعة (Quick Start)

### الخطوة 1: على خادم أوبونتو (Ubuntu Server)

1. انسخ مجلد `offline_server_setup` إلى خادم أوبونتو (أو إلى `/tmp/offline_server_setup`).
2. افتح موجه الأوامر (Terminal) ونفذ السكربت الرئيسي كمسؤول:
   ```bash
   cd /path/to/offline_server_setup
   sudo bash setup-all-offline.sh
   ```
3. سيقوم المعالج التفاعلي بسؤالك عن بيانات الشبكة الجديدة:
   - **Static IP**: اكتب الآي بي الجديد المراد للخادم (مثال: `192.168.10.50`).
   - **Subnet Prefix**: اضغط Enter للقبول بالافتراضي (`24` لـ `255.255.255.0`).
   - **Gateway**: اكتب عنوان الراوتر / الجيت واي (مثال: `192.168.10.1`).
   - **DNS Server**: اكتب آي بي سيرفر الدومين أو الدي إن إس الداخلي (مثال: `192.168.10.10`).
4. **ماذا سيفعل السكربت تلقائياً؟**
   - يضبط كرت الشبكة عبر Netplan ويفحص سريان الآي بي الجديد.
   - يسمي الخادم `solvis` ويضيف النطاق `solvis.local` إلى ملف `/etc/hosts`.
   - ينشئ مرجع شهادات داخلي خاص (`SOLVIS Internal Root CA`) صالح لمدة 10 سنوات.
   - يصدر شهادة سيرفر معتمدة بمواصفات SAN الحديثة تشمل:
     (`solvis.local` و `solvis` و الـ IP الجديد و `localhost`).
   - يضبط إعدادات Nginx لتحويل أي اتصال على بورت 80 تلقائياً إلى بورت 443 المشفر (HTTPS).
   - يصدر ملفات الشهادة لمدير الدومين إلى: `/var/www/solvis/windows_domain_setup/`.

---

### الخطوة 2: على خادم ويندوز دومين (Active Directory Domain Controller)

لضمان وصول جميع الموظفين عبر الرابط `https://solvis.local` وظهور القفل الأخضر بدون أي رسائل تحذير:

#### أ. إضافة سجل الـ DNS (DNS A-Record):
1. افتح **DNS Manager** على خادم الدومين (`dnsmgmt.msc`).
2. اضغط بالزر الأيمن على **Forward Lookup Zones** واختر **New Zone** باسم:
   `solvis.local` (Primary Zone).
3. داخل منطقة `solvis.local` أضف **New Host (A)**:
   - اترك الاسم فارغاً أو اكتب `solvis`.
   - في حقل الـ IP Address ضع الـ Static IP الخاص بسيرفر أوبونتو.
   - اضغط **Add Host**.

#### ب. توزيع الشهادة لجميع أجهزة الكمبيوتر عبر الجروب بوليسي (GPO):
1. خذ نسخة من ملف الشهادة `solvis-root-ca.crt` (المتولد على خادم أوبونتو في المسار `/var/www/solvis/windows_domain_setup/`).
2. على خادم الدومين، افتح **Group Policy Management** (`gpmc.msc`).
3. اضغط بالزر الأيمن على الدومين وأنشئ GPO جديدة باسم:
   `SOLVIS-Trusted-Root-CA-Policy`.
4. اضغط بالزر الأيمن على السياسة واختر **Edit** وانتقل إلى:
   `Computer Configuration -> Policies -> Windows Settings -> Security Settings -> Public Key Policies -> Trusted Root Certification Authorities`.
5. اضغط بالزر الأيمن واختر **Import** وحدد ملف `solvis-root-ca.crt`.
6. انتهى! سيتم نشر الشهادة تلقائياً لجميع أجهزة الكمبيوتر في المستشفى.

---

### الخطوة 3: التوثيق الفوري على جهاز الموظف (Client Workstation)

- **للأجهزة المنضمة للدومين:**
  - لتحديث السياسات فوراً على أي جهاز موظف، افتح موجه الأوامر `cmd` كمسؤول ونفذ:
    ```cmd
    gpupdate /force
    ```
  - افتح المتصفح وادخل على:
    ```text
    https://solvis.local
    ```
  - ستجد أن الموقع يفتح فوراً بقفل أمان أخضر مشفر بالكامل وتسجيل دخول تلقائي بالويندوز بدون أي تدخل.

- **للأجهزة المستقلة (غير المنضمة للدومين):**
  - انسخ مجلد `windows_domain_setup` إلى الجهاز.
  - اضغط بالزر الأيمن على `install-root-ca.bat` واختر **Run as administrator**.

---

## 🛠️ استكشاف الأخطاء وحلها (Troubleshooting)

### 1. في حال عدم استجابة الشبكة بعد تغيير الـ IP:
تأكد من تطبيق إعدادات Netplan يدوياً:
```bash
sudo netplan apply
sudo systemctl restart systemd-networkd
ip a show
```
توجد نسخة احتياطية من إعداداتك السابقة دائماً في:
`/etc/netplan/backup_*/`

### 2. في حال عدم عمل خادم Nginx:
افحص أخطاء الإعدادات وأعد التشغيل:
```bash
sudo nginx -t
sudo systemctl status nginx
sudo systemctl restart nginx
```

### 3. في حال ظهور الموقع "Not Secure" على جهاز كمبيوتر:
- تأكد أنك تدخل عبر الرابط `https://solvis.local` وليس عبر الـ IP فقط (شهادات الـ SSL تربط النطاق وتتحقق منه).
- تأكد من تثبيت شهادة `solvis-root-ca.crt` في مستودع **Trusted Root Certification Authorities** (أمر `certmgr.msc` على ويندوز).

---

## 🇬🇧 English Summary & Cheat Sheet

1. **On Ubuntu Server:**
   ```bash
   sudo bash setup-all-offline.sh
   ```
   Follow the prompt to enter your static IP, Gateway, and local DNS IP.

2. **On Windows Domain Controller:**
   - In **DNS Manager** (`dnsmgmt.msc`): Create an A-record for `solvis.local` pointing to Ubuntu's Static IP.
   - In **Group Policy** (`gpmc.msc`): Import `windows_domain_setup/solvis-root-ca.crt` into:
     `Computer Configuration -> Windows Settings -> Security Settings -> Public Key Policies -> Trusted Root Certification Authorities`.

3. **On Client PCs:**
   Run `gpupdate /force` and open `https://solvis.local`. Enjoy zero-warning, 100% trusted HTTPS!
