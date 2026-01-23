/**
 * 认证处理工具
 * 提供统一的认证失败处理逻辑
 */
import { showToast } from '../components/Toast.js';

/**
 * 处理认证失败：清除登录状态并跳转到登录页
 */
export function handleAuthenticationFailure() {
    // 清除登录状态
    localStorage.removeItem('auth_token');
    
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
