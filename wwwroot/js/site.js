// ============================================================
// Building Management — Site-wide helpers
// ============================================================
(function () {
    'use strict';

    // ============================================================
    // Global helpers — متاحة لكل السكربتات
    // ============================================================
    window.bmHelpers = {
        // جلب Anti-Forgery Token من أي صفحة
        getAntiForgeryToken() {
            const el = document.querySelector('input[name="__RequestVerificationToken"]');
            return el ? el.value : '';
        },

        // تنسيق العملة
        formatCurrency(n) {
            return new Intl.NumberFormat('ar-EG', {
                minimumFractionDigits: 2,
                maximumFractionDigits: 2
            }).format(n);
        },

        // Confirm Dialog موحّد
        confirm(message) {
            return window.confirm(message);
        }
    };

    // ============================================================
    // Bootstrap Tooltips — تفعيل تلقائي
    // ============================================================
    function initTooltips() {
        if (typeof bootstrap === 'undefined') return;
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
            // تجنب التكرار
            if (el._tooltipInitialized) return;
            new bootstrap.Tooltip(el);
            el._tooltipInitialized = true;
        });
    }

    // شغّل بعد التحميل
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initTooltips);
    } else {
        initTooltips();
    }

    // ============================================================
    // Auto-dismiss Alerts بعد 6 ثواني
    // ============================================================
    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('.alert-dismissible').forEach(alert => {
            setTimeout(() => {
                if (alert.parentElement) {
                    const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
                    bsAlert.close();
                }
            }, 6000);
        });
    });

    console.log('[BM] site.js loaded');
})();