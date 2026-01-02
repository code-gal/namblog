import { ref, reactive, computed, onMounted } from 'vue';
import { auth } from '../api/auth.js';
import { useRouter, useRoute } from 'vue-router';

export default {
    setup() {
        const router = useRouter();
        const route = useRoute();

        const form = reactive({
            username: '',
            password: ''
        });
        const isLoading = ref(false);
        const errorMsg = ref('');
        const rememberMe = ref(false); // 记住用户名选项

        // 重试次数限制
        const MAX_ATTEMPTS = 5;
        const LOCK_DURATION = 5 * 60 * 1000; // 5分钟
        const loginAttempts = ref(0);
        const isLocked = ref(false);
        const lockUntil = ref(null);
        const remainingTime = ref(0);

        // 加载记住的用户名
        const loadRememberedUsername = () => {
            const remembered = localStorage.getItem('rememberedUsername');
            if (remembered) {
                form.username = remembered;
                rememberMe.value = true;
            }
        };

        // 保存或清除记住的用户名
        const handleRememberMe = (username) => {
            if (rememberMe.value) {
                localStorage.setItem('rememberedUsername', username);
            } else {
                localStorage.removeItem('rememberedUsername');
            }
        };

        // 检查登录锁定状态
        const checkLockStatus = () => {
            const locked = localStorage.getItem('loginLocked');
            const attempts = localStorage.getItem('loginAttempts');

            if (locked) {
                const lockTime = parseInt(locked);
                if (Date.now() < lockTime) {
                    isLocked.value = true;
                    lockUntil.value = new Date(lockTime);
                    updateRemainingTime();
                    return true;
                } else {
                    // 锁定已过期，清除
                    localStorage.removeItem('loginLocked');
                    localStorage.removeItem('loginAttempts');
                }
            }

            if (attempts) {
                loginAttempts.value = parseInt(attempts);
            }

            return false;
        };

        // 更新剩余锁定时间
        const updateRemainingTime = () => {
            if (lockUntil.value) {
                const diff = lockUntil.value.getTime() - Date.now();
                if (diff > 0) {
                    remainingTime.value = Math.ceil(diff / 1000); // 秒
                    setTimeout(updateRemainingTime, 1000);
                } else {
                    isLocked.value = false;
                    lockUntil.value = null;
                    remainingTime.value = 0;
                    localStorage.removeItem('loginLocked');
                    localStorage.removeItem('loginAttempts');
                }
            }
        };

        // 格式化剩余时间显示
        const formattedRemainingTime = computed(() => {
            const minutes = Math.floor(remainingTime.value / 60);
            const seconds = remainingTime.value % 60;
            return `${minutes}:${seconds.toString().padStart(2, '0')}`;
        });

        // 增强表单校验
        const validateForm = () => {
            errorMsg.value = '';

            if (!form.username || !form.password) {
                errorMsg.value = '请输入用户名和密码';
                return false;
            }

            if (form.username.length < 3) {
                errorMsg.value = '用户名至少3个字符';
                return false;
            }

            if (form.username.length > 20) {
                errorMsg.value = '用户名最多20个字符';
                return false;
            }

            if (form.password.length < 6) {
                errorMsg.value = '密码至少6个字符';
                return false;
            }

            if (form.password.length > 50) {
                errorMsg.value = '密码最多50个字符';
                return false;
            }

            return true;
        };

        // 处理登录失败
        const handleLoginFailure = () => {
            loginAttempts.value++;
            localStorage.setItem('loginAttempts', loginAttempts.value.toString());

            if (loginAttempts.value >= MAX_ATTEMPTS) {
                const lockTime = Date.now() + LOCK_DURATION;
                localStorage.setItem('loginLocked', lockTime.toString());
                isLocked.value = true;
                lockUntil.value = new Date(lockTime);
                updateRemainingTime();
                errorMsg.value = `登录失败次数过多，账号已锁定 ${formattedRemainingTime.value}`;
            }
        };

        // 处理登录成功
        const handleLoginSuccess = () => {
            // 保存或清除记住的用户名
            handleRememberMe(form.username);

            // 清除登录失败记录
            localStorage.removeItem('loginAttempts');
            localStorage.removeItem('loginLocked');
            loginAttempts.value = 0;
            isLocked.value = false;

            // 获取redirect参数，跳转到原目标页面或主页
            const redirect = route.query.redirect || '/';

            // XSS防护：确保redirect是内部路由（不是外部链接）
            if (redirect.startsWith('http://') || redirect.startsWith('https://') || redirect.startsWith('//')) {
                router.push('/');
            } else {
                router.push(redirect);
            }
        };

        const handleLogin = async () => {
            // 检查是否被锁定
            if (isLocked.value) {
                errorMsg.value = `账号已锁定，请等待 ${formattedRemainingTime.value} 后再试`;
                return;
            }

            // 表单校验
            if (!validateForm()) {
                return;
            }

            isLoading.value = true;
            errorMsg.value = '';

            try {
                const result = await auth.login(form.username, form.password);
                if (result.success) {
                    handleLoginSuccess();
                } else {
                    handleLoginFailure();
                    errorMsg.value = result.message || '登录失败，请检查用户名和密码';
                }
            } catch (e) {
                handleLoginFailure();
                errorMsg.value = '登录发生错误，请重试';
                console.error('Login error:', e);
            } finally {
                isLoading.value = false;
            }
        };

        // 组件挂载时检查锁定状态并加载记住的用户名
        onMounted(() => {
            checkLockStatus();
            loadRememberedUsername();
        });

        return {
            form,
            isLoading,
            errorMsg,
            handleLogin,
            isLocked,
            formattedRemainingTime,
            loginAttempts,
            MAX_ATTEMPTS,
            rememberMe // 导出记住用户名选项
        };
    },
    template: `
        <div class="min-h-screen flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
            <div class="max-w-md w-full bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                <h2 class="text-2xl font-bold text-gray-800 dark:text-white text-center">管理员登录</h2>

                <!-- 锁定提示 -->
                <div v-if="isLocked" class="mt-4 p-3 bg-red-100 dark:bg-red-900 border border-red-400 dark:border-red-700 rounded">
                    <p class="text-red-700 dark:text-red-200 text-sm text-center">
                        🔒 登录失败次数过多，账号已锁定
                    </p>
                    <p class="text-red-600 dark:text-red-300 text-xs text-center mt-1">
                        剩余时间: {{ formattedRemainingTime }}
                    </p>
                </div>

                <form @submit.prevent="handleLogin" class="mt-6">
                    <div class="mb-4">
                        <label class="block text-gray-700 dark:text-gray-300 text-sm font-bold mb-2" for="username">
                            用户名
                        </label>
                        <input
                            v-model="form.username"
                            :disabled="isLocked"
                            class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline dark:bg-gray-700 dark:text-white dark:border-gray-600 disabled:opacity-50 disabled:cursor-not-allowed"
                            id="username"
                            type="text"
                            autocomplete="username"
                            placeholder="请输入用户名（3-20字符）"
                            maxlength="20">
                    </div>

                    <div class="mb-4">
                        <label class="block text-gray-700 dark:text-gray-300 text-sm font-bold mb-2" for="password">
                            密码
                        </label>
                        <input
                            v-model="form.password"
                            :disabled="isLocked"
                            class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline dark:bg-gray-700 dark:text-white dark:border-gray-600 disabled:opacity-50 disabled:cursor-not-allowed"
                            id="password"
                            type="password"
                            autocomplete="current-password"
                            placeholder="请输入密码（至少6个字符）"
                            maxlength="50">
                    </div>

                    <!-- 记住用户名 -->
                    <div class="mb-4 flex items-center">
                        <input
                            v-model="rememberMe"
                            :disabled="isLocked"
                            type="checkbox"
                            id="rememberMe"
                            class="w-4 h-4 text-blue-600 bg-gray-100 border-gray-300 rounded focus:ring-blue-500 dark:bg-gray-700 dark:border-gray-600 disabled:opacity-50">
                        <label for="rememberMe" class="ml-2 text-sm text-gray-600 dark:text-gray-400 cursor-pointer">
                            记住用户名
                        </label>
                    </div>

                    <!-- 错误提示 -->
                    <div v-if="errorMsg" class="mb-4 text-red-500 text-sm text-center">
                        {{ errorMsg }}
                    </div>

                    <!-- 剩余尝试次数提示 -->
                    <div v-if="!isLocked && loginAttempts > 0" class="mb-4 text-yellow-600 dark:text-yellow-400 text-xs text-center">
                        登录失败 {{ loginAttempts }} 次，还剩 {{ MAX_ATTEMPTS - loginAttempts }} 次机会
                    </div>

                    <div class="flex items-center justify-center">
                        <button
                            :disabled="isLoading || isLocked"
                            class="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded focus:outline-none focus:shadow-outline w-full transition duration-300 disabled:opacity-50 disabled:cursor-not-allowed"
                            type="submit">
                            {{ isLoading ? '登录中...' : isLocked ? '账号已锁定' : '登录' }}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    `
}
