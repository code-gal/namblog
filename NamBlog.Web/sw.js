/**
 * Service Worker - NamBlog PWA
 * 版本: 1.0.0
 *
 * 缓存策略:
 * - 静态资源(JS/CSS/字体): 缓存优先
 * - HTML页面: 网络优先，失败时用缓存
 * - API请求: 仅网络
 * - 文章静态HTML(/posts/*): 缓存优先
 */

const CACHE_VERSION = '1.0.0';
const CACHE_NAME = `namblog-v${CACHE_VERSION}`;
const OFFLINE_URL = '/offline.html';

// 核心静态资源（安装时预缓存）
const CORE_ASSETS = [
    '/',
    '/index.html',
    '/offline.html',
    '/manifest.json',
    '/css/style.css',

    // Vue 相关库
    '/libs/vue/vue.esm-browser.prod.js',
    '/libs/vue/vue-router.esm-browser.prod.js',
    '/libs/vue/index.js',

    // Tailwind CSS
    '/libs/tailwindcss/tailwind.min.js',

    // EasyMDE
    '/libs/easymde/easymde.min.js',
    '/libs/easymde/easymde.min.css',

    // 主要JS文件
    '/js/main.js',
    '/js/App.js',
    '/js/config.js',
    '/js/store.js',
    '/js/api/client.js',
    '/js/api/auth.js',
    '/js/api/articleApi.js',
];

// 安装事件：预缓存核心资源
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            return cache.addAll(CORE_ASSETS);
        }).then(() => {
            // 立即激活新的 Service Worker
            return self.skipWaiting();
        })
    );
});

// 激活事件：清理旧缓存
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames
                    .filter(name => name.startsWith('namblog-') && name !== CACHE_NAME)
                    .map(name => {
                        return caches.delete(name);
                    })
            );
        }).then(() => {
            // 立即控制所有客户端
            return self.clients.claim();
        })
    );
});

// 拦截请求：应用缓存策略
self.addEventListener('fetch', event => {
    const { request } = event;
    const url = new URL(request.url);

    // 只处理同源请求
    if (url.origin !== location.origin) {
        return;
    }

    // API 请求：仅网络（不缓存动态数据）
    if (url.pathname.startsWith('/graphql')) {
        event.respondWith(fetch(request));
        return;
    }

    // 文章静态HTML (/posts/*): 缓存优先
    if (url.pathname.startsWith('/posts/')) {
        event.respondWith(cacheFirst(request));
        return;
    }

    // 资源文件 (/resources/*): 缓存优先
    if (url.pathname.startsWith('/resources/')) {
        event.respondWith(cacheFirst(request));
        return;
    }

    // 静态资源 (JS/CSS/字体/图标): 缓存优先
    if (isStaticAsset(url.pathname)) {
        event.respondWith(cacheFirst(request));
        return;
    }

    // HTML 页面: 网络优先，失败时用缓存或离线页
    if (request.mode === 'navigate') {
        event.respondWith(networkFirst(request));
        return;
    }

    // 其他请求: 网络优先
    event.respondWith(networkFirst(request));
});

/**
 * 缓存优先策略
 * 命中缓存 → 立即返回
 * 未命中 → 网络请求 → 存入缓存
 */
async function cacheFirst(request) {
    const cache = await caches.open(CACHE_NAME);
    const cached = await cache.match(request);

    if (cached) {
        return cached;
    }

    try {
        const response = await fetch(request);

        // 只缓存成功的响应
        if (response.ok) {
            cache.put(request, response.clone());
        }

        return response;
    } catch (error) {
        console.error('[SW] 请求失败:', request.url, error);
        throw error;
    }
}

/**
 * 网络优先策略
 * 网络成功 → 返回最新 → 更新缓存
 * 网络失败 → 返回缓存（或离线页）
 */
async function networkFirst(request) {
    const cache = await caches.open(CACHE_NAME);

    try {
        const response = await fetch(request);

        // 缓存成功的响应
        if (response.ok) {
            cache.put(request, response.clone());
        }

        return response;
    } catch (error) {
        // 尝试返回缓存
        const cached = await cache.match(request);
        if (cached) {
            return cached;
        }

        // 如果是页面导航请求，返回离线页
        if (request.mode === 'navigate') {
            const offlinePage = await cache.match(OFFLINE_URL);
            if (offlinePage) {
                return offlinePage;
            }
        }

        throw error;
    }
}

/**
 * 判断是否为静态资源
 */
function isStaticAsset(pathname) {
    const staticExtensions = ['.js', '.css', '.woff', '.woff2', '.ttf', '.otf', '.eot', '.png', '.jpg', '.jpeg', '.gif', '.svg', '.ico'];
    return staticExtensions.some(ext => pathname.endsWith(ext));
}

/**
 * 监听消息（用于手动触发缓存清理等操作）
 */
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});
