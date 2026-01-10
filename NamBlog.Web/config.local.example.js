/**
 * 本地开发配置示例
 *
 * 使用方法：
 * 1. 复制此文件为 config.local.js
 *    cp config.local.example.js config.local.js
 *
 * 2. 根据需要修改配置
 *
 * 3. config.local.js 已在 .gitignore 中排除，不会提交到仓库
 *
 * 4. 生产环境不需要此文件，会自动使用 index.html 中的默认配置
 */

// 覆盖默认配置
window.APP_CONFIG.DEV_MODE = true;
window.APP_CONFIG.API_BASE_URL = 'http://localhost:5000';

// 语言配置（可选）
// 留空或注释掉此行则自动检测浏览器语言
// 支持的值：'zh-CN'（中文）或 'en-US'（英文）
// window.APP_CONFIG.LANGUAGE = 'zh-CN';  // 强制使用中文
// window.APP_CONFIG.LANGUAGE = 'en-US';  // 强制使用英文

// 可选：添加其他开发环境配置
// window.APP_CONFIG.DEBUG = true;
