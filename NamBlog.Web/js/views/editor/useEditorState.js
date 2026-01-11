/**
 * 编辑器状态管理
 * 管理编辑器的所有响应式状态
 */
import { ref, computed } from 'vue';

export function useEditorState() {
    // 加载和操作状态
    const isLoading = ref(false);
    const isSavingMeta = ref(false);
    const isSubmitting = ref(false);
    const isGenerating = ref(false);
    const isDeleting = ref(false);
    const isToggling = ref(false);
    const isNew = ref(true);

    // UI状态
    const isHtmlCollapsed = ref(false);
    const isFullscreenPreview = ref(false); // 全屏预览状态
    const isHtmlEditing = ref(false); // HTML全屏编辑状态
    const isFullscreenPromptEditor = ref(false); // 全屏Prompt编辑器状态
    const isFullscreenMarkdownEditor = ref(false); // 全屏Markdown编辑器状态
    const fullscreenMarkdownMode = ref('edit'); // 全屏Markdown模式: 'edit' | 'preview' | 'split'
    const isMobileHtmlPanel = ref(false); // 移动端HTML浮动面板状态
    const pageErrors = ref([]);

    // 版本相关
    const versions = ref([]);
    const selectedVersion = ref('');

    // HTML内容
    const htmlContent = ref('');
    const htmlPreviewFrame = ref(null); // iframe预览框引用
    const generationProgress = ref(0); // 流式生成进度

    // 分类
    const categories = ref([]);

    // 分类下拉（自定义，替代 datalist，确保移动端一致）
    const isCategoryDropdownOpen = ref(false);
    const categoryActiveIndex = ref(-1);
    const categoryDropdownRef = ref(null);

    // Prompt相关
    const selectedPromptIndex = ref(-1); // -1表示使用自定义，>=0表示使用历史
    const expandedPromptIndex = ref(-1); // 当前展开的prompt卡片索引
    const isCustomPromptExpanded = ref(false); // 自定义prompt是否展开
    const customPromptRows = ref(8); // 自定义prompt展开时的行数
    const promptListRef = ref(null); // prompt列表引用
    const customPromptRef = ref(null); // 自定义prompt块引用
    const historyPromptRefs = ref([]); // 历史prompt块引用数组

    // 清除草稿确认
    const clearDraftConfirm = ref(false); // 清除草稿确认状态
    const preloadedArticle = ref(null); // 预加载的文章数据

    // 删除版本确认
    const deleteVersionConfirm = ref(false); // 删除版本确认状态

    // 清空HTML确认
    const clearHtmlConfirm = ref(false); // 清空HTML确认状态

    // 表单数据
    const form = ref({
        id: null,
        title: '',
        slug: '',
        category: '',
        markdown: '',
        customPrompt: '',
        isPublished: false,
        isFeatured: false,
        aiPrompts: []
    });

    // 过滤后的分类建议（用于自定义下拉）
    const filteredCategories = computed(() => {
        const list = Array.isArray(categories.value) ? categories.value : [];
        const q = (form.value.category ?? '').toString().trim().toLowerCase();
        if (!q) return list;
        return list.filter(c => (c ?? '').toString().toLowerCase().includes(q));
    });

    // 计算属性：是否有正在进行的操作（用于禁用其他按钮）
    const isBusy = computed(() =>
        isSavingMeta.value || isSubmitting.value ||
        isGenerating.value || isDeleting.value || isToggling.value
    );

    return {
        // 状态标志
        isLoading,
        isSavingMeta,
        isSubmitting,
        isGenerating,
        isDeleting,
        isToggling,
        isNew,
        isBusy,

        // UI状态
        isHtmlCollapsed,
        isFullscreenPreview,
        isHtmlEditing,
        isFullscreenPromptEditor,
        isFullscreenMarkdownEditor,
        fullscreenMarkdownMode,
        isMobileHtmlPanel,
        pageErrors,

        // 版本
        versions,
        selectedVersion,

        // HTML
        htmlContent,
        htmlPreviewFrame,
        generationProgress,

        // 分类
        categories,

        // 分类下拉
        isCategoryDropdownOpen,
        categoryActiveIndex,
        categoryDropdownRef,
        filteredCategories,

        // Prompt
        selectedPromptIndex,
        expandedPromptIndex,
        isCustomPromptExpanded,
        customPromptRows,
        promptListRef,
        customPromptRef,
        historyPromptRefs,

        // 清除草稿
        clearDraftConfirm,
        preloadedArticle,
        // 删除版本
        deleteVersionConfirm,

        // 清空HTML
        clearHtmlConfirm,
        // 表单
        form
    };
}
