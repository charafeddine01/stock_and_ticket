/**
 * STOCK & HELPDESK MANAGEMENT PORTAL
 * Interactive Scripts: Modern Top Notifications, Page Transitions & Responsive Autohide Sidebar
 * Fully localized for English & Arabic (RTL support)
 */

(function () {
    "use strict";

    const isArabic = document.documentElement.dir === "rtl" || document.documentElement.lang === "ar";

    /* ==========================================================================
       1. SIDEBAR & AUTOHIDE CONTROLLER (TASK 3)
       ========================================================================== */
    const SIDEBAR_STORAGE_KEY = "stockportal_sidebar_collapsed";

    function initSidebar() {
        const body = document.body;
        const toggleBtn = document.getElementById("sidebarToggleBtn");
        const pinBtn = document.getElementById("sidebarPinBtn");
        const closeBtn = document.getElementById("sidebarCloseBtn");
        const backdrop = document.getElementById("sidebarBackdrop");
        const sidebar = document.querySelector(".sidebar");
        const autohideTrigger = document.getElementById("sidebarAutohideTrigger");

        // Read saved collapsed preference for desktop (>= 992px)
        const isDesktop = window.innerWidth >= 992;
        const savedCollapsed = localStorage.getItem(SIDEBAR_STORAGE_KEY) === "true";

        if (isDesktop && savedCollapsed) {
            body.classList.add("sidebar-collapsed");
            updateToggleBtnIcon(true);
        }

        // Toggle button in Top Navbar
        if (toggleBtn) {
            toggleBtn.addEventListener("click", function (e) {
                e.preventDefault();
                if (window.innerWidth < 992) {
                    // Mobile Drawer Toggle
                    body.classList.toggle("sidebar-mobile-show");
                    if (backdrop) {
                        backdrop.classList.toggle("show", body.classList.contains("sidebar-mobile-show"));
                    }
                } else {
                    // Desktop Autohide/Collapse Toggle
                    const nowCollapsed = body.classList.toggle("sidebar-collapsed");
                    localStorage.setItem(SIDEBAR_STORAGE_KEY, nowCollapsed ? "true" : "false");
                    updateToggleBtnIcon(nowCollapsed);
                }
            });
        }

        // Pin/Unpin button in Sidebar Header
        if (pinBtn) {
            pinBtn.addEventListener("click", function (e) {
                e.preventDefault();
                const nowCollapsed = body.classList.toggle("sidebar-collapsed");
                localStorage.setItem(SIDEBAR_STORAGE_KEY, nowCollapsed ? "true" : "false");
                updateToggleBtnIcon(nowCollapsed);
            });
        }

        // Mobile close button
        if (closeBtn) {
            closeBtn.addEventListener("click", function () {
                closeMobileSidebar();
            });
        }

        // Mobile backdrop click
        if (backdrop) {
            backdrop.addEventListener("click", function () {
                closeMobileSidebar();
            });
        }

        // Close mobile drawer when clicking navigation links
        document.querySelectorAll(".sidebar-nav .nav-link").forEach(function (link) {
            link.addEventListener("click", function () {
                if (window.innerWidth < 992) {
                    closeMobileSidebar();
                }
            });
        });

        // Desktop Autohide Peek on edge hover
        if (autohideTrigger && sidebar) {
            let peekTimeout = null;

            autohideTrigger.addEventListener("mouseenter", function () {
                if (body.classList.contains("sidebar-collapsed") && window.innerWidth >= 992) {
                    clearTimeout(peekTimeout);
                    sidebar.classList.add("sidebar-peeking");
                }
            });

            sidebar.addEventListener("mouseleave", function () {
                if (body.classList.contains("sidebar-collapsed") && window.innerWidth >= 992) {
                    peekTimeout = setTimeout(function () {
                        sidebar.classList.remove("sidebar-peeking");
                    }, 200);
                }
            });

            sidebar.addEventListener("mouseenter", function () {
                clearTimeout(peekTimeout);
            });
        }

        // Keyboard Shortcut: Ctrl + B or Cmd + B to toggle sidebar
        document.addEventListener("keydown", function (e) {
            if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "b") {
                const targetTag = (e.target.tagName || "").toLowerCase();
                if (targetTag !== "input" && targetTag !== "textarea" && targetTag !== "select") {
                    e.preventDefault();
                    if (toggleBtn) toggleBtn.click();
                }
            }
        });

        function closeMobileSidebar() {
            body.classList.remove("sidebar-mobile-show");
            if (backdrop) {
                backdrop.classList.remove("show");
            }
        }

        function updateToggleBtnIcon(collapsed) {
            if (!toggleBtn) return;
            const icon = toggleBtn.querySelector("i");
            if (icon) {
                if (collapsed) {
                    icon.className = "fa-solid fa-bars-staggered";
                    toggleBtn.setAttribute("title", isArabic ? "تثبيت الشريط الجانبي (Ctrl+B)" : "Pin Sidebar (Ctrl+B)");
                } else {
                    icon.className = "fa-solid fa-bars";
                    toggleBtn.setAttribute("title", isArabic ? "إخفاء تلقائي للشريط الجانبي (Ctrl+B)" : "Auto-hide Sidebar (Ctrl+B)");
                }
            }
        }
    }

    /* ==========================================================================
       2. SYSTEM ANIMATIONS & PAGE-TO-PAGE TRANSITIONS (TASK 2)
       ========================================================================== */
    function initPageTransitions() {
        const loadingBar = document.getElementById("pageLoadingBar");
        if (!loadingBar) return;

        // Reset loading bar on initial load or browser back/forward (bfcache)
        window.addEventListener("pageshow", function () {
            loadingBar.classList.remove("active");
            loadingBar.style.width = "0%";
            loadingBar.style.opacity = "0";
            document.body.classList.remove("page-navigating");
        });

        // Intercept internal link clicks to trigger smooth top loading bar & page transition
        document.addEventListener("click", function (e) {
            const anchor = e.target.closest("a");
            if (!anchor) return;

            const href = anchor.getAttribute("href");
            const target = anchor.getAttribute("target");

            // Ignore external, anchor hash, download, or javascript links
            if (!href || href.startsWith("#") || href.startsWith("javascript:") || href.startsWith("mailto:") || href.startsWith("tel:")) {
                return;
            }
            if (target && target !== "_self") {
                return;
            }
            if (anchor.getAttribute("download") !== null || anchor.getAttribute("data-bs-toggle") !== null) {
                return;
            }

            // Check if same origin
            try {
                const url = new URL(anchor.href, window.location.origin);
                if (url.origin !== window.location.origin) {
                    return;
                }
                // If same URL including hash, ignore
                if (url.pathname === window.location.pathname && url.search === window.location.search && url.hash) {
                    return;
                }
            } catch (err) {
                return;
            }

            // Start progress bar animation
            startPageLoadingBar();
        });

        function startPageLoadingBar() {
            loadingBar.style.opacity = "1";
            loadingBar.classList.add("active");
            loadingBar.style.width = "30%";
            document.body.classList.add("page-navigating");

            setTimeout(function () {
                if (loadingBar.classList.contains("active")) {
                    loadingBar.style.width = "75%";
                }
            }, 120);
        }
    }

    /* ==========================================================================
       3. MODERN TOP FLOATING NOTIFICATION SYSTEM (TASK 1)
       ========================================================================== */
    const DISPLAYED_NOTIF_KEY = "displayed_popup_notif_ids";
    let displayedIds = new Set();
    try {
        const stored = sessionStorage.getItem(DISPLAYED_NOTIF_KEY);
        if (stored) {
            JSON.parse(stored).forEach(id => displayedIds.add(id));
        }
    } catch (e) { }

    // Synthesized Web Audio API harmonic chime (pure browser-native, zero external files)
    function playNotificationChime() {
        try {
            const AudioContextClass = window.AudioContext || window.webkitAudioContext;
            if (!AudioContextClass) return;
            const ctx = new AudioContextClass();
            if (ctx.state === "suspended") {
                ctx.resume().catch(function () {});
            }

            const now = ctx.currentTime;
            
            // Note 1 (E5 - 659.25Hz)
            const osc1 = ctx.createOscillator();
            const gain1 = ctx.createGain();
            osc1.type = "sine";
            osc1.frequency.setValueAtTime(659.25, now);
            gain1.gain.setValueAtTime(0.12, now);
            gain1.gain.exponentialRampToValueAtTime(0.001, now + 0.3);
            osc1.connect(gain1);
            gain1.connect(ctx.destination);
            osc1.start(now);
            osc1.stop(now + 0.3);

            // Note 2 (A5 - 880Hz, soft chime chord)
            const osc2 = ctx.createOscillator();
            const gain2 = ctx.createGain();
            osc2.type = "sine";
            osc2.frequency.setValueAtTime(880, now + 0.08);
            gain2.gain.setValueAtTime(0.14, now + 0.08);
            gain2.gain.exponentialRampToValueAtTime(0.001, now + 0.42);
            osc2.connect(gain2);
            gain2.connect(ctx.destination);
            osc2.start(now + 0.08);
            osc2.stop(now + 0.42);
        } catch (e) { }
    }

    // Modern Top Notification Card Generator
    function showModernNotification(item) {
        let container = document.getElementById("modernNotifContainer");
        if (!container) {
            container = document.createElement("div");
            container.id = "modernNotifContainer";
            container.className = "modern-notif-container";
            container.setAttribute("aria-live", "polite");
            document.body.appendChild(container);
        }

        const notifId = "modern_notif_" + item.id;
        if (document.getElementById(notifId)) return;

        // Classify notification type & styles with localization
        let cardTypeClass = "type-ticket";
        let tagText = isArabic ? "تنبيه تذكرة" : "Ticket Alert";
        let iconHtml = '<i class="fa-solid fa-ticket"></i>';

        const typeLower = (item.type || "").toLowerCase();
        if (typeLower.includes("message") || typeLower.includes("comment")) {
            cardTypeClass = "type-message";
            tagText = isArabic ? "رسالة جديدة" : "New Message";
            iconHtml = '<i class="fa-solid fa-comments"></i>';
        } else if (typeLower.includes("status")) {
            cardTypeClass = "type-status";
            tagText = isArabic ? "تحديث الحالة" : "Status Update";
            iconHtml = '<i class="fa-solid fa-arrows-rotate"></i>';
        } else if (typeLower.includes("urgent") || typeLower.includes("critical")) {
            cardTypeClass = "type-urgent";
            tagText = isArabic ? "إشعار عاجل" : "Urgent Notice";
            iconHtml = '<i class="fa-solid fa-triangle-exclamation"></i>';
        } else if (typeLower.includes("success") || typeLower.includes("resolved") || typeLower.includes("closed")) {
            cardTypeClass = "type-success";
            tagText = isArabic ? "تم الحل بنجاح" : "Resolved";
            iconHtml = '<i class="fa-solid fa-circle-check"></i>';
        }

        const card = document.createElement("div");
        card.id = notifId;
        card.className = "modern-notif-card " + cardTypeClass;
        card.setAttribute("role", "alert");

        const targetUrl = item.linkUrl || "/Notifications";
        const openText = isArabic ? "عرض التفاصيل" : "Open Details";
        const dismissText = isArabic ? "إغلاق" : "Dismiss";
        const timeAgoText = item.timeAgo || (isArabic ? "الآن" : "Just now");

        card.innerHTML = `
            <div class="modern-notif-accent"></div>
            <div class="modern-notif-body">
                <div class="modern-notif-icon-wrap">
                    ${iconHtml}
                    <div class="modern-notif-ping"></div>
                </div>
                <div class="modern-notif-main">
                    <div class="modern-notif-header">
                        <span class="modern-notif-tag">${tagText}</span>
                        <span class="modern-notif-time"><i class="fa-regular fa-clock me-1"></i>${timeAgoText}</span>
                    </div>
                    <div class="modern-notif-title">${item.title || (isArabic ? "إشعار" : "Notification")}</div>
                    <div class="modern-notif-desc">${item.message || ""}</div>
                    <div class="modern-notif-actions">
                        <a href="${targetUrl}" class="modern-notif-btn-open">
                            <span>${openText}</span>
                            <i class="fa-solid fa-arrow-right"></i>
                        </a>
                        <button type="button" class="modern-notif-btn-dismiss">${dismissText}</button>
                    </div>
                </div>
                <button type="button" class="modern-notif-close-icon" aria-label="Close" title="${dismissText}">
                    <i class="fa-solid fa-xmark"></i>
                </button>
            </div>
            <div class="modern-notif-progress">
                <div class="modern-notif-progress-bar"></div>
            </div>
        `;

        // Prepend so newest notification appears on top
        if (container.firstChild) {
            container.insertBefore(card, container.firstChild);
        } else {
            container.appendChild(card);
        }

        // Auto-dismiss timer (8 seconds)
        let autoDismissTimer = setTimeout(function () {
            dismissCard(card, item.id, false);
        }, 8000);

        // Pause countdown on hover
        card.addEventListener("mouseenter", function () {
            clearTimeout(autoDismissTimer);
        });

        card.addEventListener("mouseleave", function () {
            autoDismissTimer = setTimeout(function () {
                dismissCard(card, item.id, false);
            }, 3000);
        });

        // Click handlers
        const btnDismiss = card.querySelector(".modern-notif-btn-dismiss");
        const btnClose = card.querySelector(".modern-notif-close-icon");
        const btnOpen = card.querySelector(".modern-notif-btn-open");

        if (btnDismiss) {
            btnDismiss.addEventListener("click", function (e) {
                e.stopPropagation();
                dismissCard(card, item.id, true);
            });
        }

        if (btnClose) {
            btnClose.addEventListener("click", function (e) {
                e.stopPropagation();
                dismissCard(card, item.id, true);
            });
        }

        if (btnOpen) {
            btnOpen.addEventListener("click", function () {
                markNotificationReadOnServer(item.id);
            });
        }
    }

    function dismissCard(card, id, markServerRead) {
        if (!card || card.classList.contains("dismissing")) return;
        card.classList.add("dismissing");

        if (markServerRead && id) {
            markNotificationReadOnServer(id);
        }

        setTimeout(function () {
            if (card.parentNode) {
                card.parentNode.removeChild(card);
            }
        }, 360);
    }

    // Send AJAX to mark notification as read
    function markNotificationReadOnServer(id) {
        if (!id || id <= 0) return;
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        const headers = {
            "X-Requested-With": "XMLHttpRequest"
        };
        if (tokenInput && tokenInput.value) {
            headers["RequestVerificationToken"] = tokenInput.value;
        }
        fetch("/Notifications/Dismiss/" + id, {
            method: "POST",
            headers: headers
        }).catch(function () {});
    }

    // Polling function for unread notifications
    async function checkUnreadNotifications() {
        try {
            const res = await fetch("/Notifications/GetLatestUnread", {
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            if (!res.ok) return;
            const data = await res.json();

            // Update top navbar badge
            const badge = document.getElementById("navUnreadBadge");
            const navLink = document.getElementById("navNotificationsLink");
            if (data.count > 0) {
                if (badge) {
                    badge.textContent = data.count;
                    badge.style.display = "inline-block";
                    badge.classList.add("badge-pulse");
                } else if (navLink) {
                    const newBadge = document.createElement("span");
                    newBadge.id = "navUnreadBadge";
                    newBadge.className = "position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger badge-pulse";
                    newBadge.style.fontSize = "0.65rem";
                    newBadge.textContent = data.count;
                    navLink.appendChild(newBadge);
                }
            } else if (badge) {
                badge.style.display = "none";
                badge.classList.remove("badge-pulse");
            }

            // Display modern top floating popups for new notifications
            if (Array.isArray(data.notifications)) {
                let hasNew = false;
                data.notifications.forEach(function (item) {
                    if (!displayedIds.has(item.id)) {
                        displayedIds.add(item.id);
                        hasNew = true;
                        showModernNotification(item);
                    }
                });

                if (hasNew) {
                    playNotificationChime();
                    try {
                        sessionStorage.setItem(DISPLAYED_NOTIF_KEY, JSON.stringify(Array.from(displayedIds)));
                    } catch (e) { }
                }
            }
        } catch (e) { }
    }

    // Expose preview/test helper to window for interactive testing
    window.showTestNotification = function (customTitle, customMessage, customType) {
        const testItem = {
            id: Date.now(),
            title: customTitle || (isArabic ? "تم تعيين تذكرة خدمة جديدة" : "New Service Ticket Assigned"),
            message: customMessage || (isArabic ? "تم توجيه تذكرة عاجلة لفحص شبكة قسم الأشعة المقطعية إلى فريقك." : "Ticket #204 - Emergency CT Scanner Network Port issue has been dispatched to your queue."),
            type: customType || "TicketStatus",
            linkUrl: "/Tickets",
            timeAgo: isArabic ? "الآن" : "Just now"
        };
        playNotificationChime();
        showModernNotification(testItem);
    };

    /* ==========================================================================
       4. THEME CONTROLLER & PERSISTENCE
       ========================================================================== */
    const THEME_STORAGE_KEY = "stockportal_theme";
    const THEME_LABELS = {
        "light": isArabic ? "فاتح" : "Light",
        "dark": isArabic ? "داكن" : "Dark",
        "clinical": isArabic ? "طبي (أخضر فاتح)" : "Clinical (Light Green)"
    };

    window.setAppTheme = function (themeName) {
        if (themeName === "clarity" || themeName === "navy") themeName = "light";
        if (!themeName || !THEME_LABELS[themeName]) themeName = "light";
        document.documentElement.setAttribute("data-theme", themeName);
        try {
            localStorage.setItem(THEME_STORAGE_KEY, themeName);
        } catch (e) { }

        const labelElem = document.getElementById("currentThemeLabel");
        if (labelElem && THEME_LABELS[themeName]) {
            labelElem.textContent = THEME_LABELS[themeName];
        }
    };

    function initTheme() {
        let savedTheme = "light";
        try {
            savedTheme = localStorage.getItem(THEME_STORAGE_KEY) || "light";
            if (savedTheme === "clarity" || savedTheme === "navy") savedTheme = "light";
        } catch (e) { }
        window.setAppTheme(savedTheme);
    }

    /* ==========================================================================
       5. BOOTSTRAP MODAL VIEWPORT HOISTING & STACKING FIX
       ========================================================================== */
    function initModalFixes() {
        // Automatically hoist any Bootstrap modal to document.body when opening.
        // This completely prevents stacking context traps, containing-block clipping,
        // and backdrop overlay interception when modals are declared inside table cells
        // (td/tr), cards, or layout containers with CSS animations/transforms.
        document.addEventListener("show.bs.modal", function (e) {
            const modal = e.target;
            if (modal && modal.parentElement && modal.parentElement !== document.body) {
                document.body.appendChild(modal);
            }
        });
    }

    /* ==========================================================================
       6. UNIVERSAL DATA TABLE COLUMN SORTING
       ========================================================================== */
    const PRIORITY_MAP = {
        "urgent": 4, "عاجل": 4, "طارئ": 4, "critical": 4, "حرج": 4,
        "high": 3, "مرتفع": 3, "عالي": 3,
        "medium": 2, "متوسط": 2,
        "low": 1, "منخفض": 1
    };

    function tryParsePriority(str) {
        if (!str) return null;
        const lower = str.toLowerCase().trim();
        if (PRIORITY_MAP[lower] !== undefined) return PRIORITY_MAP[lower];
        for (const key in PRIORITY_MAP) {
            if (lower === key || lower.indexOf(" " + key) >= 0 || lower.indexOf(key + " ") >= 0) {
                return PRIORITY_MAP[key];
            }
        }
        return null;
    }

    function tryParseDuration(str) {
        if (!str) return null;
        const match = str.match(/^(\d+(?:\.\d+)?)\s*(days?|day|d|أيام|يوم|hours?|hour|hrs?|h|ساعات|ساعة|mins?|minute|minutes?|m|دقائق|دقيقة)$/i);
        if (match) {
            const val = parseFloat(match[1]);
            const unit = match[2].toLowerCase();
            if (unit.startsWith("d") || unit === "يوم" || unit === "أيام") {
                return val * 1440;
            }
            if (unit.startsWith("h") || unit === "ساعة" || unit === "ساعات") {
                return val * 60;
            }
            return val;
        }
        return null;
    }

    function tryParseNumber(str) {
        if (!str) return null;
        let s = str.replace(/[$,€£¥SARر.س%\s]/gi, "").replace(/,/g, "");
        if (s.startsWith("#")) s = s.substring(1);
        if (/^-?\d+(\.\d+)?$/.test(s)) {
            const val = parseFloat(s);
            if (!isNaN(val)) return val;
        }
        return null;
    }

    function tryParseDate(str) {
        if (!str || str.length < 6) return null;
        if (/^\d+$/.test(str.trim())) return null;

        // ISO format YYYY-MM-DD or YYYY/MM/DD
        const isoMatch = str.match(/^(\d{4})[-/](\d{1,2})[-/](\d{1,2})(?:\s+(\d{1,2}):(\d{1,2})(?::(\d{1,2}))?)?/);
        if (isoMatch) {
            const year = parseInt(isoMatch[1], 10);
            const month = parseInt(isoMatch[2], 10) - 1;
            const day = parseInt(isoMatch[3], 10);
            const hour = isoMatch[4] ? parseInt(isoMatch[4], 10) : 0;
            const min = isoMatch[5] ? parseInt(isoMatch[5], 10) : 0;
            const sec = isoMatch[6] ? parseInt(isoMatch[6], 10) : 0;
            const d = new Date(year, month, day, hour, min, sec);
            if (!isNaN(d.getTime())) return d.getTime();
        }

        if (/(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)/i.test(str)) {
            const t = Date.parse(str);
            if (!isNaN(t)) return t;
        }

        const t = Date.parse(str);
        if (!isNaN(t) && isNaN(str)) {
            return t;
        }
        return null;
    }

    function getCellValue(tr, colIndex) {
        const cell = tr.children[colIndex];
        if (!cell) return "";

        if (cell.hasAttribute("data-sort-value")) return cell.getAttribute("data-sort-value").trim();
        if (cell.hasAttribute("data-order")) return cell.getAttribute("data-order").trim();
        if (cell.hasAttribute("data-value")) return cell.getAttribute("data-value").trim();

        const input = cell.querySelector("input:not([type=checkbox]):not([type=hidden]), select");
        if (input) return (input.value || "").trim();

        return (cell.innerText || cell.textContent || "").trim();
    }

    function compareCells(a, b, colIndex, dir) {
        const rawA = getCellValue(a, colIndex);
        const rawB = getCellValue(b, colIndex);

        const isEmptyA = rawA === "" || rawA === "-" || rawA === "N/A" || rawA === "n/a";
        const isEmptyB = rawB === "" || rawB === "-" || rawB === "N/A" || rawB === "n/a";
        if (isEmptyA && isEmptyB) return 0;
        if (isEmptyA) return 1;
        if (isEmptyB) return -1;

        const prioA = tryParsePriority(rawA);
        const prioB = tryParsePriority(rawB);
        if (prioA !== null && prioB !== null) {
            return dir === "asc" ? prioA - prioB : prioB - prioA;
        }

        const durA = tryParseDuration(rawA);
        const durB = tryParseDuration(rawB);
        if (durA !== null && durB !== null) {
            return dir === "asc" ? durA - durB : durB - durA;
        }

        const numA = tryParseNumber(rawA);
        const numB = tryParseNumber(rawB);
        if (numA !== null && numB !== null) {
            return dir === "asc" ? numA - numB : numB - numA;
        }

        const dateA = tryParseDate(rawA);
        const dateB = tryParseDate(rawB);
        if (dateA !== null && dateB !== null) {
            return dir === "asc" ? dateA - dateB : dateB - dateA;
        }

        const cmp = rawA.localeCompare(rawB, undefined, { numeric: true, sensitivity: "base" });
        return dir === "asc" ? cmp : -cmp;
    }

    function isIgnoredRow(row, totalCols) {
        if (!row) return true;
        if (row.classList.contains("no-sort-row") || row.classList.contains("table-summary") || row.classList.contains("total-row")) {
            return true;
        }
        const tdColspan = row.querySelector("td[colspan]");
        if (tdColspan) {
            const span = parseInt(tdColspan.getAttribute("colspan"), 10);
            if (span >= 3 || (totalCols && span >= Math.ceil(totalCols / 2))) {
                return true;
            }
        }
        return false;
    }

    function isActionsHeader(th) {
        if (th.classList.contains("no-sort") || th.classList.contains("col-actions") || th.hasAttribute("data-no-sort") || th.getAttribute("data-sort") === "false") {
            return true;
        }
        const text = (th.innerText || th.textContent || "").trim().toLowerCase();
        if (text === "actions" || text === "action" || text === "إجراءات" || text === "إجراء" || text === "عمليات") {
            return true;
        }
        if (th.querySelector("input[type=checkbox]") && text === "") {
            return true;
        }
        return false;
    }

    function decorateTableHeaders(container) {
        const root = container || document;
        const tables = root.querySelectorAll("table.table");
        tables.forEach(table => {
            if (table.classList.contains("no-sort") || table.hasAttribute("data-no-sort") || table.id === "fieldsTable" || table.id === "partsTable") {
                return;
            }
            const thead = table.querySelector("thead");
            if (!thead) return;

            const thList = thead.querySelectorAll("tr:last-child th");
            thList.forEach(th => {
                if (isActionsHeader(th)) return;

                if (!th.classList.contains("sortable-th")) {
                    th.classList.add("sortable-th");
                    th.setAttribute("role", "columnheader");
                    th.setAttribute("tabindex", "0");
                    if (!th.hasAttribute("aria-sort")) {
                        th.setAttribute("aria-sort", "none");
                    }
                    if (!th.getAttribute("title")) {
                        th.setAttribute("title", isArabic ? "اضغط للترتيب" : "Click to sort");
                    }
                }

                if (!th.querySelector(".sort-icon-box")) {
                    const iconBox = document.createElement("span");
                    iconBox.className = "sort-icon-box";
                    iconBox.innerHTML = '<i class="fa-solid fa-sort"></i>';
                    th.appendChild(iconBox);
                }
            });
        });
    }

    function sortTableByColumn(th) {
        const table = th.closest("table");
        if (!table) return;
        const tbody = table.querySelector("tbody");
        if (!tbody) return;

        const headerRow = th.closest("tr");
        const allThs = Array.from(headerRow.children);
        const colIndex = allThs.indexOf(th);
        if (colIndex === -1) return;

        const currentDir = th.getAttribute("data-sort-order");
        const nextDir = currentDir === "asc" ? "desc" : "asc";

        // Update headers visual state
        allThs.forEach(otherTh => {
            if (otherTh === th) {
                otherTh.setAttribute("data-sort-order", nextDir);
                otherTh.setAttribute("aria-sort", nextDir === "asc" ? "ascending" : "descending");
                otherTh.setAttribute("title", nextDir === "asc"
                    ? (isArabic ? "مرتب تصاعدياً (اضغط للترتيب تنازلياً)" : "Sorted ascending (click to sort descending)")
                    : (isArabic ? "مرتب تنازلياً (اضغط للترتيب تصاعدياً)" : "Sorted descending (click to sort ascending)"));
                const icon = otherTh.querySelector(".sort-icon-box i");
                if (icon) {
                    icon.className = nextDir === "asc"
                        ? "fa-solid fa-sort-up text-primary"
                        : "fa-solid fa-sort-down text-primary";
                }
            } else if (otherTh.classList.contains("sortable-th")) {
                otherTh.removeAttribute("data-sort-order");
                otherTh.setAttribute("aria-sort", "none");
                otherTh.setAttribute("title", isArabic ? "اضغط للترتيب" : "Click to sort");
                const icon = otherTh.querySelector(".sort-icon-box i");
                if (icon) {
                    icon.className = "fa-solid fa-sort text-muted opacity-25";
                }
            }
        });

        // Filter data rows vs ignored rows (e.g. empty states, footers)
        const allRows = Array.from(tbody.querySelectorAll(":scope > tr"));
        const dataRows = [];
        const ignoredRows = [];

        allRows.forEach(row => {
            if (isIgnoredRow(row, allThs.length)) {
                ignoredRows.push(row);
            } else {
                dataRows.push(row);
            }
        });

        if (dataRows.length <= 1) return;

        // Assign stable index
        dataRows.forEach((r, idx) => {
            if (r.dataset.initialIndex === undefined) {
                r.dataset.initialIndex = idx;
            }
        });

        // Sort rows
        dataRows.sort((a, b) => {
            const res = compareCells(a, b, colIndex, nextDir);
            if (res !== 0) return res;
            return parseInt(a.dataset.initialIndex, 10) - parseInt(b.dataset.initialIndex, 10);
        });

        // Re-append to tbody with DocumentFragment for performance
        const frag = document.createDocumentFragment();
        dataRows.forEach(r => frag.appendChild(r));
        ignoredRows.forEach(r => frag.appendChild(r));
        tbody.appendChild(frag);

        table.dispatchEvent(new CustomEvent("tablesorted", {
            bubbles: true,
            detail: { column: colIndex, direction: nextDir }
        }));
    }

    function initTableSorting() {
        decorateTableHeaders(document);

        // Click handler with event delegation
        document.addEventListener("click", function (e) {
            const th = e.target.closest("th");
            if (!th || !th.classList.contains("sortable-th")) return;

            // Ignore clicks directly inside interactive elements (checkboxes, inputs, links, buttons)
            if (e.target.closest("input, button, a, select, .dropdown-menu, label")) {
                return;
            }

            e.preventDefault();
            sortTableByColumn(th);
        });

        // Keyboard accessibility: Enter or Space on focused header
        document.addEventListener("keydown", function (e) {
            if (e.key === "Enter" || e.key === " ") {
                const th = document.activeElement;
                if (th && th.tagName === "TH" && th.classList.contains("sortable-th")) {
                    e.preventDefault();
                    sortTableByColumn(th);
                }
            }
        });

        // Dynamic decoration on tab switch
        document.addEventListener("shown.bs.tab", function (e) {
            const targetSelector = e.target.getAttribute("data-bs-target");
            if (targetSelector) {
                const pane = document.querySelector(targetSelector);
                if (pane) decorateTableHeaders(pane);
            }
        });

        // MutationObserver to decorate dynamically injected tables
        if (window.MutationObserver) {
            let debounceTimer = null;
            const observer = new MutationObserver(function () {
                clearTimeout(debounceTimer);
                debounceTimer = setTimeout(function () {
                    decorateTableHeaders(document);
                }, 100);
            });
            observer.observe(document.body, { childList: true, subtree: true });
        }
    }

    // Expose sort helper globally
    window.sortPortalTable = function (thElement) {
        if (thElement) sortTableByColumn(thElement);
    };

    /* ==========================================================================
       6b. DYNAMIC TRANSPARENT BRAND VIDEO PROCESSOR
       Renders the animated brand video onto a transparent canvas, automatically
       stripping dark backgrounds or checkerboard grids in real-time.
       ========================================================================== */
    function initTransparentBrandVideo() {
        const video = document.getElementById("solvisBrandVideo");
        const canvas = document.getElementById("solvisBrandCanvas");
        if (!video || !canvas) return;

        const ctx = canvas.getContext("2d", { willReadFrequently: true });
        if (!ctx) return;

        // Render transparent fallback logo immediately so there's zero blank flash
        const poster = new Image();
        poster.onload = function () {
            if (canvas.width <= 300) {
                canvas.width = poster.naturalWidth || 808;
                canvas.height = poster.naturalHeight || 324;
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                ctx.drawImage(poster, 0, 0, canvas.width, canvas.height);
            }
        };
        poster.src = "/images/solvis-logo-transparent.png";

        let animId = null;

        function renderFrame() {
            if (video.readyState >= 2 && !video.paused && !video.ended) {
                const vw = video.videoWidth || 300;
                const vh = video.videoHeight || 169;

                if (canvas.width !== vw || canvas.height !== vh) {
                    canvas.width = vw;
                    canvas.height = vh;
                }

                ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
                const imgData = ctx.getImageData(0, 0, canvas.width, canvas.height);
                const data = imgData.data;
                const len = data.length;

                // Inspect top-left corner to determine background type
                const cR = data[0], cG = data[1], cB = data[2];
                const isLightBg = cR > 180 && cG > 180 && cB > 180;

                if (isLightBg) {
                    // Key out light / checkerboard pixels
                    for (let i = 0; i < len; i += 4) {
                        const r = data[i], g = data[i + 1], b = data[i + 2];
                        const min = Math.min(r, g, b);
                        const max = Math.max(r, g, b);
                        const sat = max - min;
                        if (min > 210 && sat < 22) {
                            data[i + 3] = 0; // Transparent
                        } else if (min > 190 && sat < 18) {
                            data[i + 3] = Math.round(((210 - min) / 20) * 255);
                        }
                    }
                } else {
                    // Key out dark background
                    for (let i = 0; i < len; i += 4) {
                        const r = data[i], g = data[i + 1], b = data[i + 2];
                        const lum = 0.299 * r + 0.587 * g + 0.114 * b;
                        if (lum < 28) {
                            data[i + 3] = 0; // 100% transparent
                        } else if (lum < 60) {
                            data[i + 3] = Math.min(255, Math.round(((lum - 28) / 32) * 255));
                        }
                    }
                }

                ctx.putImageData(imgData, 0, 0);
            }
            animId = requestAnimationFrame(renderFrame);
        }

        video.addEventListener("play", function () {
            if (!animId) animId = requestAnimationFrame(renderFrame);
        });

        video.addEventListener("loadeddata", function () {
            if (!animId) animId = requestAnimationFrame(renderFrame);
        });

        if (video.paused) {
            video.play().catch(function () {});
        } else {
            animId = requestAnimationFrame(renderFrame);
        }
    }

    /* ==========================================================================
       7. INITIALIZATION ON DOM READY
       ========================================================================== */
    document.addEventListener("DOMContentLoaded", function () {
        initModalFixes();
        initTheme();
        initSidebar();
        initTransparentBrandVideo();
        initPageTransitions();
        initTableSorting();
        checkUnreadNotifications();
        setInterval(checkUnreadNotifications, 8000);
    });

    // Also register modal fix immediately in case modals are initialized early
    initModalFixes();

})();
