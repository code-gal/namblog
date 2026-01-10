import { computed } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';

/**
 * 分页组件
 *
 * Props:
 * - pageInfo: { currentPage, pageSize, totalCount, totalPages, hasPreviousPage, hasNextPage }
 *
 * Events:
 * - @page-change: 页码变化时触发，传递新页码
 */
export default {
    props: {
        pageInfo: {
            type: Object,
            required: true,
            validator: (value) => {
                return value.currentPage !== undefined &&
                       value.totalPages !== undefined;
            }
        }
    },
    emits: ['page-change'],
    setup(props, { emit }) {
        const { t } = useI18n();
        const router = useRouter();

        // 计算页码列表（显示当前页前后各2页）
        const pageNumbers = computed(() => {
            const current = props.pageInfo.currentPage;
            const total = props.pageInfo.totalPages;
            const pages = [];

            // 如果总页数小于等于7，显示所有页码
            if (total <= 7) {
                for (let i = 1; i <= total; i++) {
                    pages.push(i);
                }
                return pages;
            }

            // 总是显示第一页
            pages.push(1);

            // 计算显示范围
            let start = Math.max(2, current - 2);
            let end = Math.min(total - 1, current + 2);

            // 如果当前页靠近开始
            if (current <= 4) {
                end = 5;
            }

            // 如果当前页靠近结束
            if (current >= total - 3) {
                start = total - 4;
            }

            // 添加省略号
            if (start > 2) {
                pages.push('...');
            }

            // 添加中间页码
            for (let i = start; i <= end; i++) {
                pages.push(i);
            }

            // 添加省略号
            if (end < total - 1) {
                pages.push('...');
            }

            // 总是显示最后一页
            if (total > 1) {
                pages.push(total);
            }

            return pages;
        });

        // 跳转到指定页
        const goToPage = (page) => {
            if (page < 1 || page > props.pageInfo.totalPages || page === props.pageInfo.currentPage) {
                return;
            }

            // 通过路由更新页码（保留其他查询参数）
            const query = { ...router.currentRoute.value.query, page };
            router.push({ query });

            // 触发事件通知父组件
            emit('page-change', page);
        };

        // 上一页
        const previousPage = () => {
            if (props.pageInfo.hasPreviousPage) {
                goToPage(props.pageInfo.currentPage - 1);
            }
        };

        // 下一页
        const nextPage = () => {
            if (props.pageInfo.hasNextPage) {
                goToPage(props.pageInfo.currentPage + 1);
            }
        };

        return {
            pageNumbers,
            goToPage,
            previousPage,
            nextPage,
            t
        };
    },
    template: `
        <div class="flex flex-col items-center space-y-4 mt-8 mb-6">
            <!-- 分页信息 -->
            <div class="text-sm text-gray-600 dark:text-gray-400">
                {{ t('pagination.totalArticles', { count: pageInfo.totalCount }) }}，
                {{ t('pagination.currentPage', { current: pageInfo.currentPage, total: pageInfo.totalPages }) }}
            </div>

            <!-- 分页控件 -->
            <div class="flex items-center space-x-2">
                <!-- 上一页按钮 -->
                <button
                    @click="previousPage"
                    :disabled="!pageInfo.hasPreviousPage"
                    :class="[
                        'px-3 py-2 rounded-lg text-sm font-medium transition-colors duration-200',
                        pageInfo.hasPreviousPage
                            ? 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-200 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700'
                            : 'bg-gray-100 dark:bg-gray-800 text-gray-400 dark:text-gray-600 cursor-not-allowed'
                    ]"
                    :title="t('pagination.previous')"
                >
                    ← {{ t('pagination.previous') }}
                </button>

                <!-- 页码列表 -->
                <div class="hidden md:flex items-center space-x-1">
                    <button
                        v-for="(page, index) in pageNumbers"
                        :key="index"
                        @click="page !== '...' && goToPage(page)"
                        :disabled="page === '...'"
                        :class="[
                            'min-w-[40px] h-10 rounded-lg text-sm font-medium transition-colors duration-200',
                            page === pageInfo.currentPage
                                ? 'bg-primary text-white'
                                : page === '...'
                                ? 'text-gray-400 dark:text-gray-600 cursor-default'
                                : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-200 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700'
                        ]"
                    >
                        {{ page }}
                    </button>
                </div>

                <!-- 移动端当前页显示 -->
                <div class="md:hidden px-4 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg text-sm font-medium text-gray-700 dark:text-gray-200">
                    {{ pageInfo.currentPage }} / {{ pageInfo.totalPages }}
                </div>

                <!-- 下一页按钮 -->
                <button
                    @click="nextPage"
                    :disabled="!pageInfo.hasNextPage"
                    :class="[
                        'px-3 py-2 rounded-lg text-sm font-medium transition-colors duration-200',
                        pageInfo.hasNextPage
                            ? 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-200 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700'
                            : 'bg-gray-100 dark:bg-gray-800 text-gray-400 dark:text-gray-600 cursor-not-allowed'
                    ]"
                    :title="t('pagination.next')"
                >
                    {{ t('pagination.next') }} →
                </button>
            </div>
        </div>
    `
};
