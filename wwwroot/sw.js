// ============================================================
// Service Worker — نظام إدارة العمارات
// ============================================================
const CACHE_NAME = 'bm-v1';
const OFFLINE_URL = '/offline.html';

// الملفات اللي بنخزنها في الـ cache من الأول
const PRECACHE_URLS = [
    '/',
    '/offline.html',
    '/css/site.css',
    '/icons/icon-192.png',
    '/icons/icon-512.png',
    '/manifest.json'
];

// ============================================================
// Install
// ============================================================
self.addEventListener('install', event => {
    console.log('[SW] Installing...');
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                console.log('[SW] Pre-caching:', PRECACHE_URLS);
                return cache.addAll(PRECACHE_URLS.map(url => new Request(url, { credentials: 'same-origin' })));
            })
            .then(() => self.skipWaiting())
            .catch(err => console.error('[SW] Pre-cache failed:', err))
    );
});

// ============================================================
// Activate
// ============================================================
self.addEventListener('activate', event => {
    console.log('[SW] Activating...');
    event.waitUntil(
        caches.keys()
            .then(cacheNames => {
                return Promise.all(
                    cacheNames
                        .filter(name => name !== CACHE_NAME)
                        .map(name => {
                            console.log('[SW] Deleting old cache:', name);
                            return caches.delete(name);
                        })
                );
            })
            .then(() => self.clients.claim())
    );
});

// ============================================================
// Fetch — استراتيجية:
// - CSS/JS/صور → Cache First
// - HTML → Network First (مع fallback للـ cache، وبعدين offline.html)
// - باقي الطلبات → Network Only (عشان Firestore يشتغل صح)
// ============================================================
self.addEventListener('fetch', event => {
    const { request } = event;
    const url = new URL(request.url);

    // ⚠️ تجاهل الطلبات الخارجية (Firebase, Cloudinary, إلخ)
    if (url.origin !== location.origin) return;

    // ⚠️ تجاهل طلبات POST/PUT/DELETE (مش بتتخزن)
    if (request.method !== 'GET') return;

    // ⚠️ تجاهل طلبات الـ API + Firebase
    if (url.pathname.startsWith('/api/') ||
        url.pathname.startsWith('/Account/') ||
        url.pathname.startsWith('/Settings/') ||
        url.pathname.startsWith('/Wallet/') ||
        url.pathname.startsWith('/Reports/')) {
        return; // Network Only
    }

    // CSS, JS, Fonts, Images → Cache First
    if (request.destination === 'style' ||
        request.destination === 'script' ||
        request.destination === 'font' ||
        request.destination === 'image') {
        event.respondWith(cacheFirst(request));
        return;
    }

    // HTML pages → Network First (مع fallback)
    if (request.destination === 'document') {
        event.respondWith(networkFirstWithOfflineFallback(request));
        return;
    }
});

// ============================================================
// Strategies
// ============================================================
async function cacheFirst(request) {
    const cache = await caches.open(CACHE_NAME);
    const cached = await cache.match(request);
    if (cached) return cached;

    try {
        const response = await fetch(request);
        if (response.ok) {
            cache.put(request, response.clone());
        }
        return response;
    } catch (err) {
        console.warn('[SW] cacheFirst failed:', request.url, err);
        return new Response('', { status: 503, statusText: 'Offline' });
    }
}

async function networkFirstWithOfflineFallback(request) {
    try {
        const response = await fetch(request);
        // نحدّث الـ cache لو الطلب نجح
        if (response.ok) {
            const cache = await caches.open(CACHE_NAME);
            cache.put(request, response.clone());
        }
        return response;
    } catch (err) {
        console.warn('[SW] networkFirst failed, trying cache:', request.url);
        const cache = await caches.open(CACHE_NAME);
        const cached = await cache.match(request);
        if (cached) return cached;

        // آخر حل: صفحة offline
        return caches.match(OFFLINE_URL);
    }
}

// ============================================================
// رسالة من الـ page (لتحديث الـ SW)
// ============================================================
self.addEventListener('message', event => {
    if (event.data === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});