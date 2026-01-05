import { ref, onMounted } from 'vue';
import { request } from '../api/client.js';

export default {
    setup() {
        // 响应式数据
        const blogName = ref('NamBlog'); // 默认值
        const currentYear = new Date().getFullYear();

        // 获取博客基本信息
        const fetchBlogInfo = async () => {
            try {
                const query = `
                    query GetBlogInfo {
                        blog {
                            baseInfo {
                                blogName
                                analyticsScript
                            }
                        }
                    }
                `;
                const data = await request(query);
                if (data?.blog?.baseInfo?.blogName) {
                    blogName.value = data.blog.baseInfo.blogName;
                }

                // 动态注入统计脚本
                if (data?.blog?.baseInfo?.analyticsScript) {
                    injectAnalyticsScript(data.blog.baseInfo.analyticsScript);
                }
            } catch (error) {
                console.warn('Failed to fetch blog info, using default name:', error);
                // 失败时保持默认值
            }
        };

        // 动态注入统计脚本
        const injectAnalyticsScript = (scriptHtml) => {
            try {
                // 创建临时容器解析 HTML
                const tempDiv = document.createElement('div');
                tempDiv.innerHTML = scriptHtml.trim();

                // 提取所有 script 标签并重新创建（浏览器不会执行 innerHTML 中的 script）
                const scriptElements = tempDiv.querySelectorAll('script');
                scriptElements.forEach(oldScript => {
                    const newScript = document.createElement('script');

                    // 复制所有属性
                    Array.from(oldScript.attributes).forEach(attr => {
                        newScript.setAttribute(attr.name, attr.value);
                    });

                    // 复制脚本内容
                    if (oldScript.textContent) {
                        newScript.textContent = oldScript.textContent;
                    }

                    // 添加到页面底部
                    document.body.appendChild(newScript);
                });
            } catch (error) {
                console.error('Failed to inject analytics script:', error);
            }
        };

        // 组件挂载时获取数据
        onMounted(() => {
            fetchBlogInfo();
        });

        return {
            blogName,
            currentYear
        };
    },
    template: `
        <footer class="bg-white dark:bg-gray-800 border-t border-gray-200 dark:border-gray-700 mt-auto">
            <div class="container mx-auto px-4 py-3">
                <!-- 三栏布局：版权 | 中间链接 | Powered by -->
                <div class="grid grid-cols-1 md:grid-cols-3 gap-2 items-center text-xs">
                    <!-- 左侧：版权声明 -->
                    <div class="text-gray-600 dark:text-gray-400 text-center md:text-left">
                        &copy; {{ currentYear }} {{ blogName }}
                    </div>

                    <!-- 中间：关于和免责声明 -->
                    <div class="flex justify-center space-x-4">
                        <router-link
                            to="/article/about"
                            class="text-gray-500 hover:text-primary dark:hover:text-blue-400 transition-colors">
                            关于
                        </router-link>
                        <router-link
                            to="/article/disclaimer"
                            class="text-gray-500 hover:text-primary dark:hover:text-blue-400 transition-colors">
                            免责声明
                        </router-link>
                    </div>

                    <!-- 右侧：Powered by -->
                    <div class="text-center md:text-right">
                        <a
                            href="https://github.com/code-gal/NamBlog"
                            target="_blank"
                            rel="noopener noreferrer"
                            class="text-gray-500 hover:text-primary dark:hover:text-blue-400 transition-colors">
                            Powered by NamBlog
                        </a>
                    </div>
                </div>
            </div>
        </footer>
    `
}
