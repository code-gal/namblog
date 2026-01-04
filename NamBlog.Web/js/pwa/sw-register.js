/**
 * Service Worker 注册与管理
 * 负责注册、更新检测、安装提示
 */

// 注册 Service Worker
export async function registerServiceWorker() {
    if (!('serviceWorker' in navigator)) {
        return null;
    }

    try {
        const registration = await navigator.serviceWorker.register('/sw.js', {
            scope: '/'
        });

        // 监听更新
        registration.addEventListener('updatefound', () => {
            const newWorker = registration.installing;

            newWorker.addEventListener('statechange', () => {
                if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                    // 新版本已安装，但旧版本仍在运行
                    notifyUpdate(newWorker);
                }
            });
        });

        // 定期检查更新（每小时）
        setInterval(() => {
            registration.update();
        }, 60 * 60 * 1000);

        return registration;
    } catch (error) {
        console.error('[PWA] Service Worker 注册失败:', error);
        return null;
    }
}

/**
 * 通知用户有新版本可用
 */
function notifyUpdate(newWorker) {
    // MVP版本：简单的控制台提示
    // 后续可扩展为UI提示框

    // 可选：自动提示用户
    if (confirm('发现新版本！是否立即刷新页面？')) {
        newWorker.postMessage({ type: 'SKIP_WAITING' });
        window.location.reload();
    }
}

/**
 * 监听网络状态变化
 */
export function setupNetworkListener() {
    window.addEventListener('online', () => {
        // 可选：显示提示
    });

    window.addEventListener('offline', () => {
        // 可选：显示提示
    });
}

/**
 * 检测是否可以安装 PWA
 */
export function setupInstallPrompt() {
    let deferredPrompt = null;

    window.addEventListener('beforeinstallprompt', (e) => {
        // 阻止默认的安装提示
        e.preventDefault();
        deferredPrompt = e;

        // MVP版本：不显示UI，用户可通过浏览器菜单安装
        // 后续可扩展为自定义安装提示UI
    });

    window.addEventListener('appinstalled', () => {
        deferredPrompt = null;
    });

    return deferredPrompt;
}

/**
 * 初始化 PWA 功能
 */
export async function initPWA() {
    // 开发模式下禁用 PWA（Live Server 环境下 Service Worker 路径会有问题）
    if (window.APP_CONFIG && window.APP_CONFIG.DEV_MODE) {
        console.log('[PWA] 开发模式：已禁用 PWA 功能');
        return null;
    }

    // 注册 Service Worker
    const registration = await registerServiceWorker();

    // 设置网络状态监听
    setupNetworkListener();

    // 设置安装提示
    setupInstallPrompt();

    return registration;
}
