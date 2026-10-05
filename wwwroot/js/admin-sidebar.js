/**
 * Admin Sidebar — Toggle + Mobile + Persistence
 */
(function () {
    'use strict';

    const STORAGE_KEY = 'bm_sidebar_collapsed';
    const MOBILE_BREAKPOINT = 992;

    const body = document.body;
    const toggleBtn = document.getElementById('sidebarToggle');
    const overlay = document.getElementById('sidebarOverlay');

    // ============================================================
    // 1. Restore collapsed state (Desktop only)
    // ============================================================
    function isMobile() {
        return window.innerWidth < MOBILE_BREAKPOINT;
    }

    function restoreState() {
        if (isMobile()) return;

        const collapsed = localStorage.getItem(STORAGE_KEY) === 'true';
        if (collapsed) {
            body.classList.add('sidebar-collapsed');
        }
    }

    // ============================================================
    // 2. Toggle Sidebar
    // ============================================================
    function toggleSidebar() {
        if (isMobile()) {
            // Mobile: open/close drawer
            body.classList.toggle('sidebar-mobile-open');
        } else {
            // Desktop: collapse/expand
            const collapsed = body.classList.toggle('sidebar-collapsed');
            localStorage.setItem(STORAGE_KEY, collapsed ? 'true' : 'false');
        }
    }

    // ============================================================
    // 3. Close mobile drawer
    // ============================================================
    function closeMobileDrawer() {
        body.classList.remove('sidebar-mobile-open');
    }

    // ============================================================
    // 4. Event Listeners
    // ============================================================
    if (toggleBtn) {
        toggleBtn.addEventListener('click', toggleSidebar);
    }

    if (overlay) {
        overlay.addEventListener('click', closeMobileDrawer);
    }

    // Close on link click (mobile)
    document.querySelectorAll('.sidebar-nav .nav-link').forEach(link => {
        link.addEventListener('click', () => {
            if (isMobile()) closeMobileDrawer();
        });
    });

    // Close on ESC
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && body.classList.contains('sidebar-mobile-open')) {
            closeMobileDrawer();
        }
    });

    // Handle resize
    let resizeTimer;
    window.addEventListener('resize', () => {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(() => {
            if (!isMobile()) {
                closeMobileDrawer();
            }
        }, 150);
    });

    // ============================================================
    // 5. Init
    // ============================================================
    restoreState();
})();