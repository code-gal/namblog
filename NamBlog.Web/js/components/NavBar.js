import { inject, ref, onMounted, onUnmounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { auth } from '../api/auth.js';
import { store } from '../store.js';
import { request } from '../api/client.js';
import { HIDDEN_CATEGORIES } from '../config.js';

export default {
    setup() {
        const { t } = useI18n();
        const toggleSidebar = inject('toggleSidebar');
        const isSidebarOpen = inject('isSidebarOpen');
        const route = useRoute();
        const router = useRouter();

        // Use computed property for reactivity from store
        const isLoggedIn = computed(() => store.isAuthenticated);
        const blogName = computed(() => store.state.blogName);
        const categories = ref([]);

        // Check if current page is a list page (home, category, tag)
        const isListPage = computed(() => {
            const ctx = store.state.context;
            return ctx.type === 'home' || ctx.type === 'list';
        });

        const fetchCategories = async () => {
            const query = `
                query GetCategories {
                    blog {
                        listCollection {
                            categorys {
                                name
                                count
                            }
                        }
                    }
                }
            `;
            try {
                const data = await request(query);
                if (data.blog.listCollection && data.blog.listCollection.categorys) {
                    // 过滤掉隐藏的分类，不在导航栏中显示
                    categories.value = data.blog.listCollection.categorys.filter(
                        cat => !HIDDEN_CATEGORIES.includes(cat.name.toLowerCase())
                    );
                }
            } catch (error) {
                console.error('Failed to fetch categories:', error);
            }
        };

        const handleLogout = () => {
            auth.logout();
            router.push('/');
        };

        // Action button configuration based on context
        const actionButtonConfig = computed(() => {
            if (!store.isAuthenticated) {
                return null; // Will show login link instead
            }

            const ctx = store.state.context;

            // Editor page: show logout button
            if (ctx.type === 'editor') {
                return {
                    icon: 'logout',
                    title: t('nav.logout'),
                    action: handleLogout
                };
            }

            // Article page: show edit button
            if (ctx.type === 'article' && ctx.slug) {
                return {
                    icon: 'edit',
                    title: t('nav.editArticle'),
                    action: () => router.push(`/editor/${ctx.slug}`)
                };
            }

            // Home or list page: show create button
            return {
                icon: 'create',
                title: t('nav.createArticle'),
                action: () => router.push('/editor')
            };
        });

        // Mobile Menu State
        const isMobileMenuOpen = ref(false);
        const toggleMobileMenu = () => {
            isMobileMenuOpen.value = !isMobileMenuOpen.value;
        };

        // Simple theme toggler logic
        const toggleTheme = () => {
            if (document.documentElement.classList.contains('dark')) {
                document.documentElement.classList.remove('dark');
                localStorage.setItem('theme', 'light');
            } else {
                document.documentElement.classList.add('dark');
                localStorage.setItem('theme', 'dark');
            }
        };

        // Initialize theme
        if (localStorage.theme === 'dark' || (!('theme' in localStorage) && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.classList.remove('dark');
        }

        // Scroll Logic
        const isNavVisible = ref(true);
        let lastScrollTop = 0;

        const handleScroll = () => {
            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
            if (scrollTop > lastScrollTop && scrollTop > 60) {
                isNavVisible.value = false;
            } else {
                isNavVisible.value = true;
            }
            lastScrollTop = scrollTop <= 0 ? 0 : scrollTop;
        };

        onMounted(() => {
            window.addEventListener('scroll', handleScroll);
            fetchCategories();
        });

        onUnmounted(() => {
            window.removeEventListener('scroll', handleScroll);
        });

        // Active Link Logic
        const isActive = (path) => route.path === path;

        // Category Active Link Logic
        const isCategoryActive = (categoryName) => {
            return route.path === `/category/${encodeURIComponent(categoryName)}`;
        };

        const navigateToCategory = (categoryName) => {
            router.push(`/category/${encodeURIComponent(categoryName)}`);
            isMobileMenuOpen.value = false;
        };

        return {
            t,
            blogName,
            toggleTheme,
            toggleSidebar,
            isSidebarOpen,
            isNavVisible,
            isActive,
            isMobileMenuOpen,
            toggleMobileMenu,
            isLoggedIn,
            handleLogout,
            actionButtonConfig,
            categories,
            navigateToCategory,
            isCategoryActive,
            isListPage
        };
    },
    template: `
        <nav :class="[
            'bg-white dark:bg-gray-800 shadow-md sticky top-0 z-40 transition-all duration-300',
            isNavVisible ? 'translate-y-0' : '-translate-y-full'
        ]">
            <div class="container mx-auto px-4">
                <div class="flex justify-between items-center h-16">
                    <!-- Left Side: Mobile Menu Button & Logo -->
                    <div class="flex items-center space-x-3">
                        <!-- Mobile Menu Button (Categories) -->
                        <button @click="toggleMobileMenu" class="p-2 rounded-md text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700 focus:outline-none md:hidden" title="菜单">
                            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path v-if="!isMobileMenuOpen" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
                                <path v-else stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                            </svg>
                        </button>

                        <!-- Logo -->
                        <router-link to="/" class="text-2xl font-bold text-primary dark:text-blue-400">
                            {{ blogName }}
                        </router-link>
                    </div>

                    <!-- Desktop Menu -->
                    <div class="hidden md:flex items-center space-x-1 h-full">
                        <router-link to="/"
                            :class="[
                                'px-4 h-full flex items-center text-lg font-medium border-b-4 transition-colors duration-200',
                                isActive('/')
                                    ? 'border-primary text-primary bg-gray-50 dark:bg-gray-900 dark:text-blue-400 dark:border-blue-400'
                                    : 'border-transparent text-gray-700 dark:text-gray-300 hover:text-primary dark:hover:text-blue-400 hover:bg-gray-50 dark:hover:bg-gray-700'
                            ]">
                            {{ t('nav.home') }}
                        </router-link>

                        <!-- Categories -->
                        <a v-for="cat in categories"
                           :key="cat.name"
                           @click.prevent="navigateToCategory(cat.name)"
                           href="#"
                           :class="[
                               'px-4 h-full flex items-center text-lg font-medium border-b-4 transition-colors duration-200 cursor-pointer',
                               isCategoryActive(cat.name)
                                   ? 'border-primary text-primary bg-gray-50 dark:bg-gray-900 dark:text-blue-400 dark:border-blue-400'
                                   : 'border-transparent text-gray-700 dark:text-gray-300 hover:text-primary dark:hover:text-blue-400 hover:bg-gray-50 dark:hover:bg-gray-700'
                           ]">
                            {{ cat.name }}
                        </a>
                    </div>

                    <!-- Right Side Actions -->
                    <div class="flex items-center space-x-2">
                        <!-- Sidebar Toggle Button (List Pages Only, Mobile & Desktop) -->
                        <button v-if="isListPage" @click="toggleSidebar" class="p-2 rounded-md text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700 focus:outline-none" :title="isSidebarOpen ? t('nav.closeSidebar') : t('nav.openSidebar')">
                            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path v-if="isSidebarOpen" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                                <path v-else stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"></path>
                            </svg>
                        </button>

                        <!-- Theme Toggle -->
                        <button @click="toggleTheme" class="p-2 rounded-full text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700 focus:outline-none" :title="t('nav.toggleTheme')">
                            <svg class="w-6 h-6 hidden dark:block" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"></path></svg>
                            <svg class="w-6 h-6 block dark:hidden" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"></path></svg>
                        </button>

                        <!-- Login/Create/Edit/Logout Button -->
                        <button v-if="actionButtonConfig"
                                @click="actionButtonConfig.action"
                                class="p-2 rounded-full text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-primary dark:hover:text-blue-400 focus:outline-none"
                                :title="actionButtonConfig.title">
                            <!-- Create Icon (Plus) -->
                            <svg v-if="actionButtonConfig.icon === 'create'" class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path>
                            </svg>
                            <!-- Edit Icon -->
                            <svg v-else-if="actionButtonConfig.icon === 'edit'" class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path>
                            </svg>
                            <!-- Logout Icon -->
                            <svg v-else-if="actionButtonConfig.icon === 'logout'" class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"></path>
                            </svg>
                        </button>
                        <!-- Login Button -->
                        <router-link v-else-if="!isLoggedIn" to="/login" class="p-2 rounded-full text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-primary dark:hover:text-blue-400 focus:outline-none" :title="t('nav.login')">
                            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1"></path>
                            </svg>
                        </router-link>
                    </div>
                </div>
            </div>

            <!-- Mobile Menu (Dropdown) -->
            <div v-show="isMobileMenuOpen" class="md:hidden bg-white dark:bg-gray-800 border-t border-gray-200 dark:border-gray-700">
                <div class="px-2 pt-2 pb-3 space-y-1 sm:px-3">
                    <router-link to="/"
                        :class="[
                            'block px-3 py-2 rounded-md text-base font-medium',
                            isActive('/')
                                ? 'bg-gray-100 dark:bg-gray-900 text-primary dark:text-blue-400'
                                : 'text-gray-700 dark:text-gray-300 hover:text-primary dark:hover:text-blue-400 hover:bg-gray-50 dark:hover:bg-gray-700'
                        ]" @click="isMobileMenuOpen = false">
                        {{ t('nav.home') }}
                    </router-link>
                    <a v-for="cat in categories"
                       :key="cat.name"
                       @click.prevent="navigateToCategory(cat.name)"
                       href="#"
                       :class="[
                           'block px-3 py-2 rounded-md text-base font-medium cursor-pointer',
                           isCategoryActive(cat.name)
                               ? 'bg-gray-100 dark:bg-gray-900 text-primary dark:text-blue-400'
                               : 'text-gray-700 dark:text-gray-300 hover:text-primary dark:hover:text-blue-400 hover:bg-gray-50 dark:hover:bg-gray-700'
                       ]">
                        {{ cat.name }}
                    </a>
                    <!-- Login/Create/Edit/Logout Button -->
                    <button v-if="actionButtonConfig"
                            @click="actionButtonConfig.action(); isMobileMenuOpen = false"
                            class="block w-full text-left px-3 py-2 rounded-md text-base font-medium text-gray-700 dark:text-gray-300 hover:text-primary dark:hover:text-blue-400 hover:bg-gray-50 dark:hover:bg-gray-700">
                        {{ actionButtonConfig.title }}
                    </button>
                    <router-link v-else-if="!isLoggedIn" to="/login" class="block px-3 py-2 rounded-md text-base font-medium text-gray-700 dark:text-gray-300 hover:text-primary dark:hover:text-blue-400 hover:bg-gray-50 dark:hover:bg-gray-700" @click="isMobileMenuOpen = false">
                        {{ t('nav.login') }}
                    </router-link>
                </div>
            </div>
        </nav>
    `
}
