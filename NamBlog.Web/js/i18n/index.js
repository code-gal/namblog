import { createI18n } from 'vue-i18n';
import zhCN from './locales/zh-CN.js';
import enUS from './locales/en-US.js';
import { config } from '../config.js';

// Detect browser language
function getBrowserLanguage() {
    const browserLang = navigator.language || navigator.userLanguage;
    // Map browser language to supported locales
    if (browserLang.startsWith('zh')) {
        return 'zh-CN';
    }
    return 'en-US';
}

// Load custom locale if configured
async function loadCustomLocale() {
    if (!config.CUSTOM_LOCALE_URL || !config.CUSTOM_LOCALE_CODE) {
        return null;
    }

    try {
        const module = await import(config.CUSTOM_LOCALE_URL);
        return {
            code: config.CUSTOM_LOCALE_CODE,
            messages: module.default
        };
    } catch (error) {
        console.warn(`Failed to load custom locale from ${config.CUSTOM_LOCALE_URL}:`, error);
        return null;
    }
}

// Get initial locale with support for custom locales
function getInitialLocale() {
    // 1. 优先使用配置文件中的语言设置
    if (config.LANGUAGE) {
        return config.LANGUAGE;
    }

    // 2. 如果配置了自定义语言包，使用自定义语言
    if (config.CUSTOM_LOCALE_CODE) {
        return config.CUSTOM_LOCALE_CODE;
    }

    // 3. 其次使用localStorage中保存的语言偏好
    const saved = localStorage.getItem('locale');
    if (saved) {
        return saved;
    }

    // 4. 最后自动检测浏览器语言
    return getBrowserLanguage();
}

// Initialize messages with built-in locales
const initialMessages = {
    'zh-CN': zhCN,
    'en-US': enUS
};

// Create i18n instance
export const i18n = createI18n({
    legacy: false, // Use Composition API mode
    locale: getInitialLocale(),
    fallbackLocale: 'en-US',
    messages: initialMessages
});

// Load and register custom locale if configured
if (config.CUSTOM_LOCALE_URL && config.CUSTOM_LOCALE_CODE) {
    loadCustomLocale().then(customLocale => {
        if (customLocale) {
            i18n.global.setLocaleMessage(customLocale.code, customLocale.messages);
            console.log(`Custom locale ${customLocale.code} loaded successfully`);
        }
    });
}

// Get list of supported locales
export function getSupportedLocales() {
    const locales = ['zh-CN', 'en-US'];
    if (config.CUSTOM_LOCALE_CODE) {
        locales.push(config.CUSTOM_LOCALE_CODE);
    }
    return locales;
}

// Helper function to change language
export function setLocale(locale) {
    const supported = getSupportedLocales();
    if (supported.includes(locale)) {
        i18n.global.locale.value = locale;
        localStorage.setItem('locale', locale);
        // Update HTML lang attribute
        document.documentElement.lang = locale;
    }
}

// Get current locale
export function getLocale() {
    return i18n.global.locale.value;
}
