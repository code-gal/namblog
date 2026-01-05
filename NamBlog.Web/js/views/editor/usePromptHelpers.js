/**
 * AI Prompt相关辅助函数
 */
import { nextTick } from 'vue';
import { showToast } from '../../components/Toast.js';

/**
 * 选择历史Prompt
 * @param {number} index - 反转后数组的索引
 */
export function selectHistoryPrompt(index, state) {
    // 因为模板中使用了 form.aiPrompts.slice().reverse()，
    // 所以需要将反转后的索引转换回原始索引
    const actualIndex = state.form.value.aiPrompts.length - 1 - index;
    state.selectedPromptIndex.value = actualIndex;
    // 不自动复制内容，只标记选中状态
}

/**
 * 选择自定义Prompt
 */
export function selectCustomPrompt(state) {
    state.selectedPromptIndex.value = -1;
}

/**
 * 切换自定义Prompt展开状态
 */
export function toggleCustomPromptExpand(state) {
    state.isCustomPromptExpanded.value = !state.isCustomPromptExpanded.value;
    if (state.isCustomPromptExpanded.value) {
        selectCustomPrompt(state);
        // 展开自定义块时，自动收缩历史块
        if (state.expandedPromptIndex.value >= 0) {
            state.expandedPromptIndex.value = -1;
        }
        // 展开时自动滚动到最右边
        scrollToCustomPrompt(state);
    }
}

/**
 * 切换历史Prompt展开状态
 * @param {number} index - 反转后数组的索引
 */
export function toggleHistoryPromptExpand(index, state) {
    const currentExpandedIndex = state.expandedPromptIndex.value;
    const targetIndex = state.form.value.aiPrompts.length - 1 - index;

    // 如果点击的是当前展开的块，则收缩；否则展开新的块
    if (currentExpandedIndex === targetIndex) {
        state.expandedPromptIndex.value = -1;
    } else {
        state.expandedPromptIndex.value = targetIndex;
        // 展开历史块时，自动收缩自定义块
        if (state.isCustomPromptExpanded.value) {
            state.isCustomPromptExpanded.value = false;
        }
    }
}

/**
 * 处理自定义Prompt点击
 */
export function handleCustomPromptClick(state) {
    if (!state.isCustomPromptExpanded.value) {
        toggleCustomPromptExpand(state);
    }
}

/**
 * 滚动到自定义prompt块（最右侧）
 */
export function scrollToCustomPrompt(state) {
    // 使用更长的延迟确保展开动画和DOM更新完成
    nextTick(() => {
        setTimeout(() => {
            if (state.promptListRef.value) {
                const container = state.promptListRef.value;
                // 使用scrollTo方法，带平滑动画
                container.scrollTo({
                    left: container.scrollWidth,
                    behavior: 'smooth'
                });
            }
        }, 150);
    });
}

/**
 * 复制Prompt到剪贴板
 */
export async function copyPrompt(prompt) {
    try {
        await navigator.clipboard.writeText(prompt);
        showToast('Prompt已复制到剪贴板', 'success');
    } catch (e) {
        showToast('复制失败: ' + e.message, 'error');
    }
}
