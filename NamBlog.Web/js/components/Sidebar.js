/**
 * Sidebar 组件 - 列表页侧边栏
 *
 * 功能：
 * - 显示博主信息（头像、名称、简介、外链）
 * - 显示标签云（可点击跳转）
 * - 响应式布局（桌面/移动端）
 * - 移动端滑出动画
 * - 移动端手势控制（右边缘左滑呼出，右滑或点击遮罩关闭）
 *
 * 使用页面：Home.js、Category.js、Tag.js
 */

import { inject, ref, onMounted, onUnmounted } from 'vue';

export default {
    props: {
        // 标签列表
        tags: {
            type: Array,
            default: () => []
        },
        // 博客基本信息
        blogInfo: {
            type: Object,
            default: () => ({})
        }
    },
    setup(props) {
        const isSidebarOpen = inject('isSidebarOpen');
        const isMobile = inject('isMobile');
        const closeSidebar = inject('closeSidebar');
        const openSidebar = inject('openSidebar');

        // 手势检测
        const touchStartX = ref(0);
        const touchStartY = ref(0);
        const touchEndX = ref(0);
        const touchEndY = ref(0);

        const handleTouchStart = (e) => {
            if (!isMobile.value) return;

            touchStartX.value = e.touches[0].clientX;
            touchStartY.value = e.touches[0].clientY;
        };

        const handleTouchMove = (e) => {
            if (!isMobile.value) return;

            touchEndX.value = e.touches[0].clientX;
            touchEndY.value = e.touches[0].clientY;
        };

        const handleTouchEnd = () => {
            if (!isMobile.value) return;

            const deltaX = touchEndX.value - touchStartX.value;
            const deltaY = Math.abs(touchEndY.value - touchStartY.value);
            const screenWidth = window.innerWidth;

            // 从右边缘向左滑动呼出侧边栏（边缘区域：屏幕右侧20px）
            if (!isSidebarOpen.value &&
                touchStartX.value > screenWidth - 20 &&
                deltaX < -50 &&
                deltaY < 50) {
                openSidebar();
            }

            // 在侧边栏打开时向右滑动关闭（滑动距离超过50px）
            if (isSidebarOpen.value &&
                deltaX > 50 &&
                deltaY < 50) {
                closeSidebar();
            }

            // 重置
            touchStartX.value = 0;
            touchStartY.value = 0;
            touchEndX.value = 0;
            touchEndY.value = 0;
        };

        onMounted(() => {
            if (isMobile.value) {
                document.addEventListener('touchstart', handleTouchStart, { passive: true });
                document.addEventListener('touchmove', handleTouchMove, { passive: true });
                document.addEventListener('touchend', handleTouchEnd);
            }
        });

        onUnmounted(() => {
            document.removeEventListener('touchstart', handleTouchStart);
            document.removeEventListener('touchmove', handleTouchMove);
            document.removeEventListener('touchend', handleTouchEnd);
        });

        return {
            isSidebarOpen,
            isMobile,
            closeSidebar
        };
    },
    template: `
        <div class="relative">
            <!-- Sidebar Overlay (Mobile Only) -->
            <div v-if="isMobile && isSidebarOpen"
                 @click="closeSidebar"
                 class="fixed inset-0 bg-black bg-opacity-50 z-30 transition-opacity">
            </div>

            <!-- Sidebar -->
            <aside :class="[
                'bg-white dark:bg-gray-800 transition-all duration-300 ease-in-out',
                isMobile
                    ? 'fixed inset-y-0 right-0 w-64 z-40 shadow-xl transform'
                    : 'md:col-span-1 space-y-6 sidebar-desktop',
                (isMobile && !isSidebarOpen) ? 'translate-x-full' : 'translate-x-0',
                (!isMobile && !isSidebarOpen) ? 'hidden' : 'block'
            ]">

                <div :class="isMobile ? 'h-full overflow-y-auto p-6 space-y-6' : 'space-y-6'">
                    <!-- Close Button (Mobile Only) -->
                    <div v-if="isMobile" class="flex justify-end mb-4">
                        <button @click="closeSidebar" class="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200">
                            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                            </svg>
                        </button>
                    </div>

                    <!-- Profile Card -->
                    <div class="bg-white dark:bg-gray-800 rounded-lg shadow-sm p-6 border border-gray-100 dark:border-gray-700 text-center">
                        <!-- Avatar -->
                        <div class="w-24 h-24 bg-gray-200 dark:bg-gray-700 rounded-full mx-auto mb-4 flex items-center justify-center text-3xl overflow-hidden">
                            <img v-if="blogInfo.avatar"
                                 :src="blogInfo.avatar"
                                 alt="博主头像"
                                 class="w-full h-full object-cover" />
                            <span v-else>👨‍💻</span>
                        </div>

                        <!-- Blogger Name -->
                        <h3 class="text-lg font-bold text-gray-800 dark:text-gray-100">
                            {{ blogInfo.blogger || '博主' }}
                        </h3>

                        <!-- Slogan -->
                        <p class="text-gray-500 dark:text-gray-400 text-sm mt-2">
                            {{ blogInfo.slogan || '欢迎来到我的博客' }}
                        </p>

                        <!-- Outer Chains (Social Links) -->
                        <div v-if="blogInfo.outerChains && blogInfo.outerChains.length > 0"
                             class="mt-3 flex justify-center gap-3">
                            <a v-for="link in blogInfo.outerChains"
                               :key="link.name"
                               :href="link.link"
                               target="_blank"
                               rel="noopener noreferrer"
                               class="text-gray-600 dark:text-gray-400 hover:text-primary dark:hover:text-blue-400 transition-colors"
                               :title="link.name">
                                <img v-if="link.svg"
                                     :src="link.svg"
                                     :alt="link.name"
                                     class="w-5 h-5" />
                                <span v-else class="text-xs">{{ link.name }}</span>
                            </a>
                        </div>
                    </div>

                    <!-- Tags Cloud -->
                    <div class="bg-white dark:bg-gray-800 rounded-lg shadow-sm p-6 border border-gray-100 dark:border-gray-700">
                        <h3 class="text-lg font-bold text-gray-800 dark:text-gray-100 mb-4 border-b border-gray-100 dark:border-gray-700 pb-2">
                            标签
                        </h3>
                        <div class="tags-wrapper">
                            <div class="flex flex-wrap gap-2">
                                <router-link
                                    v-for="tag in tags"
                                    :key="tag.name"
                                    :to="'/tag/' + tag.name"
                                    class="px-3 py-1 bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 text-sm rounded-full hover:bg-primary hover:text-white dark:hover:bg-blue-600 transition-colors duration-200">
                                    {{ tag.name }}
                                    <span class="ml-1 text-xs opacity-70">({{ tag.count }})</span>
                                </router-link>
                                <span v-if="!tags || tags.length === 0" class="text-gray-500 text-sm">暂无标签</span>
                            </div>
                        </div>
                    </div>
                </div>
            </aside>
        </div>
    `
}
