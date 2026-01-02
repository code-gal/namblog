import { ref, inject, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { request } from '../api/client.js';
import { store } from '../store.js';
import Pagination from '../components/Pagination.js';
import Sidebar from '../components/Sidebar.js';

export default {
    components: {
        Pagination,
        Sidebar
    },
    setup() {
        const route = useRoute();
        const router = useRouter();
        const isSidebarOpen = inject('isSidebarOpen');
        const isMobile = inject('isMobile');
        const closeSidebar = inject('closeSidebar');

        const articles = ref([]);
        const pageInfo = ref(null);
        const tags = ref([]);
        const blogInfo = ref({});
        const icon = ref('');
        const isLoading = ref(true);
        const error = ref(null);

        const fetchArticles = async () => {
            // Set context for NavBar
            store.setContext('home', null);

            // 获取当前页码（从URL查询参数获取，默认为1）
            const currentPage = parseInt(route.query.page) || 1;

            const query = `
                query GetArticles($page: Int!, $pageSize: Int!, $isPublished: Boolean) {
                    blog {
                        article {
                            articles(page: $page, pageSize: $pageSize, isPublished: $isPublished) {
                                items {
                                    id
                                    title
                                    slug
                                    excerpt
                                    category
                                    tags
                                    publishedAt
                                    isPublished
                                    isFeatured
                                }
                                pageInfo {
                                    currentPage
                                    pageSize
                                    totalCount
                                    totalPages
                                    hasPreviousPage
                                    hasNextPage
                                }
                            }
                        }
                        listCollection {
                            tags {
                                name
                                count
                            }
                        }
                        baseInfo {
                            blogName
                            blogger
                            avatar
                            icon
                            slogan
                            outerChains {
                                name
                                link
                                svg
                            }
                        }
                    }
                }
            `;

            try {
                isLoading.value = true;
                error.value = null;

                // 发送请求，使用变量传递分页参数
                const variables = {
                    page: currentPage,
                    pageSize: 10
                };
                // 管理员可以看到所有文章，游客只能看到已发布的
                if (!store.isAuthenticated) {
                    variables.isPublished = true;
                }
                const data = await request(query, variables);

                // 处理文章列表
                const articleData = data.blog.article.articles;
                const fetchedArticles = articleData.items.map(article => ({
                    id: article.id,
                    title: article.title,
                    summary: article.excerpt || '',
                    date: new Date(article.publishedAt || Date.now()).toLocaleDateString('zh-CN'),
                    category: article.category || '未分类',
                    slug: article.slug,
                    isPublished: article.isPublished,
                    isFeatured: article.isFeatured,
                    tags: article.tags || []
                }));

                articles.value = fetchedArticles;
                pageInfo.value = articleData.pageInfo;

                // 处理标签（只在第一次加载时获取，后续分页不重新获取）
                if (data.blog.listCollection) {
                    tags.value = data.blog.listCollection.tags || [];
                }

                // 处理博主信息（只在第一次加载时获取）
                if (data.blog.baseInfo) {
                    blogInfo.value = {
                        blogName: data.blog.baseInfo.blogName,
                        blogger: data.blog.baseInfo.blogger,
                        avatar: data.blog.baseInfo.avatar,
                        slogan: data.blog.baseInfo.slogan,
                        outerChains: data.blog.baseInfo.outerChains || []
                    };
                    icon.value = data.blog.baseInfo.icon;

                    // 动态设置favicon
                    if (icon.value) {
                        let link = document.querySelector("link[rel*='icon']");
                        if (!link) {
                            link = document.createElement('link');
                            link.rel = 'icon';
                            document.head.appendChild(link);
                        }
                        link.href = icon.value;
                    }
                }

            } catch (err) {
                error.value = '加载文章失败，请稍后重试';
                console.error('获取文章列表失败:', err);
            } finally {
                isLoading.value = false;
            }
        };

        // 处理分页变化
        const handlePageChange = (page) => {
            // 滚动到页面顶部
            window.scrollTo({ top: 0, behavior: 'smooth' });
            // 重新加载数据（URL已经通过Pagination组件更新）
            fetchArticles();
        };

        onMounted(() => {
            fetchArticles();
        });

        // 监听路由查询参数变化（分页）
        watch(() => route.query.page, () => {
            fetchArticles();
        });

        return {
            articles,
            pageInfo,
            tags,
            blogInfo,
            isSidebarOpen,
            isMobile,
            closeSidebar,
            isLoading,
            error,
            handlePageChange
        };
    },
    template: `
        <div class="grid grid-cols-1 md:grid-cols-4 gap-8 relative">
            <!-- Main Content -->
            <div :class="[
                'transition-all duration-300',
                (isSidebarOpen && !isMobile) ? 'md:col-span-3' : 'md:col-span-4'
            ]">
                <div v-if="isLoading" class="text-center py-10">
                    <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900 dark:border-white"></div>
                    <p class="mt-2 text-gray-600 dark:text-gray-400">加载中...</p>
                </div>

                <div v-else-if="error" class="text-center py-10 text-red-500">
                    {{ error }}
                </div>

                <div v-else class="space-y-6">
                    <!-- 文章列表 -->
                    <article v-for="article in articles" :key="article.id" class="bg-white dark:bg-gray-800 rounded-lg shadow-sm hover:shadow-md transition-shadow duration-300 overflow-hidden border border-gray-100 dark:border-gray-700">
                        <div class="p-6">
                            <div class="flex items-center text-sm text-gray-500 dark:text-gray-400 mb-2">
                                <span class="bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 px-2 py-0.5 rounded text-xs font-medium mr-3">
                                    {{ article.category }}
                                </span>
                                <span>{{ article.date }}</span>
                                <!-- 收藏（精选）标识 -->
                                <span v-if="article.isFeatured" class="ml-2 text-yellow-500 text-sm" title="精选文章">
                                    ⭐
                                </span>
                                <!-- 未发布标识 -->
                                <span v-if="!article.isPublished" class="ml-2 text-red-500 text-xs border border-red-500 px-1 rounded" title="未发布">
                                    🔒
                                </span>
                            </div>
                            <router-link :to="'/article/' + article.slug">
                                <h2 class="text-2xl font-bold mb-3 text-gray-800 dark:text-gray-100 hover:text-primary dark:hover:text-blue-400 cursor-pointer">
                                    {{ article.title }}
                                </h2>
                            </router-link>

                            <!-- 摘要 -->
                            <p v-if="article.summary" class="text-gray-600 dark:text-gray-300 leading-relaxed mb-4">
                                {{ article.summary }}
                            </p>

                            <div class="flex items-center justify-between">
                                <router-link :to="'/article/' + article.slug" class="text-primary dark:text-blue-400 font-medium hover:underline text-sm">
                                    阅读更多 &rarr;
                                </router-link>
                                <!-- 标签列表（可选显示） -->
                                <div v-if="article.tags && article.tags.length > 0" class="flex gap-1 flex-wrap">
                                    <span v-for="tag in article.tags.slice(0, 3)" :key="tag"
                                          class="text-xs px-2 py-0.5 bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 rounded">
                                        {{ tag }}
                                    </span>
                                </div>
                            </div>
                        </div>
                    </article>

                    <!-- 空状态提示 -->
                    <div v-if="articles.length === 0" class="text-center py-10 text-gray-500 dark:text-gray-400">
                        <p class="text-lg">暂无文章</p>
                    </div>

                    <!-- 分页组件 -->
                    <Pagination
                        v-if="pageInfo && pageInfo.totalPages > 1"
                        :pageInfo="pageInfo"
                        @page-change="handlePageChange"
                    />
                </div>
            </div>

            <!-- Sidebar Component -->
            <Sidebar :tags="tags" :blogInfo="blogInfo" />
        </div>
    `
}
