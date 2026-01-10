import { createApp } from 'vue';
import { createRouter, createWebHistory, createWebHashHistory } from 'vue-router';
import App from './App.js';
import Home from './views/Home.js';
import Login from './views/Login.js';
import Article from './views/Article.js';
import Editor from './views/Editor.js';
import Category from './views/Category.js';
import Tag from './views/Tag.js';
import { store } from './store.js';
import { initPWA } from './pwa/sw-register.js';

// Define Routes
const routes = [
    { path: '/', component: Home },
    { path: '/login', component: Login },
    { path: '/article/:slug', component: Article },
    {
        path: '/editor',
        component: Editor,
        meta: { requiresAuth: true } // 需要登录
    },
    {
        path: '/editor/:slug',
        component: Editor,
        meta: { requiresAuth: true } // 需要登录
    },
    { path: '/category/:category', component: Category },
    { path: '/tag/:tag', component: Tag },
];

// 根据环境选择路由模式
// 开发环境（本地/内网）使用 Hash 模式
// 生产环境（公网域名）使用 History 模式以支持 SEO
//
// 判断逻辑：检测 hostname
// - localhost/127.0.0.1/私有IP段 为开发环境
// 使用 APP_CONFIG.DEV_MODE 判断路由模式
// - 开发模式（DEV_MODE: true）：Hash 模式（#/），无需后端配置
// - 生产模式（DEV_MODE: false）：History 模式（/），需要后端支持 SPA 路由
const isDevelopment = window.APP_CONFIG.DEV_MODE;

const routerHistory = isDevelopment
    ? createWebHashHistory()      // 开发：Hash 模式（#/article/xxx）
    : createWebHistory();         // 生产：History 模式（/article/xxx，SEO 友好）

// Create Router
const router = createRouter({
    history: routerHistory,
    routes,
});

// 路由守卫：保护需要认证的页面
router.beforeEach((to, from, next) => {
    const requiresAuth = to.matched.some(record => record.meta.requiresAuth);
    const isAuthenticated = store.isAuthenticated;

    if (requiresAuth && !isAuthenticated) {
        // 未登录且访问需要权限的页面，跳转到登录页并记录目标路由
        next({
            path: '/login',
            query: { redirect: to.fullPath }
        });
    } else if (to.path === '/login' && isAuthenticated) {
        // 已登录用户访问登录页，直接跳转到主页
        next('/');
    } else {
        next(); // 正常放行
    }
});

// Create App
const app = createApp(App);
app.use(router);
app.mount('#app');

// 初始化 PWA（Service Worker 注册）
if ('serviceWorker' in navigator) {
    initPWA().catch(err => {
        console.error('[PWA] 初始化失败:', err);
    });
}
