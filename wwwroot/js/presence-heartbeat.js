/**
 * Presence Heartbeat — يسجل المستخدم كـ "نشط" كل 30 ثانية
 * متوافق مع نسخة JS (نفس الـ endpoint والـ payload)
 */
(function () {
    'use strict';

    const HEARTBEAT_INTERVAL = 30 * 1000; // 30 ثانية
    const HEARTBEAT_ENDPOINT = '/Presence/Heartbeat';
    const SLOW_HEARTBEAT_MS = 100;

    async function sendHeartbeat() {
        const startedAt = performance.now();
        try {
            const response = await fetch(HEARTBEAT_ENDPOINT, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                credentials: 'same-origin'
            });

            const elapsedMs = performance.now() - startedAt;
            if (elapsedMs >= SLOW_HEARTBEAT_MS) {
                console.warn('[Presence PERF] Slow heartbeat request', {
                    elapsedMs: Math.round(elapsedMs),
                    status: response.status
                });
            }

            if (response.ok) {
                // console.log('[Presence] Heartbeat OK');
            }
        } catch (err) {
            // صامت — عادي لو النت قطع
        }
    }

    // أول heartbeat بعد 3 ثواني
    setTimeout(sendHeartbeat, 3000);

    // بعدها كل 30 ثانية
    setInterval(sendHeartbeat, HEARTBEAT_INTERVAL);

    // heartbeat إضافي لما المستخدم يرجع للتاب
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') {
            sendHeartbeat();
        }
    });

    console.log('[Presence] Heartbeat initialized');
})();