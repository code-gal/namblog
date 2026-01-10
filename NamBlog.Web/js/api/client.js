import { config } from '../config.js';
import { getLocale } from '../i18n/index.js';

/**
 * Generic GraphQL request function
 * @param {string} query - GraphQL query or mutation
 * @param {object} [variables] - Variables for the query
 * @returns {Promise<any>} - The data property of the response
 */
export async function request(query, variables = {}) {
    const headers = {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        'Accept-Language': getLocale(), // Add language preference header
    };

    const token = localStorage.getItem('auth_token');
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    try {
        const response = await fetch(config.GRAPHQL_ENDPOINT, {
            method: 'POST',
            headers: headers,
            body: JSON.stringify({ query, variables }),
        });

        const result = await response.json();

        if (result.errors) {
            console.error('GraphQL Errors:', result.errors);
            throw new Error(result.errors[0].message);
        }

        return result.data;
    } catch (error) {
        console.error('API Request Failed:', error);
        throw error;
    }
}
