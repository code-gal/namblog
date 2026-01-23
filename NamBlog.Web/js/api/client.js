import { config } from '../config.js';
import { getLocale } from '../i18n/index.js';
import { i18n } from '../i18n/index.js';
import { showToast } from '../components/Toast.js';
import { store } from '../store.js';

/**
 * 处理认证失败：清除登录状态，根据页面类型决定是否跳转
 */
let isAuthHandling = false;

function hasAuthToken() {
    return !!localStorage.getItem('auth_token');
}

function getCurrentRoutePath() {
    const hash = window.location.hash;
    if (hash && hash.startsWith('#/')) {
        return hash.slice(1).split('?')[0];
    }
    return window.location.pathname;
}

function handleAuthenticationFailure() {
    if (isAuthHandling) return;
    isAuthHandling = true;

    // 清除登录状态
    store.setToken(null);
    store.setUser(null);

    const routePath = getCurrentRoutePath();

    // 判断是否在需要登录的页面（需要显示提示并跳转）
    // 包括：编辑器、查看非公开文章（只有非公开文章才会触发认证错误）
    const requiresAuthPaths = ['/editor', '/article'];
    const isProtectedPage = requiresAuthPaths.some(path => routePath.startsWith(path));

    if (isProtectedPage) {
        // 非公开页面：显示友好提示并跳转
        showToast(i18n.global.t('auth.tokenExpired'), 'warning', 3000);

        const fullPath = window.location.pathname + window.location.search + window.location.hash;
        setTimeout(() => {
            window.location.href = `/login?redirect=${encodeURIComponent(fullPath)}`;
        }, 500);
    }
    // 公开页面（列表、文章详情等）：静默清除，不提示，不跳转

    // 重置标志，允许下次处理
    setTimeout(() => { isAuthHandling = false; }, 1000);
}

/**
 * Generic GraphQL request function
 * @param {string} query - GraphQL query or mutation
 * @param {object} [variables] - Variables for the query
 * @param {AbortSignal} [signal] - Optional abort signal for request cancellation
 * @returns {Promise<any>} - The data property of the response
 */
export async function request(query, variables = {}, signal = null) {
    const headers = {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        'Accept-Language': getLocale(), // Add language preference header
    };

    const token = localStorage.getItem('auth_token');
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    try {
        const fetchOptions = {
            method: 'POST',
            headers: headers,
            body: JSON.stringify({ query, variables }),
        };

        // Add abort signal if provided
        if (signal) {
            fetchOptions.signal = signal;
        }

        const response = await fetch(config.GRAPHQL_ENDPOINT, fetchOptions);

        // 检查 HTTP 状态码
        if (response.status === 401) {
            if (hasAuthToken()) {
                handleAuthenticationFailure();
                throw new Error(i18n.global.t('auth.tokenExpired'));
            }
            throw new Error(i18n.global.t('auth.unauthorized'));
        }

        const result = await response.json();

        if (result.errors) {
            console.error('GraphQL Errors:', result.errors);

            // 检查 GraphQL 错误中是否包含认证失败信息
            const authError = result.errors.find(err =>
                err.extensions?.code === 'ACCESS_DENIED' ||  // Mutation 授权失败
                err.extensions?.code === 'UNAUTHORIZED' ||   // Query 授权失败
                err.message?.includes('Access denied') ||
                err.message?.includes('未授权') ||
                err.message?.includes('无权访问')
            );

            if (authError && hasAuthToken()) {
                handleAuthenticationFailure();
            }

            throw new Error(result.errors[0].message);
        }

        return result.data;
    } catch (error) {
        // Don't log abort errors as they are intentional
        if (error.name !== 'AbortError') {
            console.error('API Request Failed:', error);
        }
        throw error;
    }
}
