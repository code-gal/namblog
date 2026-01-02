import { reactive, readonly } from 'vue';

const state = reactive({
    user: null,
    token: localStorage.getItem('auth_token'),
    // Context for smart navigation (e.g., "Edit" button)
    context: {
        type: 'home', // 'home', 'list', 'article', 'editor'
        slug: null    // slug of the article to edit
    },
    articlesCache: null,
    articlesCacheTime: 0
});

export const store = {
    state: readonly(state),

    setUser(user) {
        state.user = user;
    },

    setToken(token) {
        state.token = token;
        if (token) {
            localStorage.setItem('auth_token', token);
        } else {
            localStorage.removeItem('auth_token');
        }
    },

    setContext(type, slug = null) {
        state.context.type = type;
        state.context.slug = slug;
    },

    setArticlesCache(articles) {
        state.articlesCache = articles;
        state.articlesCacheTime = Date.now();
    },

    clearArticlesCache() {
        state.articlesCache = null;
        state.articlesCacheTime = 0;
    },

    get isAuthenticated() {
        return !!state.token;
    }
};
