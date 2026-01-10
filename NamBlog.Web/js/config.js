/**
 * 前端配置文件
 *
 * 📦 部署配置说明：
 * - 开发环境：使用默认值，可通过 config.local.js 覆盖
 * - 生产环境：（可选）通过 Docker 映射此文件来配置
 *
 */

// ==================== 配置区域（生产环境可直接修改此部分） ====================

/**
 * 语言设置
 * - 'zh-CN': 中文
 * - 'en-US': 英文
 * - null: 自动检测浏览器语言
 */
const LANGUAGE = null;

/**
 * 自定义语言包URL（可选）
 * 支持博主创建自己的语言翻译文件
 *
 * 示例：
 * const CUSTOM_LOCALE_URL = './i18n/locales/ja-JP.js';  // 日语
 * const CUSTOM_LOCALE_CODE = 'ja-JP';
 *
 * 语言文件格式参考：js/i18n/locales/zh-CN.js
 */
const CUSTOM_LOCALE_URL = null;
const CUSTOM_LOCALE_CODE = null;

// ==================== 配置区域结束 ====================

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

// 获取语言配置
const getLanguage = () => {
    // 优先使用本文件配置
    if (LANGUAGE) {
        return LANGUAGE;
    }
    // 其次使用 window.APP_CONFIG（兼容旧配置方式）
    if (typeof window !== 'undefined' && window.APP_CONFIG && window.APP_CONFIG.LANGUAGE) {
        return window.APP_CONFIG.LANGUAGE;
    }
    // 返回null表示使用自动检测
    return null;
};

export const config = {
    // API基础URL
    API_BASE_URL: apiBaseUrl,

    // 语言配置（null表示自动检测浏览器语言）
    LANGUAGE: getLanguage(),

    // 自定义语言包配置
    CUSTOM_LOCALE_URL: CUSTOM_LOCALE_URL,
    CUSTOM_LOCALE_CODE: CUSTOM_LOCALE_CODE,

    // GraphQL端点（从 API_BASE_URL 自动生成）
    get GRAPHQL_ENDPOINT() {
        return `${apiBaseUrl}/graphql`;
    },
};

/**
 * 隐藏的分类列表
 * 这些分类不会在导航栏、分类列表等位置显示，但文章仍可通过其他方式访问
 * 适用场景：特殊页面（如关于、隐私声明等）
 */
export const HIDDEN_CATEGORIES = ['pages'];
