import NavBar from './components/NavBar.js';
import Footer from './components/Footer.js';
import Toast from './components/Toast.js';
import { ref, provide, onMounted, onUnmounted, watch, computed } from 'vue';
import { useRoute } from 'vue-router';
import { request } from './api/client.js';
import { store } from './store.js';

export default {
    components: {
        NavBar,
        Footer,
        Toast
    },
    setup() {
        // Global Sidebar State
        const isSidebarOpen = ref(true);
        const isMobile = ref(false);
        const route = useRoute();

        const checkMobile = () => {
            const wasMobile = isMobile.value;
            isMobile.value = window.innerWidth < 768; // md breakpoint

            // Auto-close sidebar on mobile, auto-open on desktop (if it was closed by mobile logic)
            if (isMobile.value && !wasMobile) {
                isSidebarOpen.value = false;
            } else if (!isMobile.value && wasMobile) {
                isSidebarOpen.value = true;
            }
        };

        const toggleSidebar = () => {
            isSidebarOpen.value = !isSidebarOpen.value;
        };

        // Open sidebar
        const openSidebar = () => {
            isSidebarOpen.value = true;
        };

        // Close sidebar when clicking outside on mobile
        const closeSidebar = () => {
            if (isMobile.value) {
                isSidebarOpen.value = false;
            }
        };

        // 动态设置favicon
        const updateFavicon = async () => {
            try {
                const query = `
                    query GetBlogInfo {
                        blog {
                            baseInfo {
                                icon
                            }
                        }
                    }
                `;
                const data = await request(query);
                const iconUrl = data?.blog?.baseInfo?.icon;

                if (iconUrl) {
                    // 查找或创建 link 元素
                    let link = document.querySelector("link[rel~='icon']");
                    if (!link) {
                        link = document.createElement('link');
                        link.rel = 'icon';
                        document.head.appendChild(link);
                    }
                    link.href = iconUrl;
                }
            } catch (e) {
                console.error('Failed to load favicon:', e);
            }
        };

        onMounted(() => {
            checkMobile();
            window.addEventListener('resize', checkMobile);
            updateFavicon();
        });

        // 监听路由变化，确保每次路由切换时也更新favicon
        watch(() => route.path, () => {
            updateFavicon();
        });

        onUnmounted(() => {
            window.removeEventListener('resize', checkMobile);
        });

        // Provide to descendants
        provide('isSidebarOpen', isSidebarOpen);
        provide('toggleSidebar', toggleSidebar);
        provide('openSidebar', openSidebar);
        provide('isMobile', isMobile);
        provide('closeSidebar', closeSidebar);

        // 判断是否是文章页（需要特殊布局）
        const isArticlePage = computed(() => {
            return route.name === 'article' || route.path.startsWith('/article/');
        });

        return { isArticlePage };
    },
    template: `
        <div class="min-h-screen flex flex-col">
            <!-- 文章页：后端HTML直接渲染，导航栏在Shadow DOM中 -->
            <template v-if="isArticlePage">
                <router-view></router-view>
            </template>

            <!-- 其他页面：正常布局 -->
            <template v-else>
                <NavBar />
                <main class="flex-grow container mx-auto px-4 py-8 relative flex flex-col min-h-0">
                    <router-view class="flex-1 min-h-0"></router-view>
                </main>
                <Footer />
            </template>

            <Toast />
        </div>
    `
}
