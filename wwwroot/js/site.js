/**
 * STOCK & HELPDESK MANAGEMENT PORTAL
 * Interactive Scripts: Modern Top Notifications, Page Transitions & Responsive Autohide Sidebar
 */

(function () {
    "use strict";

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
                    toggleBtn.setAttribute("title", "Pin Sidebar (Ctrl+B)");
                } else {
                    icon.className = "fa-solid fa-bars";
                    toggleBtn.setAttribute("title", "Auto-hide Sidebar (Ctrl+B)");
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

        // Classify notification type & styles
        let cardTypeClass = "type-ticket";
        let tagText = "Ticket Alert";
        let iconHtml = '<i class="fa-solid fa-ticket"></i>';

        const typeLower = (item.type || "").toLowerCase();
        if (typeLower.includes("message") || typeLower.includes("comment")) {
            cardTypeClass = "type-message";
            tagText = "New Message";
            iconHtml = '<i class="fa-solid fa-comments"></i>';
        } else if (typeLower.includes("status")) {
            cardTypeClass = "type-status";
            tagText = "Status Update";
            iconHtml = '<i class="fa-solid fa-arrows-rotate"></i>';
        } else if (typeLower.includes("urgent") || typeLower.includes("critical")) {
            cardTypeClass = "type-urgent";
            tagText = "Urgent Notice";
            iconHtml = '<i class="fa-solid fa-triangle-exclamation"></i>';
        } else if (typeLower.includes("success") || typeLower.includes("resolved") || typeLower.includes("closed")) {
            cardTypeClass = "type-success";
            tagText = "Resolved";
            iconHtml = '<i class="fa-solid fa-circle-check"></i>';
        }

        const card = document.createElement("div");
        card.id = notifId;
        card.className = "modern-notif-card " + cardTypeClass;
        card.setAttribute("role", "alert");

        const targetUrl = item.linkUrl || "/Notifications";

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
                        <span class="modern-notif-time"><i class="fa-regular fa-clock me-1"></i>${item.timeAgo || "Just now"}</span>
                    </div>
                    <div class="modern-notif-title">${item.title || "Notification"}</div>
                    <div class="modern-notif-desc">${item.message || ""}</div>
                    <div class="modern-notif-actions">
                        <a href="${targetUrl}" class="modern-notif-btn-open">
                            <span>Open Details</span>
                            <i class="fa-solid fa-arrow-right"></i>
                        </a>
                        <button type="button" class="modern-notif-btn-dismiss">Dismiss</button>
                    </div>
                </div>
                <button type="button" class="modern-notif-close-icon" aria-label="Close" title="Dismiss">
                    <i class="fa-solid fa-xmark"></i>
                </button>
            </div>
            <div class="modern-notif-progress">
                <div class="modern-notif-progress-bar"></div>
            </div>
        `;

        // Prepend so the newest notification appears on top
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
                // Mark as read in background when opening
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
        fetch("/Notifications/Dismiss/" + id, {
            method: "POST",
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            }
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
            title: customTitle || "New Service Ticket Assigned",
            message: customMessage || "Ticket #204 - Emergency CT Scanner Network Port issue has been dispatched to your queue.",
            type: customType || "TicketStatus",
            linkUrl: "/Tickets",
            timeAgo: "Just now"
        };
        playNotificationChime();
        showModernNotification(testItem);
    };

    /* ==========================================================================
       4. INITIALIZATION ON DOM READY
       ========================================================================== */
    document.addEventListener("DOMContentLoaded", function () {
        initSidebar();
        initPageTransitions();
        checkUnreadNotifications();
        setInterval(checkUnreadNotifications, 8000);
    });

})();
