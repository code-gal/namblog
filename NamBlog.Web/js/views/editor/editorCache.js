/**
 * 编辑器缓存管理工具
 * 处理草稿的保存和恢复
 */

/**
 * 获取缓存键前缀
 */
function getCachePrefix(isNew, articleId) {
    return isNew ? 'draft_new' : `draft_edit_${articleId}`;
}

/**
 * 保存Markdown草稿
 */
export function saveMarkdownDraft(markdown, isNew, articleId) {
    const key = `${getCachePrefix(isNew, articleId)}_markdown`;
    localStorage.setItem(key, markdown);
}

/**
 * 保存元数据草稿
 */
export function saveMetadataDraft(metadata, isNew, articleId) {
    const key = `${getCachePrefix(isNew, articleId)}_metadata`;
    localStorage.setItem(key, JSON.stringify(metadata));
}

/**
 * 保存HTML草稿
 */
export function saveHtmlDraft(html, isNew, articleId) {
    const key = `${getCachePrefix(isNew, articleId)}_html`;
    localStorage.setItem(key, html);
}

/**
 * 加载Markdown草稿
 */
export function loadMarkdownDraft(isNew, articleId) {
    const key = `${getCachePrefix(isNew, articleId)}_markdown`;
    return localStorage.getItem(key);
}

/**
 * 加载元数据草稿
 */
export function loadMetadataDraft(isNew, articleId) {
    const key = `${getCachePrefix(isNew, articleId)}_metadata`;
    const data = localStorage.getItem(key);
    if (data) {
        try {
            return JSON.parse(data);
        } catch (e) {
            console.error('解析缓存元数据失败', e);
            return null;
        }
    }
    return null;
}

/**
 * 加载HTML草稿
 */
export function loadHtmlDraft(isNew, articleId) {
    const key = `${getCachePrefix(isNew, articleId)}_html`;
    return localStorage.getItem(key);
}

/**
 * 清除所有草稿缓存
 */
export function clearAllDrafts(isNew, articleId) {
    const prefix = getCachePrefix(isNew, articleId);
    localStorage.removeItem(`${prefix}_markdown`);
    localStorage.removeItem(`${prefix}_metadata`);
    localStorage.removeItem(`${prefix}_html`);
}

/**
 * 检查是否有缓存的草稿（与服务器数据不同）
 */
export function hasDraftChanges(isNew, articleId, serverData) {
    if (isNew) {
        // 新建文章：只要有缓存就算有改动
        return !!loadMarkdownDraft(true, null);
    } else {
        // 编辑文章：比较缓存与服务器数据
        const cachedMarkdown = loadMarkdownDraft(false, articleId);
        const cachedMeta = loadMetadataDraft(false, articleId);
        const cachedHtml = loadHtmlDraft(false, articleId);

        return (cachedMarkdown && cachedMarkdown !== serverData.markdown) ||
               (cachedMeta && JSON.stringify(cachedMeta) !== JSON.stringify({
                   title: serverData.title,
                   slug: serverData.slug,
                   category: serverData.category,
                   isPublished: serverData.isPublished,
                   isFeatured: serverData.isFeatured,
                   customPrompt: serverData.customPrompt
               })) ||
               (cachedHtml && cachedHtml !== serverData.mainVersionHtml);
    }
}
