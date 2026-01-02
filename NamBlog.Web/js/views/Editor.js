/**
 * 编辑器主组件
 * 负责组装各个模块和处理组件生命周期
 */
import { onMounted, onUnmounted, nextTick, watch, computed, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { store } from '../store.js';
import * as editorCache from './editor/editorCache.js';
import { editorTemplate } from './editor/EditorTemplate.js';
import { useEditorState } from './editor/useEditorState.js';
import { initEditor, destroyEditor, setEditorValue, getEditor } from './editor/useMarkdownEditor.js';
import * as editorActions from './editor/useEditorActions.js';
import * as promptHelpers from './editor/usePromptHelpers.js';

export default {
    template: editorTemplate,
    setup() {
        const route = useRoute();
        const router = useRouter();

        // 初始化状态
        const state = useEditorState();

        // ==================== 生命周期钩子 ====================

        onMounted(() => {
            if (!store.isAuthenticated) {
                router.push('/login?redirect=' + route.fullPath);
                return;
            }

            store.setContext('editor', null);
            editorActions.loadCategories(state.categories);

            // 移动端检测和默认状态设置
            const isMobile = window.matchMedia('(max-width: 768px)').matches;
            if (isMobile) {
                state.isHtmlCollapsed.value = true; // 移动端默认折叠HTML
            }

            // 添加点击外部折叠自定义prompt的监听
            const handleClickOutside = (event) => {
                if (state.isCustomPromptExpanded.value && state.customPromptRef.value) {
                    // 检查点击是否在自定义提示词块外部
                    if (!state.customPromptRef.value.contains(event.target)) {
                        state.isCustomPromptExpanded.value = false;
                    }
                }
            };
            document.addEventListener('click', handleClickOutside);

            // ESC键监听：关闭全屏预览和全屏编辑器
            const handleKeyDown = (event) => {
                if (event.key === 'Escape') {
                    if (state.isFullscreenPreview.value) {
                        state.isFullscreenPreview.value = false;
                    } else if (state.isFullscreenPromptEditor.value) {
                        state.isFullscreenPromptEditor.value = false;
                    } else if (state.isFullscreenMarkdownEditor.value) {
                        state.isFullscreenMarkdownEditor.value = false;
                    }
                }
            };
            document.addEventListener('keydown', handleKeyDown);

            // 监听来自iframe的HTML质量检测消息
            window.addEventListener('message', handleHtmlQualityMessage);

            // 窗口大小调整监听：刷新编辑器以适应布局变化
            let resizeTimer = null;
            const handleResize = () => {
                // 使用防抖，避免频繁刷新
                if (resizeTimer) clearTimeout(resizeTimer);
                resizeTimer = setTimeout(() => {
                    const editor = getEditor();
                    if (editor && editor.codemirror) {
                        editor.codemirror.refresh();
                    }
                }, 150); // 150ms延迟
            };
            window.addEventListener('resize', handleResize);

            // 清理监听器
            onUnmounted(() => {
                document.removeEventListener('click', handleClickOutside);
                document.removeEventListener('keydown', handleKeyDown);
                window.removeEventListener('resize', handleResize);
                if (resizeTimer) clearTimeout(resizeTimer);
            });

            // 路由监听：切换新建/编辑模式
            const unwatch = router.afterEach((to, from) => {
                if (to.path === '/editor' && to.params.slug === undefined) {
                    state.isNew.value = true;
                    state.form.value = {
                        id: null,
                        title: '',
                        slug: '',
                        category: '',
                        markdown: '',
                        customPrompt: '',
                        isPublished: false,
                        isFeatured: false,
                        aiPrompts: []
                    };
                    state.versions.value = [];
                    state.selectedVersion.value = 1;
                    state.htmlContent.value = '';

                    // 清空编辑器
                    setEditorValue('');

                    // Load draft if exists (新建文章缓存)
                    const draft = editorCache.loadMarkdownDraft(true, null);
                    if (draft) {
                        state.form.value.markdown = draft;
                        setEditorValue(draft);
                    }
                }
            });

            onUnmounted(() => {
                unwatch();
                window.removeEventListener('message', handleHtmlQualityMessage);
            });

            // 加载文章或初始化编辑器
            const slug = route.params.slug;
            if (slug) {
                state.isNew.value = false;
                editorActions.loadArticle(slug, state).then((success) => {
                    if (success) {
                        nextTick(() => {
                            initEditor(state.form, () => {
                                state.isFullscreenMarkdownEditor.value = true;
                            });
                            promptHelpers.scrollToCustomPrompt(state);
                        });
                    }
                });
            } else {
                state.isNew.value = true;
                state.isLoading.value = false;

                // Load draft (新建文章缓存)
                const draft = editorCache.loadMarkdownDraft(true, null);
                const draftMeta = editorCache.loadMetadataDraft(true, null);
                const draftHtml = editorCache.loadHtmlDraft(true, null);

                if (draft) {
                    state.form.value.markdown = draft;
                }
                if (draftMeta) {
                    state.form.value.title = draftMeta.title || '';
                    // 验证slug长度，超过50字符的无效slug不恢复（留空让后端AI生成）
                    state.form.value.slug = (draftMeta.slug && draftMeta.slug.length <= 50) ? draftMeta.slug : '';
                    state.form.value.category = draftMeta.category || '';
                    state.form.value.customPrompt = draftMeta.customPrompt || '';
                    state.form.value.isPublished = draftMeta.isPublished || false;
                    state.form.value.isFeatured = draftMeta.isFeatured || false;
                }
                if (draftHtml) {
                    state.htmlContent.value = draftHtml;
                }

                nextTick(() => {
                    initEditor(state.form, () => {
                        state.isFullscreenMarkdownEditor.value = true;
                    });
                });
            }
        });

        // ==================== 监听器 ====================

        // Auto-save draft (分场景缓存)
        watch(() => state.form.value.markdown, (newVal) => {
            editorCache.saveMarkdownDraft(newVal, state.isNew.value, state.form.value.id);
        });

        // Auto-save metadata (including customPrompt)
        watch(() => ({
            title: state.form.value.title,
            slug: state.form.value.slug,
            category: state.form.value.category,
            isPublished: state.form.value.isPublished,
            isFeatured: state.form.value.isFeatured,
            customPrompt: state.form.value.customPrompt
        }), (newVal) => {
            editorCache.saveMetadataDraft(newVal, state.isNew.value, state.form.value.id);
        }, { deep: true });

        // Auto-save HTML content
        watch(() => state.htmlContent.value, (newVal) => {
            if (newVal) {
                editorCache.saveHtmlDraft(newVal, state.isNew.value, state.form.value.id);
            }
        });

        // Markdown预览（简单的markdown转HTML，用于全屏编辑器）
        const markdownPreview = computed(() => {
            const markdown = state.form.value.markdown;
            if (!markdown) return '<p class="text-gray-400">暂无内容</p>';

            // 简单的markdown渲染（基础支持）
            let html = markdown
                // 标题
                .replace(/^### (.+)$/gm, '<h3 class="text-xl font-bold mt-4 mb-2">$1</h3>')
                .replace(/^## (.+)$/gm, '<h2 class="text-2xl font-bold mt-6 mb-3">$1</h2>')
                .replace(/^# (.+)$/gm, '<h1 class="text-3xl font-bold mt-8 mb-4">$1</h1>')
                // 粗体和斜体
                .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
                .replace(/\*(.+?)\*/g, '<em>$1</em>')
                // 链接
                .replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2" class="text-blue-500 hover:underline">$1</a>')
                // 代码块
                .replace(/```([\s\S]*?)```/g, '<pre class="bg-gray-100 dark:bg-gray-800 p-4 rounded my-2 overflow-x-auto"><code>$1</code></pre>')
                // 行内代码
                .replace(/`([^`]+)`/g, '<code class="bg-gray-100 dark:bg-gray-800 px-1 rounded">$1</code>')
                // 段落
                .replace(/\n\n/g, '</p><p class="my-2">')
                // 换行
                .replace(/\n/g, '<br>');

            return '<div class="prose dark:prose-invert max-w-none"><p class="my-2">' + html + '</p></div>';
        });

        // 准备HTML预览内容（仅注入夜间模式类，不修改HTML内容本身）
        const previewHtml = computed(() => {
            let html = state.htmlContent.value;
            if (!html) return '';

            // 注入错误监听脚本（必须在HTML最前面，确保能捕获所有错误）
            const errorListener = `
<script>
(function() {
    const errors = [];
    let reported = false;

    // 监听资源加载错误（图片、CSS、JS文件等）
    window.addEventListener('error', function(e) {
        if (e.target && e.target !== window) {
            const resource = e.target.src || e.target.href;
            if (resource) {
                errors.push('资源加载失败: ' + resource);
            }
        }
    }, true);

    // 监听JS运行时错误（包括hljs未定义等）
    window.onerror = function(message, source, lineno, colno, error) {
        errors.push('JS错误: ' + message + (lineno ? ' (行' + lineno + ')' : ''));
        return true; // 阻止默认错误处理
    };

    // 延迟报告，确保捕获所有错误
    const reportErrors = function() {
        if (!reported) {
            reported = true;
            if (errors.length > 0) {
                window.parent.postMessage({
                    type: 'html-quality-check',
                    errors: errors
                }, '*');
            }
        }
    };

    // 页面加载完成后等待一段时间再报告
    window.addEventListener('load', function() {
        setTimeout(reportErrors, 500);
    });

    // 如果5秒后还有错误产生也报告
    setTimeout(function() {
        reportErrors();
    }, 5000);
})();
</script>`;

            // 在HTML最前面注入监听脚本（优先级最高）
            if (html.includes('<head>')) {
                html = html.replace('<head>', '<head>' + errorListener);
            } else if (html.includes('<html>')) {
                html = html.replace('<html>', '<html>' + errorListener);
            } else {
                html = errorListener + html;
            }

            // 如果当前是夜间模式，注入dark类到<html>标签（仅用于预览，不影响原始HTML）
            const isDark = document.documentElement.classList.contains('dark');
            if (isDark && !html.includes('class="dark"')) {
                html = html.replace(/<html([^>]*)>/, '<html$1 class="dark">');
            }

            return html;
        });

        // 监听来自iframe的HTML质量检测消息
        const handleHtmlQualityMessage = (event) => {
            if (event.data && event.data.type === 'html-quality-check') {
                const errors = event.data.errors || [];
                if (errors.length > 0) {
                    // 添加一个总结性错误提示
                    state.pageErrors.value = [
                        `HTML预览发现 ${errors.length} 个问题（来自AI生成的HTML）：`,
                        ...errors.slice(0, 5), // 最多显示5个错误
                        ...(errors.length > 5 ? ['…还有更多错误（共' + errors.length + '个）'] : [])
                    ];
                }
            }
        };

        // 全屏预览刷新key
        const fullscreenRefreshKey = ref(0);
        const refreshFullscreenPreview = () => {
            fullscreenRefreshKey.value++;
        };

        // HTML预览切换（智能处理桌面端和移动端）
        const toggleHtmlPreview = () => {
            const isMobile = window.matchMedia('(max-width: 1023px)').matches;
            if (isMobile) {
                // 移动端：打开/关闭浮动面板
                const wasOpen = state.isMobileHtmlPanel.value;
                state.isMobileHtmlPanel.value = !state.isMobileHtmlPanel.value;

                // 如果是关闭面板，需要刷新编辑器以修复可能的显示问题
                // 使用setTimeout确保在transition动画完成后刷新
                if (wasOpen) {
                    setTimeout(() => {
                        const editor = getEditor();
                        if (editor && editor.codemirror) {
                            editor.codemirror.refresh();
                        }
                    }, 350); // transition动画时长+缓冲
                }
            } else {
                // 桌面端：切换分栏显示/隐藏
                state.isHtmlCollapsed.value = !state.isHtmlCollapsed.value;

                // 桌面端切换后也刷新编辑器
                nextTick(() => {
                    const editor = getEditor();
                    if (editor && editor.codemirror) {
                        editor.codemirror.refresh();
                    }
                });
            }
        };

        onUnmounted(() => {
            destroyEditor();
        });

        // ==================== 操作函数包装 ====================

        const handleVersionChange = () => editorActions.handleVersionChange(state);
        const saveMetadata = () => editorActions.saveMetadata(state, router);
        const submitArticle = () => editorActions.submitArticle(state, router);
        const deleteArticle = () => editorActions.deleteArticle(state, router, route);
        const clearDraft = () => editorActions.clearDraft(state, route);
        const clearHtml = () => editorActions.clearHtml(state);
        const generateHtml = () => editorActions.generateHtml(state);
        const togglePublish = () => editorActions.togglePublish(state);

        // 打开全屏预览（关闭移动端浮动面板）
        const openFullscreenPreview = () => {
            state.isMobileHtmlPanel.value = false;
            state.isFullscreenPreview.value = true;
        };

        // 关闭移动端HTML面板（统一入口，确保刷新编辑器）
        const closeMobileHtmlPanel = () => {
            state.isMobileHtmlPanel.value = false;

            // 强制重新计算整个页面布局
            const forceRelayout = () => {
                // 找到主编辑区容器并强制重排
                const editorContainer = document.querySelector('.EasyMDEContainer');
                const markdownWrapper = editorContainer?.closest('.flex-1.flex.flex-col');

                if (markdownWrapper) {
                    // 强制布局重算：先隐藏再显示
                    markdownWrapper.style.display = 'none';
                    // 强制回流
                    void markdownWrapper.offsetHeight;
                    markdownWrapper.style.display = '';
                }

                // 刷新CodeMirror
                const editor = getEditor();
                if (editor && editor.codemirror) {
                    editor.codemirror.refresh();
                }
            };

            // 在transition动画完成后强制重排
            setTimeout(forceRelayout, 350);
        };

        // 删除版本（关闭移动端浮动面板）
        const deleteVersion = () => {
            state.isMobileHtmlPanel.value = false;
            deleteArticle();
        };

        const selectHistoryPrompt = (index) => promptHelpers.selectHistoryPrompt(index, state);
        const selectCustomPrompt = () => promptHelpers.selectCustomPrompt(state);
        const toggleCustomPromptExpand = () => promptHelpers.toggleCustomPromptExpand(state);
        const handleCustomPromptClick = () => promptHelpers.handleCustomPromptClick(state);
        const scrollToCustomPrompt = () => promptHelpers.scrollToCustomPrompt(state);
        const copyPrompt = (prompt) => promptHelpers.copyPrompt(prompt);

        const goBack = () => router.back();

        // ==================== 返回暴露给模板的内容 ====================

        return {
            // 状态
            ...state,
            previewHtml,
            markdownPreview,
            fullscreenRefreshKey,

            // 操作函数
            handleVersionChange,
            saveMetadata,
            togglePublish,
            submitArticle,
            deleteArticle,
            deleteVersion,
            generateHtml,
            clearHtml,
            refreshFullscreenPreview,
            openFullscreenPreview,
            closeMobileHtmlPanel,
            toggleHtmlPreview,
            goBack,
            clearDraft,

            // Prompt相关
            selectHistoryPrompt,
            selectCustomPrompt,
            toggleCustomPromptExpand,
            handleCustomPromptClick,
            scrollToCustomPrompt,
            copyPrompt
        };
    }
};
