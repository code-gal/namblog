import { config } from '../config.js';
import { getLocale } from '../i18n/index.js';
import { showToast } from '../components/Toast.js';
import { store } from '../store.js';

/**
 * 处理认证失败：清除登录状态并跳转到登录页
 */
let isAuthHandling = false;

function hasAuthToken() {
    return !!localStorage.getItem('auth_token');
}

function handleAuthenticationFailure() {
    if (isAuthHandling) return;
    isAuthHandling = true;

    // 清除登录状态
    store.setToken(null);
    store.setUser(null);

    // 显示友好提示
    showToast('登录已过期，请重新登录', 'warning', 3000);

    // 保存当前路径，用于登录后返回
    const currentPath = window.location.pathname + window.location.search + window.location.hash;

    // 延迟跳转，让用户看到提示
    setTimeout(() => {
        if (currentPath !== '/' && currentPath !== '/login') {
            window.location.href = `/login?redirect=${encodeURIComponent(currentPath)}`;
        } else {
            window.location.href = '/login';
        }
    }, 500);
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
                throw new Error('登录已过期，请重新登录');
            }
            throw new Error('未登录或无权限');
        }

        const result = await response.json();

        if (result.errors) {
            console.error('GraphQL Errors:', result.errors);

            // 检查 GraphQL 错误中是否包含认证失败信息
            const authError = result.errors.find(err =>
                err.message?.includes('Unauthorized') ||
                err.message?.includes('未授权') ||
                err.extensions?.code === 'AUTH_NOT_AUTHENTICATED'
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
