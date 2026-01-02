import { ref } from 'vue';

// Toast状态管理
const toasts = ref([]);
let nextId = 1;

/**
 * 显示Toast通知
 * @param {string} message - 消息内容
 * @param {string} type - 类型: 'success', 'error', 'warning', 'info'
 * @param {number} duration - 显示时长(毫秒)，默认3000
 */
export function showToast(message, type = 'info', duration = 3000) {
    const id = nextId++;
    const toast = {
        id,
        message,
        type,
        visible: true
    };
    
    toasts.value.push(toast);
    
    // 自动移除
    setTimeout(() => {
        removeToast(id);
    }, duration);
}

function removeToast(id) {
    const index = toasts.value.findIndex(t => t.id === id);
    if (index !== -1) {
        toasts.value.splice(index, 1);
    }
}

export default {
    setup() {
        return {
            toasts,
            removeToast
        };
    },
    template: `
        <div class="fixed top-4 right-4 z-50 space-y-2">
            <transition-group name="toast">
                <div 
                    v-for="toast in toasts" 
                    :key="toast.id"
                    :class="[
                        'px-6 py-3 rounded-lg shadow-lg text-white flex items-center space-x-3 min-w-[300px] max-w-md',
                        toast.type === 'success' ? 'bg-green-500' : '',
                        toast.type === 'error' ? 'bg-red-500' : '',
                        toast.type === 'warning' ? 'bg-yellow-500' : '',
                        toast.type === 'info' ? 'bg-blue-500' : ''
                    ]"
                >
                    <!-- Icon -->
                    <div class="flex-shrink-0">
                        <svg v-if="toast.type === 'success'" class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
                        </svg>
                        <svg v-else-if="toast.type === 'error'" class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/>
                        </svg>
                        <svg v-else-if="toast.type === 'warning'" class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
                        </svg>
                        <svg v-else class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"/>
                        </svg>
                    </div>
                    
                    <!-- Message -->
                    <div class="flex-1 text-sm font-medium">
                        {{ toast.message }}
                    </div>
                    
                    <!-- Close Button -->
                    <button @click="removeToast(toast.id)" class="flex-shrink-0 ml-2 text-white hover:text-gray-200">
                        <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"/>
                        </svg>
                    </button>
                </div>
            </transition-group>
        </div>
    `
};
