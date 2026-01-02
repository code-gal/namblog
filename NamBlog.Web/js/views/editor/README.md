# 编辑器模块说明

## 📁 文件结构

```
js/views/
├── Editor.js (221行) - 主组件，组装各模块
└── editor/
    ├── EditorTemplate.js (285行) - HTML模板
    ├── useEditorState.js (97行) - 响应式状态管理
    ├── useEditorActions.js (491行) - 业务逻辑操作
    ├── useMarkdownEditor.js (76行) - Markdown编辑器管理
    └── usePromptHelpers.js (62行) - AI Prompt辅助函数
```

## 📦 模块说明

### Editor.js (主组件)
- 组件入口，负责组装各个模块
- 处理组件生命周期钩子
- 设置监听器和路由管理
- 导出给模板使用的所有状态和方法

### EditorTemplate.js
- 完整的HTML模板字符串
- 包含所有UI结构和Vue指令
- 纯视图层，不包含业务逻辑

### useEditorState.js
- 集中管理所有响应式状态
- 包括：加载状态、表单数据、版本信息、HTML内容等
- 使用Vue的ref创建响应式变量

### useEditorActions.js
- 核心业务逻辑函数
- 包含：加载文章、保存、删除、提交、生成HTML等
- 与后端API交互
- 处理草稿缓存和错误

### useMarkdownEditor.js
- 管理EasyMDE编辑器实例
- 初始化、销毁、内容设置
- 监听编辑器变化

### usePromptHelpers.js
- AI Prompt相关的辅助函数
- 选择、切换、复制Prompt
- 滚动控制

## 🔧 使用方式

主文件导入所有模块：

```javascript
import { editorTemplate } from './editor/EditorTemplate.js';
import { useEditorState } from './editor/useEditorState.js';
import { initEditor, destroyEditor } from './editor/useMarkdownEditor.js';
import * as editorActions from './editor/useEditorActions.js';
import * as promptHelpers from './editor/usePromptHelpers.js';
```

在setup()中：
1. 初始化状态：`const state = useEditorState();`
2. 调用操作函数：`editorActions.loadArticle(...)`
3. 返回给模板：`return { ...state, handleXxx, ... }`

## ✅ 优势

1. **可维护性提升**：每个文件职责单一，易于定位和修改
2. **代码复用**：操作函数可以在不同场景下复用
3. **测试友好**：各模块可独立测试
4. **可读性增强**：主文件只关注组装逻辑，不涉及实现细节
5. **团队协作**：多人可同时编辑不同模块，减少冲突

## 🔄 向后兼容

- 原Editor.js备份为Editor.js.backup
- 对外暴露的接口完全一致
- 无需修改其他引用Editor的代码
