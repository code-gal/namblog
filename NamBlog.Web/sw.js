/**
 * Service Worker - NamBlog PWA
 *
 * 缓存策略:
 * - 静态资源(JS/CSS/字体): 本地优先 + 后台更新（SWR）
 * - HTML页面: 网络优先，失败时用缓存
 * - API请求: 仅网络
 * - 文章静态HTML(/posts/*): 缓存优先
 */

const CACHE_VERSION = '0.10.4';
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

// 安装事件：预缓存核心资源（强制绕过 HTTP 缓存，确保拿到最新文件）
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(async cache => {
            await Promise.all(
                CORE_ASSETS.map(async (url) => {
                    const request = new Request(url, { cache: 'reload' });
                    const response = await fetch(request);
                    if (response && response.ok) {
                        await cache.put(request, response);
                    }
                })
            );
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
        // 本地先用；后台在线更新缓存（下次请求生效）
        event.respondWith(staleWhileRevalidate(request, event, { forceRefresh: true }));
        return;
    }

    // 静态资源 (JS/CSS/字体/图标): 本地优先 + 后台更新
    if (isStaticAsset(url.pathname)) {
        event.respondWith(staleWhileRevalidate(request, event));
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
        if (response.ok && request.method === 'GET') {
            cache.put(request, response.clone());
        }

        return response;
    } catch (error) {
        console.error('[SW] 请求失败:', request.url, error);
        throw error;
    }
}

/**
 * Stale-While-Revalidate（本地优先 + 后台更新）
 * - 命中缓存：立即返回缓存，同时后台 fetch 更新缓存
 * - 未命中：走网络；成功则写入缓存
 *
 * 适用场景：/resources/* 这类“可先显示旧资源，但希望在线时自动变新”的静态文件。
 */
async function staleWhileRevalidate(request, event, { forceRefresh = false } = {}) {
    const cache = await caches.open(CACHE_NAME);
    const cached = await cache.match(request);

    const fetchAndUpdate = (async () => {
        try {
            const fetchRequest = forceRefresh
                ? new Request(request, { cache: 'no-store' })
                : request;

            const response = await fetch(fetchRequest);

            // 只缓存成功的 GET 响应，避免污染缓存
            if (response && response.ok && request.method === 'GET') {
                await cache.put(request, response.clone());
            }

            return response;
        } catch (error) {
            return null;
        }
    })();

    if (cached) {
        // 不阻塞返回；让更新在后台跑
        if (event && typeof event.waitUntil === 'function') {
            event.waitUntil(fetchAndUpdate);
        }
        return cached;
    }

    const response = await fetchAndUpdate;
    if (response) {
        return response;
    }

    throw new Error(`[SW] 请求失败: ${request.url}`);
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

        // 缓存成功的 GET 响应
        if (response.ok && request.method === 'GET') {
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
    if (!event.data || !event.data.type) {
        return;
    }

    if (event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
        return;
    }

    // 删除指定 URL 的缓存（用于删除文章版本后清理 /posts/* 的缓存）
    if (event.data.type === 'CACHE_DELETE_URLS') {
        const urls = Array.isArray(event.data.urls) ? event.data.urls : [];
        event.waitUntil(deleteCachedUrls(urls));
        return;
    }

    // 删除匹配前缀的缓存（用于删除整篇文章后清理 /posts/{slug}/*）
    if (event.data.type === 'CACHE_DELETE_PREFIXES') {
        const prefixes = Array.isArray(event.data.prefixes) ? event.data.prefixes : [];
        event.waitUntil(deleteCachedByPathPrefixes(prefixes));
    }
});

async function deleteCachedUrls(urls) {
    if (!urls.length) {
        return;
    }

    const cacheNames = await caches.keys();
    const targetCaches = cacheNames.filter(name => name.startsWith('namblog-'));

    await Promise.all(
        targetCaches.map(async cacheName => {
            const cache = await caches.open(cacheName);

            for (const url of urls) {
                try {
                    const absoluteUrl = new URL(url, self.location.origin).toString();
                    await cache.delete(absoluteUrl);
                } catch {
                    // ignore bad url
                }
            }
        })
    );
}

async function deleteCachedByPathPrefixes(prefixes) {
    if (!prefixes.length) {
        return;
    }

    const cacheNames = await caches.keys();
    const targetCaches = cacheNames.filter(name => name.startsWith('namblog-'));

    await Promise.all(
        targetCaches.map(async cacheName => {
            const cache = await caches.open(cacheName);
            const requests = await cache.keys();

            await Promise.all(
                requests.map(req => {
                    try {
                        const url = new URL(req.url);
                        const matched = prefixes.some(prefix => url.pathname.startsWith(prefix));
                        return matched ? cache.delete(req) : Promise.resolve(false);
                    } catch {
                        return Promise.resolve(false);
                    }
                })
            );
        })
    );
}






