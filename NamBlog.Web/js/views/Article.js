/**
 * 文章页组件 - 反转隔离方案
 *
 * 核心思路：
 * - 后端HTML直接渲染在主文档中（脚本正常执行）
 * - 导航栏放在Shadow DOM中（样式隔离，不受后端HTML影响）
 */

import { ref, onMounted, onUnmounted, watch, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { request } from '../api/client.js';
import { store } from '../store.js';

export default {
    setup() {
        const route = useRoute();
        const router = useRouter();
        const article = ref(null);
        const htmlContent = ref('');
        const isLoading = ref(true);
        const error = ref(null);
        const showDefaultContent = ref(false); // 是否显示默认内容
        const navRef = ref(null);         // 导航栏Shadow DOM容器
        const contentRef = ref(null);     // 内容容器
        let navShadowRoot = null;

        const isAuthenticated = computed(() => store.isAuthenticated);

        // 获取文章数据
        const fetchArticle = async () => {
            const slug = route.params.slug;
            isLoading.value = true;
            error.value = null;
            htmlContent.value = '';
            showDefaultContent.value = false; // 重置默认内容状态

            try {
                // 一次性获取文章详情和主版本HTML
                const articleQuery = `
                    query GetArticle($slug: String!) {
                        blog {
                            article {
                                article(slug: $slug) {
                                    id
                                    title
                                    author
                                    category
                                    publishedAt
                                    tags
                                    isPublished
                                    isFeatured
                                    mainVersion {
                                        versionName
                                    }
                                    mainVersionHtml
                                }
                            }
                        }
                    }
                `;

                const data = await request(articleQuery, { slug });
                const articleData = data.blog?.article?.article;

                if (articleData) {
                    // 检查主版本HTML是否存在
                    if (!articleData.mainVersionHtml) {
                        error.value = '文章内容不存在';
                        store.setContext('article', null);
                        return;
                    }

                    article.value = {
                        id: articleData.id,
                        title: articleData.title,
                        slug: slug,
                        isPublished: articleData.isPublished,
                        versionName: articleData.mainVersion?.versionName
                    };

                    // 直接使用返回的主版本HTML
                    htmlContent.value = articleData.mainVersionHtml;

                    // 更新页面标题
                    const blogName = store.state.blogName || '博客';
                    document.title = articleData.title + ' - ' + blogName;
                    store.setContext('article', slug);

                    // 渲染内容
                    await renderContent();
                } else {
                    // 文章不存在，检查是否是特殊页面
                    if (slug === 'about' || slug === 'disclaimer') {
                        showDefaultContent.value = true;
                        const defaultContent = slug === 'about'
                            ? getDefaultAboutContent()
                            : getDefaultDisclaimerContent();
                        htmlContent.value = defaultContent;

                        // 设置页面标题
                        const blogName = store.state.blogName || '博客';
                        document.title = (slug === 'about' ? '关于' : '免责声明') + ' - ' + blogName;
                        store.setContext('article', slug);

                        // 渲染默认内容
                        await renderContent();
                    } else {
                        error.value = '文章未找到';
                        store.setContext('article', null);
                    }
                }
            } catch (err) {
                console.error('加载文章错误:', err);
                // API调用失败，也检查是否是特殊页面
                if (slug === 'about' || slug === 'disclaimer') {
                    showDefaultContent.value = true;
                    const defaultContent = slug === 'about'
                        ? getDefaultAboutContent()
                        : getDefaultDisclaimerContent();
                    htmlContent.value = defaultContent;

                    // 设置页面标题
                    const blogName = store.state.blogName || '博客';
                    document.title = (slug === 'about' ? '关于' : '免责声明') + ' - ' + blogName;
                    store.setContext('article', slug);

                    // 渲染默认内容
                    await renderContent();
                } else {
                    error.value = err.message?.includes('fetch') ? '网络连接失败' : '加载文章失败';
                }
            } finally {
                isLoading.value = false;
            }
        };

        // 默认About页面内容
        const getDefaultAboutContent = () => {
            return `
                <div style="max-width: 800px; margin: 0 auto; padding: 2rem;">
                    <h1 style="font-size: 2rem; font-weight: bold; margin-bottom: 1.5rem; color: #1f2937;">关于本站</h1>
                    <div style="line-height: 1.8; color: #374151;">
                        <p style="margin-bottom: 1rem;">欢迎来到本博客！</p>
                        <p style="margin-bottom: 1rem;">这是一个由 AI 智能体渲染 Markdown 文档成 HTML 的现代化博客系统。</p>
                        <p style="margin-bottom: 1rem;">本站使用 Vue 3 构建前端，ASP.NET Core 构建后端，支持文章管理、分类标签、暗色模式等功能。</p>
                        <div style="background: #f0f9ff; border-left: 4px solid #3b82f6; padding: 1rem; margin-top: 1.5rem; border-radius: 0.25rem;">
                            <p style="margin: 0; color: #1e40af;">💡 <strong>提示</strong>：登录后台，创建 slug 为 "about" 的文章即可替换此默认页面。</p>
                        </div>
                    </div>
                </div>
            `;
        };

        // 默认Disclaimer页面内容
        const getDefaultDisclaimerContent = () => {
            return `
                <div style="max-width: 800px; margin: 0 auto; padding: 2rem;">
                    <h1 style="font-size: 2rem; font-weight: bold; margin-bottom: 1.5rem; color: #1f2937;">免责声明</h1>
                    <div style="line-height: 1.8; color: #374151;">
                        <h2 style="font-size: 1.5rem; font-weight: 600; margin: 1.5rem 0 1rem; color: #1f2937;">内容声明</h2>
                        <p style="margin-bottom: 1rem;">本博客所有内容仅代表作者个人观点，不代表任何组织或机构的立场。文章内容仅供参考，读者应自行判断其准确性和适用性。</p>

                        <h2 style="font-size: 1.5rem; font-weight: 600; margin: 1.5rem 0 1rem; color: #1f2937;">版权声明</h2>
                        <p style="margin-bottom: 1rem;">本站原创内容版权归博主所有，转载请注明出处。引用的第三方内容版权归原作者所有。</p>

                        <h2 style="font-size: 1.5rem; font-weight: 600; margin: 1.5rem 0 1rem; color: #1f2937;">准确性声明</h2>
                        <p style="margin-bottom: 1rem;">本站力求内容准确，但不保证完整性和时效性。对于因使用本站内容而导致的任何损失，本站不承担责任。</p>

                        <h2 style="font-size: 1.5rem; font-weight: 600; margin: 1.5rem 0 1rem; color: #1f2937;">外部链接</h2>
                        <p style="margin-bottom: 1rem;">本站可能包含指向外部网站的链接，这些链接仅为方便读者而提供。本站不对外部网站的内容负责。</p>

                        <div style="background: #fef3c7; border-left: 4px solid #f59e0b; padding: 1rem; margin-top: 1.5rem; border-radius: 0.25rem;">
                            <p style="margin: 0; color: #92400e;">💡 <strong>提示</strong>：登录后台，创建 slug 为 "disclaimer" 的文章即可替换此默认页面。</p>
                        </div>
                    </div>
                </div>
            `;
        };

        // 渲染后端HTML到主文档
        const renderContent = async () => {
            if (!htmlContent.value || !contentRef.value) return;

            const html = htmlContent.value;

            // 检测是否是完整HTML文档
            const isFullDocument = html.trim().startsWith('<!DOCTYPE') || html.trim().toLowerCase().startsWith('<html');

            if (isFullDocument) {
                // 解析完整HTML文档
                const parser = new DOMParser();
                const doc = parser.parseFromString(html, 'text/html');

                // 清空内容区
                contentRef.value.innerHTML = '';

                // 1. 提取并添加<head>中的样式
                doc.querySelectorAll('head style').forEach(style => {
                    const newStyle = document.createElement('style');
                    newStyle.textContent = style.textContent;
                    contentRef.value.appendChild(newStyle);
                });

                // 2. 提取并添加<head>中的外部样式表
                doc.querySelectorAll('head link[rel="stylesheet"]').forEach(link => {
                    const newLink = document.createElement('link');
                    newLink.rel = 'stylesheet';
                    newLink.href = link.href;
                    contentRef.value.appendChild(newLink);
                });

                // 3. 添加<body>内容（不包含script）
                const bodyClone = doc.body.cloneNode(true);
                bodyClone.querySelectorAll('script').forEach(s => s.remove());

                // 创建内容包装器
                const wrapper = document.createElement('div');
                wrapper.className = 'article-body-content';
                wrapper.innerHTML = bodyClone.innerHTML;
                contentRef.value.appendChild(wrapper);

                // 4. 加载<head>中的外部脚本
                const headScripts = Array.from(doc.querySelectorAll('head script[src]'));
                for (const script of headScripts) {
                    await loadScript(script.src);
                }

                // 5. 执行<head>中的内联脚本
                doc.querySelectorAll('head script:not([src])').forEach(script => {
                    executeScript(script.textContent);
                });

                // 6. 加载和执行<body>中的脚本（按顺序）
                const bodyScripts = Array.from(doc.querySelectorAll('body script'));
                for (const script of bodyScripts) {
                    if (script.src) {
                        await loadScript(script.src);
                    } else {
                        executeScript(script.textContent);
                    }
                }

            } else {
                // HTML片段，直接设置
                contentRef.value.innerHTML = html;
            }
        };

        // 加载外部脚本
        const loadScript = (src) => {
            return new Promise((resolve, reject) => {
                // 检查是否已加载
                if (document.querySelector(`script[src="${src}"]`)) {
                    resolve();
                    return;
                }
                const script = document.createElement('script');
                script.src = src;
                script.onload = resolve;
                script.onerror = () => {
                    console.warn('脚本加载失败:', src);
                    resolve(); // 不阻塞后续
                };
                document.head.appendChild(script);
            });
        };

        // 执行内联脚本
        const executeScript = (code) => {
            if (!code.trim()) return;
            try {
                const script = document.createElement('script');
                script.textContent = code;
                document.body.appendChild(script);
            } catch (e) {
                console.error('脚本执行错误:', e);
            }
        };

        // 获取分类列表
        const categories = ref([]);
        const fetchCategories = async () => {
            const query = `
                query GetCategories {
                    blog {
                        listCollection {
                            categorys {
                                name
                                count
                            }
                        }
                    }
                }
            `;
            try {
                const data = await request(query);
                if (data.blog?.listCollection?.categorys) {
                    categories.value = data.blog.listCollection.categorys;
                    updateNavCategories();
                }
            } catch (error) {
                console.error('获取分类失败:', error);
            }
        };

        // 夜间模式
        const isDarkMode = ref(document.documentElement.classList.contains('dark'));
        const toggleDarkMode = () => {
            isDarkMode.value = !isDarkMode.value;
            document.documentElement.classList.toggle('dark', isDarkMode.value);
            localStorage.setItem('theme', isDarkMode.value ? 'dark' : 'light');
            updateNavDarkMode();
        };

        // 初始化Shadow DOM导航栏
        const initNavShadow = () => {
            if (!navRef.value || navShadowRoot) return;

            navShadowRoot = navRef.value.attachShadow({ mode: 'open' });

            // 完整的折叠式导航栏
            navShadowRoot.innerHTML = `
                <style>
                    :host {
                        display: block;
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                    }

                    /* 触发区域 - 右上角 */
                    .nav-trigger {
                        position: fixed;
                        top: 0;
                        right: 0;
                        width: 80px;
                        height: 80px;
                        z-index: 99998;
                    }

                    /* 折叠指示器 */
                    .nav-indicator {
                        position: fixed;
                        top: 12px;
                        right: 12px;
                        width: 40px;
                        height: 40px;
                        border-radius: 50%;
                        background: rgba(59, 130, 246, 0.8);
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        cursor: pointer;
                        z-index: 99999;
                        transition: all 0.3s ease;
                        box-shadow: 0 2px 12px rgba(0, 0, 0, 0.2);
                    }
                    .nav-indicator.collapsed {
                        opacity: 0.4;
                        transform: scale(0.8);
                    }
                    .nav-indicator:hover {
                        opacity: 1 !important;
                        transform: scale(1) !important;
                    }
                    .nav-indicator svg {
                        width: 20px;
                        height: 20px;
                        color: white;
                    }
                    .nav-indicator.expanded {
                        opacity: 0;
                        pointer-events: none;
                    }

                    /* 导航面板 */
                    .nav-panel {
                        position: fixed;
                        top: 0;
                        right: -320px;
                        width: 300px;
                        height: 100vh;
                        background: rgba(255, 255, 255, 0.98);
                        box-shadow: -4px 0 20px rgba(0, 0, 0, 0.15);
                        z-index: 100000;
                        transition: right 0.3s ease;
                        display: flex;
                        flex-direction: column;
                        overflow: hidden;
                    }
                    .nav-panel.open {
                        right: 0;
                    }

                    /* 面板头部 */
                    .nav-header {
                        padding: 16px 20px;
                        border-bottom: 1px solid #e5e7eb;
                        display: flex;
                        align-items: center;
                        justify-content: space-between;
                    }
                    .nav-title {
                        font-size: 16px;
                        font-weight: 600;
                        color: #1f2937;
                    }
                    .nav-close {
                        width: 32px;
                        height: 32px;
                        border-radius: 50%;
                        border: none;
                        background: #f3f4f6;
                        cursor: pointer;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        transition: background 0.2s;
                    }
                    .nav-close:hover {
                        background: #e5e7eb;
                    }
                    .nav-close svg {
                        width: 16px;
                        height: 16px;
                        color: #6b7280;
                    }

                    /* 导航内容 */
                    .nav-content {
                        flex: 1;
                        overflow-y: auto;
                        padding: 12px 0;
                    }

                    /* 优化滚动条样式 */
                    .nav-content::-webkit-scrollbar,
                    .category-list::-webkit-scrollbar {
                        width: 6px;
                    }
                    .nav-content::-webkit-scrollbar-track,
                    .category-list::-webkit-scrollbar-track {
                        background: transparent;
                    }
                    .nav-content::-webkit-scrollbar-thumb,
                    .category-list::-webkit-scrollbar-thumb {
                        background: rgba(156, 163, 175, 0.3);
                        border-radius: 3px;
                    }
                    .nav-content::-webkit-scrollbar-thumb:hover,
                    .category-list::-webkit-scrollbar-thumb:hover {
                        background: rgba(156, 163, 175, 0.5);
                    }
                    /* Firefox 滚动条样式 */
                    .nav-content,
                    .category-list {
                        scrollbar-width: thin;
                        scrollbar-color: rgba(156, 163, 175, 0.3) transparent;
                    }

                    /* 导航项 */
                    .nav-item {
                        display: flex;
                        align-items: center;
                        gap: 12px;
                        padding: 12px 20px;
                        color: #374151;
                        text-decoration: none;
                        cursor: pointer;
                        transition: background 0.2s;
                        border: none;
                        background: none;
                        width: 100%;
                        text-align: left;
                        font-size: 14px;
                    }
                    .nav-item:hover {
                        background: #f3f4f6;
                    }
                    .nav-item svg {
                        width: 20px;
                        height: 20px;
                        color: #6b7280;
                        flex-shrink: 0;
                    }
                    .nav-item.active {
                        background: #eff6ff;
                        color: #2563eb;
                    }
                    .nav-item.active svg {
                        color: #2563eb;
                    }

                    /* 分隔线 */
                    .nav-divider {
                        height: 1px;
                        background: #e5e7eb;
                        margin: 8px 20px;
                    }

                    /* 分类标题 */
                    .nav-section-title {
                        padding: 8px 20px 4px;
                        font-size: 12px;
                        color: #9ca3af;
                        font-weight: 500;
                        text-transform: uppercase;
                    }

                    /* 分类列表 */
                    .category-list {
                        max-height: 300px;
                        overflow-y: auto;
                    }
                    .category-item {
                        display: flex;
                        align-items: center;
                        justify-content: space-between;
                        padding: 10px 20px 10px 32px;
                        color: #4b5563;
                        text-decoration: none;
                        cursor: pointer;
                        transition: background 0.2s;
                        font-size: 14px;
                    }
                    .category-item:hover {
                        background: #f3f4f6;
                    }
                    .category-count {
                        font-size: 12px;
                        color: #9ca3af;
                        background: #f3f4f6;
                        padding: 2px 8px;
                        border-radius: 10px;
                    }

                    /* 未发布标识 */
                    .unpublished-badge {
                        display: inline-flex;
                        align-items: center;
                        gap: 4px;
                        margin-left: auto;
                        padding: 4px 10px;
                        border-radius: 12px;
                        background: #fef3c7;
                        color: #92400e;
                        font-size: 12px;
                    }

                    /* 遮罩 */
                    .nav-overlay {
                        position: fixed;
                        top: 0;
                        left: 0;
                        right: 0;
                        bottom: 0;
                        background: rgba(0, 0, 0, 0.3);
                        z-index: 99999;
                        opacity: 0;
                        pointer-events: none;
                        transition: opacity 0.3s;
                    }
                    .nav-overlay.visible {
                        opacity: 1;
                        pointer-events: auto;
                    }

                    /* 暗色模式 */
                    :host(.dark) .nav-panel {
                        background: rgba(31, 41, 55, 0.98);
                    }
                    :host(.dark) .nav-header {
                        border-color: #374151;
                    }
                    :host(.dark) .nav-title {
                        color: #f3f4f6;
                    }
                    :host(.dark) .nav-close {
                        background: #374151;
                    }
                    :host(.dark) .nav-close:hover {
                        background: #4b5563;
                    }
                    :host(.dark) .nav-close svg {
                        color: #9ca3af;
                    }
                    :host(.dark) .nav-item {
                        color: #d1d5db;
                    }
                    :host(.dark) .nav-item:hover {
                        background: #374151;
                    }
                    :host(.dark) .nav-item svg {
                        color: #9ca3af;
                    }
                    :host(.dark) .nav-item.active {
                        background: #1e3a5f;
                        color: #60a5fa;
                    }
                    :host(.dark) .nav-item.active svg {
                        color: #60a5fa;
                    }
                    :host(.dark) .nav-divider {
                        background: #374151;
                    }
                    :host(.dark) .nav-section-title {
                        color: #6b7280;
                    }
                    :host(.dark) .category-item {
                        color: #d1d5db;
                    }
                    :host(.dark) .category-item:hover {
                        background: #374151;
                    }
                    :host(.dark) .category-count {
                        background: #374151;
                        color: #9ca3af;
                    }
                    :host(.dark) .unpublished-badge {
                        background: #78350f;
                        color: #fde68a;
                    }
                    /* 暗色模式滚动条 */
                    :host(.dark) .nav-content::-webkit-scrollbar-thumb,
                    :host(.dark) .category-list::-webkit-scrollbar-thumb {
                        background: rgba(75, 85, 99, 0.5);
                    }
                    :host(.dark) .nav-content::-webkit-scrollbar-thumb:hover,
                    :host(.dark) .category-list::-webkit-scrollbar-thumb:hover {
                        background: rgba(75, 85, 99, 0.7);
                    }
                    :host(.dark) .nav-content,
                    :host(.dark) .category-list {
                        scrollbar-color: rgba(75, 85, 99, 0.5) transparent;
                    }
                </style>

                <!-- 触发区域 -->
                <div class="nav-trigger" id="navTrigger"></div>

                <!-- 折叠指示器 -->
                <div class="nav-indicator" id="navIndicator">
                    <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
                    </svg>
                </div>

                <!-- 遮罩 -->
                <div class="nav-overlay" id="navOverlay"></div>

                <!-- 导航面板 -->
                <div class="nav-panel" id="navPanel">
                    <div class="nav-header">
                        <span class="nav-title">导航</span>
                        <button class="nav-close" id="navClose">
                            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                            </svg>
                        </button>
                    </div>
                    <div class="nav-content">
                        <!-- 主要导航 -->
                        <button class="nav-item" id="homeBtn">
                            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"/>
                            </svg>
                            <span>首页</span>
                        </button>

                        <button class="nav-item" id="editBtn" style="display: none;">
                            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/>
                            </svg>
                            <span>编辑文章</span>
                            <span class="unpublished-badge" id="unpublishedBadge" style="display: none;">🔒 未发布</span>
                        </button>

                        <button class="nav-item" id="loginBtn" style="display: none;">
                            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1"/>
                            </svg>
                            <span>登录</span>
                        </button>

                        <div class="nav-divider"></div>

                        <button class="nav-item" id="darkModeBtn">
                            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" id="darkModeIcon">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"/>
                            </svg>
                            <span id="darkModeText">夜间模式</span>
                        </button>

                        <div class="nav-divider"></div>

                        <!-- 分类 -->
                        <div class="nav-section-title">分类</div>
                        <div class="category-list" id="categoryList">
                            <!-- 动态填充 -->
                        </div>
                    </div>
                </div>
            `;

            // 获取元素
            const navTrigger = navShadowRoot.getElementById('navTrigger');
            const navIndicator = navShadowRoot.getElementById('navIndicator');
            const navPanel = navShadowRoot.getElementById('navPanel');
            const navOverlay = navShadowRoot.getElementById('navOverlay');
            const navClose = navShadowRoot.getElementById('navClose');
            const homeBtn = navShadowRoot.getElementById('homeBtn');
            const editBtn = navShadowRoot.getElementById('editBtn');
            const loginBtn = navShadowRoot.getElementById('loginBtn');
            const darkModeBtn = navShadowRoot.getElementById('darkModeBtn');

            let isOpen = false;
            let scrollTimeout = null;

            // 打开/关闭面板
            const openPanel = () => {
                navPanel.classList.add('open');
                navOverlay.classList.add('visible');
                navIndicator.classList.add('expanded');
                isOpen = true;
            };
            const closePanel = () => {
                navPanel.classList.remove('open');
                navOverlay.classList.remove('visible');
                navIndicator.classList.remove('expanded');
                isOpen = false;
            };

            // 桌面端：鼠标进入触发区域打开
            navTrigger.addEventListener('mouseenter', openPanel);
            navPanel.addEventListener('mouseleave', (e) => {
                // 检查是否移动到面板外部
                const rect = navPanel.getBoundingClientRect();
                if (e.clientX < rect.left || e.clientX > rect.right ||
                    e.clientY < rect.top || e.clientY > rect.bottom) {
                    closePanel();
                }
            });

            // 点击指示器打开
            navIndicator.addEventListener('click', openPanel);

            // 点击关闭按钮或遮罩关闭
            navClose.addEventListener('click', closePanel);
            navOverlay.addEventListener('click', closePanel);

            // 导航按钮事件
            homeBtn.addEventListener('click', () => {
                router.push('/');
                closePanel();
            });
            editBtn.addEventListener('click', () => {
                if (article.value?.slug) {
                    router.push(`/editor/${article.value.slug}`);
                }
                closePanel();
            });
            loginBtn.addEventListener('click', () => {
                router.push('/login');
                closePanel();
            });
            darkModeBtn.addEventListener('click', () => {
                toggleDarkMode();
            });

            // 滚动时折叠指示器
            window.addEventListener('scroll', () => {
                if (!isOpen) {
                    navIndicator.classList.add('collapsed');
                }
                clearTimeout(scrollTimeout);
                scrollTimeout = setTimeout(() => {
                    navIndicator.classList.remove('collapsed');
                }, 800);
            }, { passive: true });

            // 初始化暗色模式
            updateNavDarkMode();
        };

        // 更新分类列表
        const updateNavCategories = () => {
            if (!navShadowRoot) return;
            const categoryList = navShadowRoot.getElementById('categoryList');
            if (!categoryList) return;

            categoryList.innerHTML = categories.value.map(cat => `
                <div class="category-item" data-category="${cat.name}">
                    <span>${cat.name}</span>
                    <span class="category-count">${cat.count}</span>
                </div>
            `).join('');

            // 绑定点击事件
            categoryList.querySelectorAll('.category-item').forEach(item => {
                item.addEventListener('click', () => {
                    const category = item.dataset.category;
                    router.push(`/category/${encodeURIComponent(category)}`);
                    // 关闭面板
                    navShadowRoot.getElementById('navPanel')?.classList.remove('open');
                    navShadowRoot.getElementById('navOverlay')?.classList.remove('visible');
                    navShadowRoot.getElementById('navIndicator')?.classList.remove('expanded');
                });
            });
        };

        // 更新暗色模式状态
        const updateNavDarkMode = () => {
            if (!navShadowRoot) return;

            // 更新host的class
            if (isDarkMode.value) {
                navRef.value?.classList.add('dark');
            } else {
                navRef.value?.classList.remove('dark');
            }

            const darkModeIcon = navShadowRoot.getElementById('darkModeIcon');
            const darkModeText = navShadowRoot.getElementById('darkModeText');

            if (darkModeIcon && darkModeText) {
                if (isDarkMode.value) {
                    darkModeIcon.innerHTML = '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"/>';
                    darkModeText.textContent = '日间模式';
                } else {
                    darkModeIcon.innerHTML = '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"/>';
                    darkModeText.textContent = '夜间模式';
                }
            }
        };

        // 更新导航栏状态
        const updateNavState = () => {
            if (!navShadowRoot) return;

            const editBtn = navShadowRoot.getElementById('editBtn');
            const loginBtn = navShadowRoot.getElementById('loginBtn');
            const badge = navShadowRoot.getElementById('unpublishedBadge');

            // 登录状态显示编辑按钮，否则显示登录按钮
            if (editBtn) {
                editBtn.style.display = isAuthenticated.value && article.value ? 'flex' : 'none';
            }
            if (loginBtn) {
                loginBtn.style.display = isAuthenticated.value ? 'none' : 'flex';
            }

            // 显示/隐藏未发布标识
            if (badge) {
                badge.style.display = article.value && !article.value.isPublished ? 'inline-flex' : 'none';
            }
        };

        // 监听路由变化
        watch(() => route.params.slug, (newSlug, oldSlug) => {
            if (newSlug && newSlug !== oldSlug) {
                article.value = null;
                htmlContent.value = '';
                error.value = null;
                fetchArticle();
            }
        });

        // 监听文章和认证状态变化，更新导航栏
        watch([article, isAuthenticated], () => {
            updateNavState();
        });

        onMounted(async () => {
            initNavShadow();
            fetchCategories();  // 异步获取分类列表
            await fetchArticle();
            updateNavState();
        });

        onUnmounted(() => {
            store.setContext('article', null);
        });

        return {
            article,
            htmlContent,
            isLoading,
            error,
            showDefaultContent,
            navRef,
            contentRef
        };
    },
    template: `
        <div class="article-page">
            <!-- Shadow DOM 导航栏（样式隔离） -->
            <div ref="navRef"></div>

            <!-- 后端HTML内容区（直接渲染在主文档） -->
            <div ref="contentRef"
                 v-show="(!isLoading && !error && htmlContent) || showDefaultContent"
                 class="article-content"></div>

            <!-- 加载状态 -->
            <div v-if="isLoading" class="fixed inset-0 flex items-center justify-center bg-white dark:bg-gray-900">
                <div class="text-center">
                    <div class="inline-block animate-spin rounded-full h-12 w-12 border-4 border-blue-500 border-t-transparent"></div>
                    <p class="mt-4 text-gray-600 dark:text-gray-400">加载中...</p>
                </div>
            </div>

            <!-- 错误状态 -->
            <div v-if="error && !isLoading" class="fixed inset-0 flex items-center justify-center bg-white dark:bg-gray-900">
                <div class="text-center">
                    <div class="text-6xl mb-4">😕</div>
                    <p class="text-xl text-red-500 mb-6">{{ error }}</p>
                    <button @click="$router.push('/')"
                            class="px-6 py-3 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition">
                        返回首页
                    </button>
                </div>
            </div>

            <!-- 空内容 -->
            <div v-if="!isLoading && !error && !htmlContent && !showDefaultContent && article"
                 class="fixed inset-0 flex items-center justify-center bg-white dark:bg-gray-900">
                <div class="text-center">
                    <div class="text-6xl mb-4">📝</div>
                    <p class="text-xl text-gray-500">文章内容为空</p>
                </div>
            </div>
        </div>
    `
}
