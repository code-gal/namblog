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

// 可选：添加其他开发环境配置
// window.APP_CONFIG.DEBUG = true;
