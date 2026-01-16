/**
 * 编辑器操作函数
 * 包含所有业务逻辑：加载、保存、删除、生成HTML等
 */
import { nextTick } from 'vue';
import { useI18n } from 'vue-i18n';
import { showToast } from '../../components/Toast.js';
import * as articleApi from '../../api/articleApi.js';
import * as editorCache from './editorCache.js';
import { setEditorValue, getEditor } from './useMarkdownEditor.js';
import { store } from '../../store.js';

/**
 * 加载分类列表
 */
export async function loadCategories(categoriesRef, t) {
    try {
        categoriesRef.value = await articleApi.fetchCategories();
    } catch(e) {
        console.error(t('editor.loadCategoriesFailed'), e);
    }
}

/**
 * 加载文章详情
 */
export async function loadArticle(slug, state, t, silent = false) {
    if (!silent) {
        state.isLoading.value = true;
    }

    try {
        const article = await articleApi.getArticleForEdit(slug);

        if(article) {
            // 获取最新版本的aiPrompt作为当前使用的提示词
            const latestPrompt = article.versions && article.versions.length > 0
                ? article.versions[0]?.aiPrompt || ''
                : '';

            state.form.value = {
                id: article.id,
                title: article.title,
                slug: article.slug,
                category: article.category,
                markdown: article.markdown || '',
                customPrompt: latestPrompt,
                isPublished: article.isPublished,
                isFeatured: article.isFeatured,
                aiPrompts: article.aiPrompts || []
            };
            state.htmlContent.value = article.mainVersionHtml || '';
            state.versions.value = article.versions || [];
            state.selectedVersion.value = state.versions.value[0]?.versionName || '';

            // 使用缓存工具加载草稿
            const cachedMarkdown = editorCache.loadMarkdownDraft(false, article.id);
            const cachedMeta = editorCache.loadMetadataDraft(false, article.id);
            const cachedHtml = editorCache.loadHtmlDraft(false, article.id);

            // 只有当缓存内容与后端数据不同时才恢复（静默恢复，不弹窗）
            if (cachedMarkdown && cachedMarkdown !== state.form.value.markdown) {
                state.form.value.markdown = cachedMarkdown;
            }
            if (cachedMeta) {
                // 只恢复有变化的字段
                if (cachedMeta.title && cachedMeta.title !== article.title) state.form.value.title = cachedMeta.title;
                // 验证slug长度，超过50字符的无效slug不恢复
                if (cachedMeta.slug && cachedMeta.slug.length <= 50 && cachedMeta.slug !== article.slug) {
                    state.form.value.slug = cachedMeta.slug;
                }
                if (cachedMeta.category && cachedMeta.category !== article.category) state.form.value.category = cachedMeta.category;
                if (cachedMeta.customPrompt !== undefined) state.form.value.customPrompt = cachedMeta.customPrompt;
                if (cachedMeta.isPublished !== undefined && cachedMeta.isPublished !== article.isPublished) {
                    state.form.value.isPublished = cachedMeta.isPublished;
                }
                if (cachedMeta.isFeatured !== undefined && cachedMeta.isFeatured !== article.isFeatured) {
                    state.form.value.isFeatured = cachedMeta.isFeatured;
                }
            }
            if (cachedHtml && cachedHtml !== article.mainVersionHtml) {
                state.htmlContent.value = cachedHtml;
            }
        } else {
            showToast(t('editor.articleNotFound'), 'error');
            return false;
        }
        // 将最终markdown同步回编辑器实例（避免仅更新state导致EasyMDE不更新）
        // 注意：初次进入页面时编辑器可能尚未初始化，此时setEditorValue会安全地无操作
        const editor = getEditor();
        if (editor) {
            setEditorValue(state.form.value.markdown || '');
            await nextTick();
            if (editor.codemirror) {
                editor.codemirror.refresh();
            }
        }

        return true;
    } catch(e) {
        console.error(e);
        const errorMsg = t('editor.loadArticleFailed') + ': ' + e.message;
        showToast(errorMsg, 'error');
        state.pageErrors.value.push(errorMsg);
        return false;
    } finally {
        if (!silent) {
            state.isLoading.value = false;
        }
    }
}

/**
 * 切换版本
 */
export async function handleVersionChange(state, t) {
    try {
        const html = await articleApi.getVersionHtml(
            state.form.value.id,
            state.selectedVersion.value
        );

        if (html) {
            // 渲染到HTML预览模块
            state.htmlContent.value = html;

            // 如果HTML面板被折叠，展开它
            if (state.isHtmlCollapsed.value) {
                state.isHtmlCollapsed.value = false;
            }

            // 触发下一个tick确保DOM更新
            await nextTick();
        } else {
            showToast(t('editor.noHtmlInThisVersion'), 'warning');
        }
    } catch (e) {
        console.error('Failed to get version HTML', e);
        showToast(t('editor.switchVersionFailed', { message: e.message }), 'error');
    }
}

/**
 * 保存文章元数据
 */
export async function saveMetadata(state, router, t) {
    // 清空旧的错误信息
    state.pageErrors.value = [];

    // 只验证markdown是必填的，其他字段可选（后端会自动生成）
    if (!state.form.value.markdown || !state.form.value.markdown.trim()) {
        showToast(t('editor.markdownEmpty'), 'warning');
        return;
    }

    state.isSavingMeta.value = true;

    try {
        // 构建输入参数，只传递有值的字段
        const input = {
            id: state.isNew.value ? null : state.form.value.id,
            markdown: state.form.value.markdown // markdown是必填的
        };

        // 可选字段：只有非空时才传递
        if (state.form.value.title && state.form.value.title.trim()) {
            input.title = state.form.value.title;
        }
        if (state.form.value.slug && state.form.value.slug.trim()) {
            input.slug = state.form.value.slug;
        }
        if (state.form.value.category && state.form.value.category.trim()) {
            input.category = state.form.value.category;
        }
        // isFeatured 和 isPublished 是布尔值，始终传递
        input.isFeatured = state.form.value.isFeatured;
        input.isPublished = state.form.value.isPublished;
        // customPrompt 可选
        if (state.form.value.customPrompt && state.form.value.customPrompt.trim()) {
            input.customPrompt = state.form.value.customPrompt;
        }
        // mainVersion 可选（用于切换主版本，仅在编辑模式下传递）
        if (!state.isNew.value && state.selectedVersion.value) {
            input.mainVersion = state.selectedVersion.value;
        }

        const savedArticle = await articleApi.saveArticle(input);

        // 更新表单数据：将后端返回的数据同步到表单
        const needsUrlUpdate = state.isNew.value || (savedArticle.slug !== router.currentRoute.value.params.slug);

        state.form.value.id = savedArticle.postId;
        state.form.value.title = savedArticle.title;
        state.form.value.slug = savedArticle.slug;
        state.form.value.category = savedArticle.category;
        state.form.value.isPublished = savedArticle.isPublished;
        state.form.value.isFeatured = savedArticle.isFeatured;

        // 清除文章列表缓存，确保其他页面能看到最新数据
        store.clearArticlesCache();

        // 如果是新建文章，保存后切换到编辑模式
        if (state.isNew.value) {
            state.isNew.value = false;
            // 缓存当前的HTML内容，避免重新加载时丢失
            editorCache.saveHtmlDraft(state.htmlContent.value, false, savedArticle.postId);
            // 更新URL为编辑模式（会触发组件重新加载，但这是预期行为）
            router.replace('/editor/' + savedArticle.slug);
            showToast(t('editor.articleCreatedSuccess'), 'success');
            // 清除新建文章的草稿缓存
            editorCache.clearAllDrafts(true, null);
        } else {
            // 编辑模式：检查slug是否变化，如果变化则更新url
            const currentSlug = router.currentRoute.value.params.slug;
            if (currentSlug !== savedArticle.slug) {
                router.replace('/editor/' + savedArticle.slug);
                showToast(t('editor.articleSavedSlugUpdated'), 'success');
            } else {
                showToast(t('editor.metadataSaved'), 'success');
            }
        }
    } catch (e) {
        console.error(e);
        const errorMsg = t('editor.saveFailed') + ': ' + e.message;
        showToast(errorMsg, 'error');
        state.pageErrors.value.push(errorMsg);
    } finally {
        state.isSavingMeta.value = false;
    }
}

/**
 * 提交文章（创建新版本）
 */
export async function submitArticle(state, router, t) {
    // 清空旧的错误信息
    state.pageErrors.value = [];

    // 只验证markdown是必填的，其他字段可选（后端会自动生成）
    if (!state.form.value.markdown || !state.form.value.markdown.trim()) {
        showToast(t('editor.markdownEmpty'), 'warning');
        return;
    }

    state.isSubmitting.value = true;

    try {
        // 构建输入参数，只传递有值的字段
        const input = {
            id: state.isNew.value ? null : state.form.value.id,
            markdown: state.form.value.markdown // markdown是必填的
        };

        // 可选字段：只有非空时才传递
        if (state.htmlContent.value && state.htmlContent.value.trim()) {
            input.html = state.htmlContent.value;
        }
        if (state.form.value.title && state.form.value.title.trim()) {
            input.title = state.form.value.title;
        }
        if (state.form.value.slug && state.form.value.slug.trim()) {
            input.slug = state.form.value.slug;
        }
        if (state.form.value.category && state.form.value.category.trim()) {
            input.category = state.form.value.category;
        }
        // isFeatured 和 isPublished 是布尔值，始终传递
        input.isFeatured = state.form.value.isFeatured;
        input.isPublished = state.form.value.isPublished;
        // customPrompt 可选
        if (state.form.value.customPrompt && state.form.value.customPrompt.trim()) {
            input.customPrompt = state.form.value.customPrompt;
        }

        const result = await articleApi.submitArticle(input);

        // 检查返回结果
        if (!result?.slug) {
            throw new Error(t('editor.backendDataError'));
        }

        const slug = result.slug;

        // 清除草稿缓存
        editorCache.clearAllDrafts(state.isNew.value, state.form.value.id);

        // 清除文章列表缓存，确保其他页面能看到最新数据
        store.clearArticlesCache();

        showToast(state.isNew.value ? t('editor.articleCreatedShort') : t('editor.versionCreatedShort'), 'success');

        // 等待一小段时间确保toast显示，然后跳转
        await new Promise(resolve => setTimeout(resolve, 300));
        router.push('/article/' + slug);
    } catch (e) {
        console.error(e);
        // 处理GraphQL多个错误
        if (e.errors && Array.isArray(e.errors)) {
            state.pageErrors.value = e.errors.map(err => err.message || String(err));
        } else {
            state.pageErrors.value.push(t('editor.submitFailedDetail', { message: e.message }));
        }
        showToast(t('editor.submitFailedGeneric'), 'error');
        // 错误时不跳转，停留在编辑页
    } finally {
        state.isSubmitting.value = false;
    }
}

/**
 * 删除版本（如果是最后一个版本则删除整篇文章）
 * 实现二次确认机制，防止误操作
 */
export async function deleteArticle(state, router, route, t) {
    const versionToDelete = state.selectedVersion.value;
    const isLastVersion = state.versions.value.length <= 1;

    // 二次确认机制
    if (!state.deleteVersionConfirm.value) {
        // 第一次点击：根据是否是最后一个版本显示不同提示
        const tipMsg = isLastVersion
            ? t('editor.deleteLastVersionConfirm')
            : t('editor.deleteVersionConfirmAgain');

        showToast(tipMsg, 'warning');
        state.deleteVersionConfirm.value = true;

        // 5秒后重置确认状态
        setTimeout(() => {
            state.deleteVersionConfirm.value = false;
        }, 5000);
        return;
    }

    // 第二次点击：执行删除操作
    state.deleteVersionConfirm.value = false;
    state.isDeleting.value = true;

    try {
        await articleApi.deleteVersion(
            state.form.value.id,
            versionToDelete
        );

        // 删除版本后：清理可能被 PWA 缓存的 /posts/* HTML（避免直访或 iframe 资源引用仍命中旧缓存）
        try {
            const slug = state.form.value.slug;
            if (slug) {
                const encodedSlug = encodeURIComponent(slug);
                const encodedVersion = encodeURIComponent(versionToDelete);

                if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
                    if (isLastVersion) {
                        // 删除整篇文章（最后一个版本）：清理该 slug 下所有版本
                        navigator.serviceWorker.controller.postMessage({
                            type: 'CACHE_DELETE_PREFIXES',
                            prefixes: [`/posts/${encodedSlug}/`]
                        });
                    } else {
                        // 只删除当前版本：兼容 / 与 /index.html 两种请求形态
                        navigator.serviceWorker.controller.postMessage({
                            type: 'CACHE_DELETE_URLS',
                            urls: [
                                `/posts/${encodedSlug}/${encodedVersion}/`,
                                `/posts/${encodedSlug}/${encodedVersion}/index.html`
                            ]
                        });
                    }
                }
            }
        } catch {
            // 缓存清理失败不应影响删除流程
        }

        // 清除文章列表缓存，确保其他页面能看到最新数据
        store.clearArticlesCache();

        if (isLastVersion) {
            // 最后一个版本删除后，整篇文章被删除
            showToast(t('editor.articleDeletedAll'), 'success');
            // 清除该文章的草稿缓存
            editorCache.clearAllDrafts(false, state.form.value.id);
            router.push('/');
        } else {
            // 非最后一个版本，重新加载文章获取最新状态
            await loadArticle(route.params.slug, state, t);
            showToast(t('editor.versionDeletedSwitch', { version: versionToDelete }), 'success');
        }
    } catch (e) {
        console.error(e);
        const errorMsg = t('editor.deleteFailedDetail', { message: e.message });
        showToast(errorMsg, 'error');
        state.pageErrors.value.push(errorMsg);
    } finally {
        state.isDeleting.value = false;
    }
}

/**
 * 清除草稿（二次确认）
 */
export async function clearDraft(state, route, t) {
    if (!state.clearDraftConfirm.value) {
        // 第一次点击：显示确认提示，并在编辑模式下预加载文章
        state.clearDraftConfirm.value = true;
        showToast(t('editor.clearDraftConfirmAgain'), 'warning');

        // 如果是编辑模式，预加载文章数据
        if (!state.isNew.value) {
            try {
                state.preloadedArticle.value = await articleApi.getArticleForEdit(route.params.slug);
            } catch (e) {
                console.error('Failed to preload article', e);
            }
        }

        // 5秒后重置确认状态
        setTimeout(() => {
            state.clearDraftConfirm.value = false;
            state.preloadedArticle.value = null;
        }, 5000);
        return;
    }

    // 第二次点击：执行清除操作
    state.clearDraftConfirm.value = false;

    // 清除缓存
    editorCache.clearAllDrafts(state.isNew.value, state.form.value.id);

    // 清空或恢复内容
    if (state.isNew.value) {
        // 新建模式：清空所有字段
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
        state.htmlContent.value = '';
        const editor = getEditor();
        if (editor) {
            setEditorValue('');
        }
    } else {
        // 编辑模式：使用预加载的数据或重新加载
        const article = state.preloadedArticle.value;
        if (article) {
            // 使用预加载的数据直接恢复
            const latestPrompt = article.versions && article.versions.length > 0
                ? article.versions[0]?.aiPrompt || ''
                : '';

            state.form.value = {
                id: article.id,
                title: article.title,
                slug: article.slug,
                category: article.category,
                markdown: article.markdown || '',
                customPrompt: latestPrompt,
                isPublished: article.isPublished,
                isFeatured: article.isFeatured,
                aiPrompts: article.aiPrompts || []
            };
            state.htmlContent.value = article.mainVersionHtml || '';
            state.versions.value = article.versions || [];
            state.selectedVersion.value = state.versions.value[0]?.versionName || '';

            setEditorValue(state.form.value.markdown);
        } else {
            // 没有预加载数据，重新从后端加载
            await loadArticle(route.params.slug, state, t);
        }
    }

    state.preloadedArticle.value = null;
    showToast(t('editor.draftCleared'), 'success');
}

/**
 * 清空HTML
 * 实现二次确认机制，防止误操作
 */
export async function clearHtml(state, t) {
    // 二次确认机制
    if (!state.clearHtmlConfirm.value) {
        // 第一次点击：显示提示
        showToast(t('editor.clearHtmlConfirmAgain'), 'warning');
        state.clearHtmlConfirm.value = true;

        // 5秒后重置确认状态
        setTimeout(() => {
            state.clearHtmlConfirm.value = false;
        }, 5000);
        return;
    }

    // 第二次点击：执行清空操作
    state.clearHtmlConfirm.value = false;
    state.htmlContent.value = '';
    // iframe会自动清空（通过v-show和:srcdoc绑定）
    await nextTick();
    showToast(t('editor.htmlCleared'), 'success');
}

/**
 * 生成HTML（调用GraphQL convertToHtml接口）
 */
export async function generateHtml(state, t) {
    if (!state.form.value.markdown) {
        showToast(t('editor.markdownEmpty'), 'warning');
        return;
    }

    state.isGenerating.value = true;
    state.htmlContent.value = '';
    state.generationProgress.value = 0;

    // 模拟进度（因为是同步返回，无法获取真实进度）
    const progressInterval = setInterval(() => {
        if (state.generationProgress.value < 90) {
            state.generationProgress.value += 10;
        }
    }, 500);

    try {
        // 使用选中的历史prompt或自定义prompt
        const promptToUse = state.selectedPromptIndex.value >= 0
            ? state.form.value.aiPrompts[state.selectedPromptIndex.value]
            : state.form.value.customPrompt;

        // 调用GraphQL接口
        const { request } = await import('../../api/client.js');
        const mutation = `
            mutation ConvertToHtml($markdown: String!, $customPrompt: String) {
                aiAgentTools {
                    convertToHtml(markdown: $markdown, customPrompt: $customPrompt) {
                        status
                        html
                        error
                    }
                }
            }
        `;

        const data = await request(mutation, {
            markdown: state.form.value.markdown,
            customPrompt: promptToUse || null
        });

        const result = data.aiAgentTools?.convertToHtml;

        if (!result) {
            throw new Error(t('editor.emptyResponse'));
        }

        if (result.status === 'FAILED' || result.error) {
            throw new Error(result.error || t('editor.conversionFailed'));
        }

        state.htmlContent.value = result.html;
        state.generationProgress.value = 100;
        showToast(t('editor.htmlGeneratedSuccess'), 'success');
    } catch (e) {
        console.error('Failed to generate HTML:', e);
        const errorMsg = t('editor.generateHtmlFailedDetail', { message: e.message });
        showToast(errorMsg, 'error');
        state.pageErrors.value.push(errorMsg);
        state.htmlContent.value = '';
        state.generationProgress.value = 0;
    } finally {
        clearInterval(progressInterval);
        state.isGenerating.value = false;
    }
}

/**
 * 切换发布状态
 */
export async function togglePublish(state) {
    state.isToggling.value = true;

    try {
        await articleApi.togglePublish(state.form.value.id);
    } finally {
        state.isToggling.value = false;
    }
}
