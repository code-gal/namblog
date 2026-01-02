/**
 * 前端配置文件
 *
 * 优先级：
 * 1. 环境变量（通过构建工具注入）
 * 2. window.APP_CONFIG（在 index.html 中定义）
 * 3. 默认值
 */

// 从环境变量或全局配置获取API基础URL
const getApiBaseUrl = () => {
    // 尝试从环境变量获取（需要构建工具支持）
    if (typeof import.meta !== 'undefined' && import.meta.env && import.meta.env.VITE_API_BASE_URL) {
        return import.meta.env.VITE_API_BASE_URL;
    }

    // 尝试从全局配置获取
    if (typeof window !== 'undefined' && window.APP_CONFIG && window.APP_CONFIG.API_BASE_URL) {
        return window.APP_CONFIG.API_BASE_URL;
    }

    // 开发环境默认值
    return 'http://localhost:5000';
};

const apiBaseUrl = getApiBaseUrl();

export const config = {
    // API基础URL
    API_BASE_URL: apiBaseUrl,

    // GraphQL端点（从 API_BASE_URL 自动生成）
    get GRAPHQL_ENDPOINT() {
        return `${apiBaseUrl}/graphql`;
    },
};
