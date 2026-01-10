/**
 * Markdown编辑器管理
 * 初始化和管理EasyMDE编辑器实例
 */

// 导入i18n以支持国际化
import { i18n } from '../../i18n/index.js';

let easyMDE = null;

/**
 * 初始化Markdown编辑器
 * @param {Object} form - 表单数据
 * @param {Function} onFullscreen - 全屏回调函数
 */
export function initEditor(form, onFullscreen) {
    if (easyMDE) return easyMDE;

    const element = document.getElementById('markdown-editor');
    if (!element) {
        console.warn('Markdown editor element not found');
        return null;
    }

    // 构建工具栏
    const toolbar = [
        'bold', 'italic', 'heading', '|',
        'quote', 'unordered-list', 'ordered-list', '|',
        'link', 'image', 'code', '|',
        'preview', 'side-by-side', 'fullscreen', '|',
        'guide'
    ];

    // 仅在提供回调时添加自定义全屏按钮
    if (onFullscreen) {
        toolbar.push('|');
        toolbar.push({
            name: 'custom-fullscreen',
            action: onFullscreen,
            className: 'fa fa-arrows-alt no-disable',
            title: i18n.global.t('editor.fullscreenEditMobile')
        });
    }

    easyMDE = new EasyMDE({
        element: element,
        initialValue: form.value.markdown,
        spellChecker: false,
        autofocus: false, // 禁用自动聚焦，避免粘贴时跳转
        autosave: {
            enabled: false,
        },
        toolbar: toolbar,
        status: ['lines', 'words', 'cursor'],
        // 不设置maxHeight，让容器控制高度
        minHeight: "300px",
        sideBySideFullscreen: false,
        renderingConfig: {
            codeSyntaxHighlighting: true,
        },
    });

    // 监听编辑器内容变化
    easyMDE.codemirror.on('change', () => {
        form.value.markdown = easyMDE.value();
    });

    return easyMDE;
}

/**
 * 销毁编辑器实例
 */
export function destroyEditor() {
    if (easyMDE) {
        easyMDE.toTextArea();
        easyMDE = null;
    }
}

/**
 * 获取当前编辑器实例
 */
export function getEditor() {
    return easyMDE;
}

/**
 * 设置编辑器内容
 */
export function setEditorValue(value) {
    if (easyMDE) {
        easyMDE.value(value);
    }
}
