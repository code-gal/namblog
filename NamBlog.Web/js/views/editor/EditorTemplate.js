/**
 * 编辑器模板
 * 导出Editor组件的HTML模板
 */
export const editorTemplate = `
<div class="w-full px-4 py-8 flex flex-col flex-1 min-h-0">
    <!-- Loading State -->
    <div v-if="isLoading" class="flex-1 flex items-center justify-center">
        <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
    </div>

    <div v-else class="flex flex-col flex-1 min-h-0">
        <!-- Error Panel -->
        <div v-if="pageErrors.length" class="mb-6 p-4 bg-red-50 dark:bg-red-900/20 border-l-4 border-red-500 rounded-r-xl shadow-lg animate-fade-in flex-shrink-0">
            <div class="flex justify-between items-start">
                <div class="flex-1">
                    <h4 class="font-semibold text-red-800 dark:text-red-200 mb-2 flex items-center gap-2">
                        <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/>
                        </svg>
                        操作失败
                    </h4>
                    <ul class="text-sm text-red-700 dark:text-red-300 space-y-1">
                        <li v-for="(err, idx) in pageErrors" :key="idx" class="flex items-start gap-2">
                            <span class="flex-shrink-0 mt-0.5">•</span>
                            <span class="break-words">{{ err }}</span>
                        </li>
                    </ul>
                </div>
                <button @click="pageErrors = []"
                        class="ml-4 text-red-500 hover:text-red-700 dark:text-red-400 dark:hover:text-red-200 transition-colors text-2xl leading-none">
                    ×
                </button>
            </div>
        </div>

        <!-- Metadata: 标题和Slug -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
            <input v-model="form.title" placeholder="文章标题"
                   class="px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-xl bg-white dark:bg-gray-700 dark:text-white transition-all focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none shadow-sm hover:shadow-md" />
            <input v-model="form.slug" placeholder="URL Slug (唯一标识)"
                   class="px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-xl bg-white dark:bg-gray-700 dark:text-white transition-all focus:ring-2 focus:ring-purple-500 focus:border-transparent outline-none shadow-sm hover:shadow-md" />
        </div>

        <!-- Metadata: 版本、分类、发布、收藏、保存按钮 -->
        <!-- 响应式布局：大屏一行 -> 中屏A/B两行 -> 小屏C/D/B三行 -->
        <div class="flex flex-wrap items-center gap-3 mb-6">
            <!-- A组：版本、分类、发布、收藏（在中屏以下自动换行） -->
            <div class="flex flex-wrap items-center gap-3">
                <!-- C组：版本+分类 -->
                <div class="flex flex-wrap items-center gap-3">
                    <!-- 版本选择 -->
                    <div v-if="!isNew" class="flex items-center gap-2">
                        <label class="text-sm font-medium text-gray-700 dark:text-gray-300 whitespace-nowrap flex items-center gap-1">
                            <svg class="w-4 h-4 text-blue-500" fill="currentColor" viewBox="0 0 20 20">
                                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clip-rule="evenodd"/>
                            </svg>
                            版本:
                        </label>
                        <select v-model="selectedVersion" @change="handleVersionChange"
                                class="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-sm bg-white dark:bg-gray-700 dark:text-white transition-all focus:ring-2 focus:ring-blue-500 focus:border-transparent shadow-sm hover:shadow-md">
                            <option v-for="v in versions" :key="v.versionName" :value="v.versionName">
                                {{ v.versionName }}
                            </option>
                        </select>
                    </div>

                    <!-- 分类输入 -->
                    <div class="flex items-center gap-2">
                        <label class="text-sm font-medium text-gray-700 dark:text-gray-300 whitespace-nowrap flex items-center gap-1">
                            <svg class="w-4 h-4 text-green-500" fill="currentColor" viewBox="0 0 20 20">
                                <path d="M7 3a1 1 0 000 2h6a1 1 0 100-2H7zM4 7a1 1 0 011-1h10a1 1 0 110 2H5a1 1 0 01-1-1zM2 11a2 2 0 012-2h12a2 2 0 012 2v4a2 2 0 01-2 2H4a2 2 0 01-2-2v-4z"/>
                            </svg>
                            分类:
                        </label>
                        <input v-model="form.category" list="category-list" placeholder="Uncategorized"
                               class="w-48 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 dark:text-white transition-all focus:ring-2 focus:ring-green-500 focus:border-transparent outline-none shadow-sm hover:shadow-md" />
                        <datalist id="category-list">
                            <option v-for="cat in categories" :key="cat" :value="cat"></option>
                        </datalist>
                    </div>
                </div>

                <!-- D组：发布+收藏 -->
                <div class="flex items-center gap-3">
                    <!-- 发布开关 -->
                    <label class="flex items-center gap-2 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 shadow-sm hover:shadow-md transition-all cursor-pointer">
                        <span class="text-sm font-medium text-gray-900 dark:text-gray-300 whitespace-nowrap">📢 发布</span>
                        <input type="checkbox" v-model="form.isPublished"
                               class="w-4 h-4 text-blue-600 bg-gray-100 border-gray-300 rounded focus:ring-blue-500 cursor-pointer transition-transform hover:scale-110" />
                    </label>

                    <!-- 收藏开关 -->
                    <label class="flex items-center gap-2 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 shadow-sm hover:shadow-md transition-all cursor-pointer">
                        <span class="text-sm font-medium text-gray-900 dark:text-gray-300 whitespace-nowrap">⭐ 收藏</span>
                        <input type="checkbox" v-model="form.isFeatured"
                               class="w-4 h-4 text-yellow-600 bg-gray-100 border-gray-300 rounded focus:ring-yellow-500 cursor-pointer transition-transform hover:scale-110" />
                    </label>
                </div>
            </div>

            <!-- 弹性间隔符（在大屏幕上显示，推动B组到右侧） -->
            <div class="hidden lg:block flex-1"></div>

            <!-- B组：清除草稿+保存文章 -->
            <div class="flex items-center gap-3">
                <!-- 清除草稿按钮 -->
                <button @click="clearDraft"
                        class="px-3 py-2 text-gray-600 hover:text-gray-800 dark:text-gray-400 dark:hover:text-gray-200 transition-all hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg border border-gray-300 dark:border-gray-600"
                        title="清除草稿缓存">
                    🗑️ 清除草稿
                </button>

                <!-- 保存文章按钮（创建和编辑都显示） -->
                <button @click="saveMetadata"
                        class="px-4 py-2 text-blue-600 hover:text-blue-700 hover:bg-blue-50 dark:text-blue-400 dark:hover:text-blue-300 dark:hover:bg-blue-900/30 border border-blue-300 dark:border-blue-600 rounded-lg transition-all disabled:opacity-50 disabled:cursor-not-allowed font-medium"
                        :disabled="isSavingMeta"
                        :title="isNew ? '首次创建文章较慢，请耐心等待' : '仅保存元数据，不创建新版本'">
                    {{ isSavingMeta ? (isNew ? '⏳ 保存中（首次创建）' : '⏳ 保存中...') : '💾 保存文章' }}
                </button>
            </div>
        </div>

        <!-- AI Prompt Timeline -->
        <div class="mb-6" v-if="!isNew">
            <div class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3 flex items-center gap-2">
                <svg class="w-5 h-5 text-purple-500" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd" d="M11.3 1.046A1 1 0 0112 2v5h4a1 1 0 01.82 1.573l-7 10A1 1 0 018 18v-5H4a1 1 0 01-.82-1.573l7-10a1 1 0 011.12-.38z" clip-rule="evenodd"/>
                </svg>
                AI Prompt 历史
            </div>

            <!-- 单行横向滚动列表（始终不换行，只横向滚动） -->
            <div class="flex gap-3 pb-2 overflow-x-auto" ref="promptListRef">
                <!-- 历史Prompt卡片 -->
                <div v-for="(prompt, index) in form.aiPrompts.slice().reverse()" :key="index"
                     @click="selectHistoryPrompt(index)"
                     :class="['relative flex-shrink-0 w-48 p-3 border-2 rounded-xl cursor-pointer transition-all shadow-md hover:shadow-lg',
                              selectedPromptIndex === (form.aiPrompts.length - 1 - index) ? 'border-blue-400 dark:border-blue-500 bg-blue-50 dark:bg-blue-900/30' : 'border-gray-300 dark:border-gray-600 hover:border-blue-400 dark:hover:border-blue-500 bg-white dark:bg-gray-800 hover:-translate-y-1']">
                    <!-- 箭头头部 -->
                    <div class="absolute -right-2 top-1/2 transform -translate-y-1/2 w-4 h-4 rotate-45 transition-all"
                         :class="selectedPromptIndex === (form.aiPrompts.length - 1 - index) ? 'bg-blue-50 dark:bg-blue-900/30 border-r border-t border-blue-400 dark:border-blue-500' : 'bg-gray-300 dark:bg-gray-600'"></div>

                    <div class="flex justify-between items-center mb-2">
                        <span class="text-xs font-semibold" :class="selectedPromptIndex === (form.aiPrompts.length - 1 - index) ? 'text-blue-700 dark:text-blue-300' : 'text-gray-600 dark:text-gray-400'">
                            #{{ index + 1 }}
                            <span v-if="selectedPromptIndex === (form.aiPrompts.length - 1 - index)" class="ml-1 px-2 py-0.5 bg-blue-200 dark:bg-blue-800 rounded-full">(使用中)</span>
                        </span>
                        <div class="flex gap-1">
                            <button @click.stop="copyPrompt(prompt)"
                                    :class="['transition-transform hover:scale-110', selectedPromptIndex === (form.aiPrompts.length - 1 - index) ? 'text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300' : 'text-blue-500 hover:text-blue-700']"
                                    title="复制">
                                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"></path></svg>
                            </button>
                        </div>
                    </div>

                    <div class="text-sm overflow-hidden" :class="expandedPromptIndex === (form.aiPrompts.length - 1 - index) ? '' : 'line-clamp-2'" style="word-break: break-all;">
                        <span :class="selectedPromptIndex === (form.aiPrompts.length - 1 - index) ? 'text-gray-700 dark:text-gray-300' : 'text-gray-700 dark:text-gray-300'">{{ prompt || '(默认提示词)' }}</span>
                    </div>
                    <button v-if="prompt && prompt.length > 25"
                            @click.stop="expandedPromptIndex = expandedPromptIndex === (form.aiPrompts.length - 1 - index) ? -1 : (form.aiPrompts.length - 1 - index)"
                            :class="['text-xs hover:underline mt-1 transition-colors', selectedPromptIndex === (form.aiPrompts.length - 1 - index) ? 'text-blue-600 dark:text-blue-400' : 'text-blue-500 hover:text-blue-700']">
                        {{ expandedPromptIndex === (form.aiPrompts.length - 1 - index) ? '收起 ▲' : '展开 ▼' }}
                    </button>
                </div>

                <!-- 自定义Prompt块 - 在列表末尾 -->
                <div ref="customPromptRef" @click.self="handleCustomPromptClick"
                     :class="['relative p-3 border-2 rounded-xl transition-all shadow-md hover:shadow-lg',
                              isCustomPromptExpanded ? 'flex-shrink-0' : 'flex-shrink-0 w-48',
                              selectedPromptIndex === -1 ? 'border-green-400 dark:border-green-500 bg-green-50 dark:bg-green-900/30' : 'border-gray-300 dark:border-gray-600 hover:border-green-400 dark:hover:border-green-500 bg-white dark:bg-gray-800 hover:-translate-y-1']"
                     :style="isCustomPromptExpanded ? 'width: calc(100% - 4rem);' : ''">
                    <!-- 箭头头部 -->
                    <div class="absolute -right-2 top-1/2 transform -translate-y-1/2 w-4 h-4 rotate-45 transition-all"
                         :class="selectedPromptIndex === -1 ? 'bg-green-50 dark:bg-green-900/30 border-r border-t border-green-400 dark:border-green-500' : 'bg-gray-300 dark:bg-gray-600'"></div>

                    <div class="flex justify-between items-center mb-2" @click.stop="toggleCustomPromptExpand">
                        <div class="flex items-center gap-2 cursor-pointer">
                            <span class="text-sm font-semibold" :class="selectedPromptIndex === -1 ? 'text-green-700 dark:text-green-300' : 'text-gray-700 dark:text-gray-300'">✏️ 自定义</span>
                            <span v-if="selectedPromptIndex === -1" class="text-xs px-2 py-0.5 bg-green-200 dark:bg-green-800 rounded-full">使用中</span>
                        </div>
                        <button :class="['transition-transform hover:scale-110', selectedPromptIndex === -1 ? 'text-green-600 dark:text-green-400 hover:text-green-700 dark:hover:text-green-300' : 'text-blue-500 hover:text-blue-700']">
                            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path v-if="!isCustomPromptExpanded" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path>
                                <path v-else stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7"></path>
                            </svg>
                        </button>
                    </div>

                    <!-- 编辑区域容器 -->
                    <div v-if="isCustomPromptExpanded" class="relative">
                        <textarea v-model="form.customPrompt"
                                  placeholder="输入自定义 AI Prompt（留空使用默认）"
                                  @click.stop
                                  @focus="selectCustomPrompt"
                                  class="w-full p-3 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-green-500 focus:border-transparent outline-none resize-none shadow-inner"
                                  style="word-break: break-word; white-space: pre-wrap;"
                                  :rows="customPromptRows"></textarea>
                        <!-- 全屏编辑按钮 -->
                        <button @click.stop="isFullscreenPromptEditor = true"
                                class="absolute bottom-2 right-2 p-1.5 text-gray-400 hover:text-green-600 dark:hover:text-green-400 transition-colors opacity-50 hover:opacity-100"
                                title="全屏编辑">
                            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4"></path>
                            </svg>
                        </button>
                    </div>
                    <div v-else class="text-sm line-clamp-2 overflow-hidden"
                         :class="selectedPromptIndex === -1 ? 'text-gray-700 dark:text-gray-300' : 'text-gray-500 dark:text-gray-400'"
                         style="word-break: break-all;" @click="handleCustomPromptClick">
                        {{ form.customPrompt || '(留空使用默认提示词)' }}
                    </div>
                </div>
            </div>
        </div>

        <!-- 新建文章时的简单Prompt输入 -->
        <div class="mb-6" v-else>
            <label class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2 flex items-center gap-2">
                <svg class="w-5 h-5 text-purple-500" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd" d="M11.3 1.046A1 1 0 0112 2v5h4a1 1 0 01.82 1.573l-7 10A1 1 0 018 18v-5H4a1 1 0 01-.82-1.573l7-10a1 1 0 011.12-.38z" clip-rule="evenodd"/>
                </svg>
                自定义 AI Prompt（可选）
            </label>
            <textarea v-model="form.customPrompt"
                      placeholder="输入自定义 AI Prompt（留空使用默认提示词）"
                      class="w-full px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-xl bg-white dark:bg-gray-700 text-gray-900 dark:text-white transition-all focus:ring-2 focus:ring-purple-500 focus:border-transparent outline-none shadow-sm hover:shadow-md resize-none"
                      rows="3"></textarea>
        </div>

        <!-- Editor Area (Split View) -->
        <div class="flex-1 min-h-0 flex flex-col lg:flex-row gap-6 overflow-hidden">
            <!-- HTML Preview (桌面端显示，移动端隐藏) -->
            <div v-if="!isHtmlCollapsed" class="hidden lg:flex lg:flex-1 flex-col border border-gray-200 dark:border-gray-700 rounded-2xl shadow-2xl bg-white dark:bg-gray-800 overflow-hidden">
                <div class="bg-gradient-to-r from-green-50 to-emerald-50 dark:from-gray-700 dark:to-gray-700 px-5 py-3 border-b border-gray-200 dark:border-gray-600 flex items-center justify-between">
                    <div class="flex items-center gap-3">
                        <!-- 删除版本按钮 -->
                        <button v-if="!isNew" @click="deleteArticle"
                                class="text-xs px-3 py-1.5 text-red-600 hover:text-red-700 hover:bg-red-50 dark:text-red-400 dark:hover:text-red-300 dark:hover:bg-red-900/30 transition-all border border-red-300 dark:border-red-600 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed"
                                :disabled="isDeleting"
                                :title="versions.length <= 1 ? '删除此版本将删除整篇文章' : '删除当前版本'">
                            {{ isDeleting ? '删除中...' : '🗑️ 删除版本' }}
                        </button>
                        <div class="flex items-center gap-2">
                            <svg class="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 20 20">
                                <path fill-rule="evenodd" d="M3 5a2 2 0 012-2h10a2 2 0 012 2v8a2 2 0 01-2 2h-2.22l.123.489.804.804A1 1 0 0113 18H7a1 1 0 01-.707-1.707l.804-.804L7.22 15H5a2 2 0 01-2-2V5zm5.771 7H5V5h10v7H8.771z" clip-rule="evenodd"/>
                            </svg>
                            <span class="font-semibold text-sm text-gray-700 dark:text-gray-200">HTML 预览</span>
                        </div>
                    </div>
                    <div class="flex items-center gap-2">
                        <!-- 全屏预览按钮 -->
                        <button v-if="htmlContent" @click="isFullscreenPreview = true"
                                class="p-1.5 bg-purple-500/10 hover:bg-purple-500/20 dark:bg-purple-500/20 dark:hover:bg-purple-500/30 text-purple-600 dark:text-purple-400 rounded-lg transition-all"
                                title="全屏预览">
                            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4"></path>
                            </svg>
                        </button>
                        <button @click="htmlContent ? clearHtml() : generateHtml()"
                                class="text-xs px-3 py-1.5 transition-all border rounded-lg disabled:opacity-50 disabled:cursor-not-allowed"
                                :class="htmlContent ? 'text-orange-600 hover:text-orange-700 hover:bg-orange-50 dark:text-orange-400 dark:hover:text-orange-300 dark:hover:bg-orange-900/30 border-orange-300 dark:border-orange-600' : 'text-green-600 hover:text-green-700 hover:bg-green-50 dark:text-green-400 dark:hover:text-green-300 dark:hover:bg-green-900/30 border-green-300 dark:border-green-600'"
                                :disabled="isGenerating"
                                :title="htmlContent ? '清空HTML：清空后创建新版本将调用AI生成新HTML' : '生成HTML：使用当前Markdown和Prompt生成HTML预览'">
                            {{ isGenerating ? '⏳ 生成中...' : (htmlContent ? '🧹 清空HTML' : '✨ 生成 HTML') }}
                        </button>
                    </div>
                </div>
                <div class="flex-1 relative overflow-auto bg-white dark:bg-gray-900">
                    <!-- 生成进度条 -->
                    <div v-if="isGenerating" class="absolute top-0 left-0 right-0 z-10">
                        <div class="bg-gradient-to-r from-blue-500 to-purple-600 h-1.5 transition-all duration-300 shadow-lg" :style="{width: generationProgress + '%'}"></div>
                        <div class="bg-gradient-to-r from-blue-50 to-purple-50 dark:bg-gray-800 p-4 text-center">
                            <span class="text-sm font-medium bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent animate-pulse">✨ 正在生成 HTML... {{ generationProgress }}%</span>
                        </div>
                    </div>
                    <!-- 使用iframe渲染HTML，完全隔离样式和脚本 -->
                    <iframe
                        ref="htmlPreviewFrame"
                        v-show="htmlContent"
                        :srcdoc="previewHtml"
                        class="w-full h-full border-0 bg-white dark:bg-gray-900"
                        title="HTML预览">
                    </iframe>
                    <div v-show="!htmlContent && !isGenerating" class="flex flex-col items-center justify-center h-full text-gray-400 dark:text-gray-500">
                        <svg class="w-16 h-16 mb-4 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 15l-2 5L9 9l11 4-5 2zm0 0l5 5M7.188 2.239l.777 2.897M5.136 7.965l-2.898-.777M13.95 4.05l-2.122 2.122m-5.657 5.656l-2.12 2.122"/>
                        </svg>
                        <p class="text-sm">点击"生成 HTML"预览内容</p>
                    </div>
                </div>
            </div>

            <!-- Markdown Editor (移动端全屏，桌面端分栏) -->
            <div class="flex-1 min-h-0 flex flex-col border border-gray-200 dark:border-gray-700 rounded-2xl shadow-2xl bg-white dark:bg-gray-800 overflow-hidden">
                <div class="bg-gradient-to-r from-blue-50 to-purple-50 dark:from-gray-700 dark:to-gray-700 px-5 py-3 border-b border-gray-200 dark:border-gray-600 flex items-center justify-between">
                    <div class="flex items-center gap-2">
                        <!-- HTML折叠/展开按钮（桌面端切换分栏，移动端打开浮动面板） -->
                        <button @click="toggleHtmlPreview"
                                :class="['text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 transition-all hover:scale-110', htmlContent && isHtmlCollapsed ? 'animate-pulse' : '']"
                                :title="isHtmlCollapsed ? '展开HTML预览' : '折叠HTML预览'">
                            <svg v-if="!isHtmlCollapsed" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
                            </svg>
                            <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
                            </svg>
                        </button>
                        <svg class="w-5 h-5 text-blue-500" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M12.316 3.051a1 1 0 01.633 1.265l-4 12a1 1 0 11-1.898-.632l4-12a1 1 0 011.265-.633zM5.707 6.293a1 1 0 010 1.414L3.414 10l2.293 2.293a1 1 0 11-1.414 1.414l-3-3a1 1 0 010-1.414l3-3a1 1 0 011.414 0zm8.586 0a1 1 0 011.414 0l3 3a1 1 0 010 1.414l-3 3a1 1 0 11-1.414-1.414L16.586 10l-2.293-2.293a1 1 0 010-1.414z" clip-rule="evenodd"/>
                        </svg>
                        <span class="font-semibold text-sm text-gray-700 dark:text-gray-200">Markdown 源码</span>
                    </div>
                    <!-- 创建新版本按钮 -->
                    <button @click="submitArticle"
                            class="text-xs px-3 py-1.5 text-blue-600 hover:text-blue-700 hover:bg-blue-50 dark:text-blue-400 dark:hover:text-blue-300 dark:hover:bg-blue-900/30 border border-blue-300 dark:border-blue-600 rounded-lg transition-all disabled:opacity-50 disabled:cursor-not-allowed font-medium"
                            :disabled="isSubmitting"
                            :title="htmlContent ? '使用当前预览的HTML创建版本（不调用AI）' : '调用AI生成HTML并创建新版本'">
                        {{ isSubmitting ? '提交中...' : (isNew ? '✨ 创建文章' : '🚀 创建新版本') }}
                    </button>
                </div>
                <div class="flex-1 min-h-0 min-h-[300px] overflow-hidden flex flex-col">
                    <textarea id="markdown-editor"></textarea>
                </div>
            </div>
        </div>
    </div>

    <!-- 全屏预览模态框 -->
    <teleport to="body">
        <div v-if="isFullscreenPreview"
             class="fixed inset-0 z-50 bg-black bg-opacity-75 flex items-center justify-center"
             @click.self="isFullscreenPreview = false">
            <div class="w-full h-full max-w-7xl mx-auto p-4 flex flex-col">
                <!-- 工具栏 -->
                <div class="flex items-center justify-between mb-2 bg-gray-800 px-4 py-2 rounded-t-lg">
                    <span class="text-white font-semibold">🖥️ 全屏预览</span>
                    <div class="flex gap-2">
                        <button @click="refreshFullscreenPreview"
                                class="px-3 py-1 text-white hover:text-blue-400 transition-colors"
                                title="刷新">
                            🔄 刷新
                        </button>
                        <button @click="isFullscreenPreview = false"
                                class="px-3 py-1 text-white hover:text-red-400 transition-colors text-xl"
                                title="关闭（ESC）">
                            ✕
                        </button>
                    </div>
                </div>

                <!-- 全屏iframe -->
                <iframe
                    :key="fullscreenRefreshKey"
                    :srcdoc="previewHtml"
                    class="flex-1 w-full border-0 bg-white dark:bg-gray-900 rounded-b-lg"
                    title="HTML全屏预览">
                </iframe>
            </div>
        </div>

        <!-- 全屏Prompt编辑器 -->
        <div v-if="isFullscreenPromptEditor"
             class="fixed inset-0 z-50 bg-black bg-opacity-75 flex items-center justify-center p-4"
             @click.self="isFullscreenPromptEditor = false">
            <div class="w-full h-full max-w-4xl flex flex-col bg-white dark:bg-gray-800 rounded-xl shadow-2xl overflow-hidden">
                <!-- 工具栏 -->
                <div class="flex items-center justify-between px-4 py-3 bg-gradient-to-r from-green-50 to-emerald-50 dark:from-gray-700 dark:to-gray-700 border-b border-gray-200 dark:border-gray-600">
                    <div class="flex items-center gap-2">
                        <svg class="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M11.3 1.046A1 1 0 0112 2v5h4a1 1 0 01.82 1.573l-7 10A1 1 0 018 18v-5H4a1 1 0 01-.82-1.573l7-10a1 1 0 011.12-.38z" clip-rule="evenodd"/>
                        </svg>
                        <span class="text-gray-800 dark:text-gray-200 font-semibold">✏️ 编辑提示词</span>
                    </div>
                    <button @click="isFullscreenPromptEditor = false"
                            class="px-3 py-1 text-gray-600 hover:text-red-500 dark:text-gray-400 dark:hover:text-red-400 transition-colors text-xl"
                            title="关闭（ESC）">
                        ✕
                    </button>
                </div>

                <!-- 全屏编辑器 -->
                <textarea v-model="form.customPrompt"
                          placeholder="输入自定义 AI Prompt（留空使用默认提示词）"
                          class="flex-1 w-full p-6 text-base border-0 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-0 outline-none resize-none"
                          style="word-break: break-word; white-space: pre-wrap;"></textarea>
            </div>
        </div>

        <!-- 全屏Markdown编辑器（支持预览） -->
        <div v-if="isFullscreenMarkdownEditor"
             class="fixed inset-0 z-50 bg-black bg-opacity-75 flex items-center justify-center p-2 sm:p-4"
             @click.self="isFullscreenMarkdownEditor = false">
            <div class="w-full h-full flex flex-col bg-white dark:bg-gray-800 rounded-xl shadow-2xl overflow-hidden">
                <!-- 工具栏 -->
                <div class="flex items-center justify-between px-3 sm:px-4 py-2 sm:py-3 bg-gradient-to-r from-blue-50 to-purple-50 dark:from-gray-700 dark:to-gray-700 border-b border-gray-200 dark:border-gray-600">
                    <div class="flex items-center gap-2">
                        <svg class="w-5 h-5 text-blue-500" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M12.316 3.051a1 1 0 01.633 1.265l-4 12a1 1 0 11-1.898-.632l4-12a1 1 0 011.265-.633zM5.707 6.293a1 1 0 010 1.414L3.414 10l2.293 2.293a1 1 0 11-1.414 1.414l-3-3a1 1 0 010-1.414l3-3a1 1 0 011.414 0zm8.586 0a1 1 0 011.414 0l3 3a1 1 0 010 1.414l-3 3a1 1 0 11-1.414-1.414L16.586 10l-2.293-2.293a1 1 0 010-1.414z" clip-rule="evenodd"/>
                        </svg>
                        <span class="text-gray-800 dark:text-gray-200 font-semibold text-sm sm:text-base">Markdown</span>
                        <!-- 视图切换按钮 -->
                        <div class="flex items-center gap-1 ml-2">
                            <button @click="fullscreenMarkdownMode = 'edit'"
                                    :class="['px-2 py-1 text-xs rounded transition-all', fullscreenMarkdownMode === 'edit' ? 'bg-blue-500 text-white' : 'text-gray-600 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-600']"
                                    title="编辑模式">
                                <span class="hidden sm:inline">编辑</span>
                                <span class="sm:hidden">📝</span>
                            </button>
                            <button @click="fullscreenMarkdownMode = 'preview'"
                                    :class="['px-2 py-1 text-xs rounded transition-all', fullscreenMarkdownMode === 'preview' ? 'bg-blue-500 text-white' : 'text-gray-600 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-600']"
                                    title="预览模式">
                                <span class="hidden sm:inline">预览</span>
                                <span class="sm:hidden">👁️</span>
                            </button>
                            <button @click="fullscreenMarkdownMode = 'split'"
                                    :class="['px-2 py-1 text-xs rounded transition-all hidden sm:flex items-center', fullscreenMarkdownMode === 'split' ? 'bg-blue-500 text-white' : 'text-gray-600 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-600']"
                                    title="分屏模式">
                                分屏
                            </button>
                        </div>
                    </div>
                    <button @click="isFullscreenMarkdownEditor = false"
                            class="px-2 sm:px-3 py-1 text-gray-600 hover:text-red-500 dark:text-gray-400 dark:hover:text-red-400 transition-colors text-xl"
                            title="关闭（ESC）">
                        ✕
                    </button>
                </div>

                <!-- 编辑器和预览区域 -->
                <div class="flex-1 flex overflow-hidden">
                    <!-- 编辑区 -->
                    <div v-show="fullscreenMarkdownMode === 'edit' || fullscreenMarkdownMode === 'split'"
                         :class="['flex-1 flex flex-col', fullscreenMarkdownMode === 'split' ? 'border-r border-gray-300 dark:border-gray-600' : '']">
                        <textarea v-model="form.markdown"
                                  placeholder="输入 Markdown 内容..."
                                  class="flex-1 w-full p-4 sm:p-6 text-sm sm:text-base border-0 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-0 outline-none resize-none font-mono"
                                  style="word-break: break-word; white-space: pre-wrap; line-height: 1.6;"></textarea>
                    </div>

                    <!-- 预览区 -->
                    <div v-show="fullscreenMarkdownMode === 'preview' || fullscreenMarkdownMode === 'split'"
                         :class="['flex-1 overflow-auto p-4 sm:p-6 bg-gray-50 dark:bg-gray-900', fullscreenMarkdownMode === 'split' ? 'hidden sm:block' : '']"
                         v-html="markdownPreview"></div>
                </div>
            </div>
        </div>
    </teleport>

    <!-- 移动端HTML浮动面板 -->
    <teleport to="body">
        <!-- 遮罩层（使用Transition实现淡入淡出） -->
        <transition name="fade">
            <div v-if="isMobileHtmlPanel"
                 @click="closeMobileHtmlPanel"
                 class="fixed inset-0 bg-black bg-opacity-50 z-40">
            </div>
        </transition>

        <!-- 浮动面板（使用Transition实现滑入滑出） -->
        <transition name="slide-left">
            <div v-if="isMobileHtmlPanel"
                 class="fixed inset-y-0 right-0 z-50 w-full md:w-96 bg-white dark:bg-gray-800 shadow-2xl overflow-hidden">

            <!-- 头部 -->
            <div class="bg-gradient-to-r from-green-50 to-emerald-50 dark:from-gray-700 dark:to-gray-700 px-4 py-3 border-b border-gray-200 dark:border-gray-600">
                <div class="flex items-center justify-between gap-3">
                    <!-- 左侧：标题和工具按钮 -->
                    <div class="flex items-center gap-3 flex-wrap">
                        <div class="flex items-center gap-2 flex-shrink-0">
                            <svg class="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 20 20">
                                <path fill-rule="evenodd" d="M3 5a2 2 0 012-2h10a2 2 0 012 2v8a2 2 0 01-2 2h-2.22l.123.489.804.804A1 1 0 0113 18H7a1 1 0 01-.707-1.707l.804-.804L7.22 15H5a2 2 0 01-2-2V5zm5.771 7H5V5h10v7H8.771z" clip-rule="evenodd"/>
                            </svg>
                            <span class="font-semibold text-gray-700 dark:text-gray-200">HTML 预览</span>
                        </div>

                        <!-- 工具按钮 -->
                        <div class="flex items-center gap-2">
                            <button v-if="!isNew" @click="deleteVersion"
                                    class="text-xs px-2 py-1 text-red-600 hover:text-red-700 hover:bg-red-50 dark:text-red-400 dark:hover:text-red-300 dark:hover:bg-red-900/30 transition-all border border-red-300 dark:border-red-600 rounded whitespace-nowrap"
                                    title="删除当前版本">
                                🗑️
                            </button>
                            <button @click="clearHtml"
                                    class="text-xs px-2 py-1 text-orange-600 hover:text-orange-700 hover:bg-orange-50 dark:text-orange-400 dark:hover:text-orange-300 dark:hover:bg-orange-900/30 transition-all border border-orange-300 dark:border-orange-600 rounded whitespace-nowrap"
                                    title="清空预览内容">
                                🧹
                            </button>
                            <button @click="openFullscreenPreview"
                                    class="p-1 bg-purple-500/10 hover:bg-purple-500/20 dark:bg-purple-500/20 dark:hover:bg-purple-500/30 text-purple-600 dark:text-purple-400 rounded transition-all"
                                    title="全屏预览">
                                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4"></path>
                                </svg>
                            </button>
                        </div>
                    </div>

                    <!-- 右侧：关闭按钮 -->
                    <button @click="closeMobileHtmlPanel"
                            class="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 text-xl transition-colors flex-shrink-0"
                            title="关闭">
                        ✕
                    </button>
                </div>
            </div>

            <!-- iframe 预览 -->
            <div class="h-full overflow-hidden pb-16">
                <iframe
                    v-if="htmlContent"
                    :srcdoc="previewHtml"
                    class="w-full h-full border-0"
                    title="HTML移动预览">
                </iframe>
                <div v-else class="flex flex-col items-center justify-center h-full text-gray-400 dark:text-gray-500 p-4">
                    <svg class="w-16 h-16 mb-4 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 15l-2 5L9 9l11 4-5 2zm0 0l5 5M7.188 2.239l.777 2.897M5.136 7.965l-2.898-.777M13.95 4.05l-2.122 2.122m-5.657 5.656l-2.12 2.122"/>
                    </svg>
                    <p class="text-sm text-center">暂无HTML内容<br/>请先生成HTML</p>
                </div>
            </div>
        </div>
        </transition>
    </teleport>
</div>
`;
