import { request } from './client.js';
import { store } from '../store.js';

export const auth = {
    /**
     * Login with username and password
     * @param {string} username 
     * @param {string} password 
     * @returns {Promise<boolean>} success
     */
    async login(username, password) {
        const mutation = `
            mutation Login($username: String!, $password: String!) {
                auth {
                    login(username: $username, password: $password) {
                        success
                        message
                        token
                        errorCode
                    }
                }
            }
        `;

        try {
            const data = await request(mutation, { username, password });
            const result = data.auth.login;

            if (result.success && result.token) {
                store.setToken(result.token);
                // Ideally fetch user info here if available
                store.setUser({ username }); 
                return { success: true };
            } else {
                return { 
                    success: false, 
                    message: result.message || 'Login failed' 
                };
            }
        } catch (error) {
            return { 
                success: false, 
                message: error.message || 'Network error' 
            };
        }
    },

    logout() {
        store.setToken(null);
        store.setUser(null);
        // Optional: Redirect to home
        window.location.href = '/';
    },

    isAuthenticated() {
        return store.isAuthenticated;
    }
};
