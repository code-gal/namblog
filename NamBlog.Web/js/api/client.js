import { config } from '../config.js';
import { getLocale } from '../i18n/index.js';

/**
 * Generic GraphQL request function
 * @param {string} query - GraphQL query or mutation
 * @param {object} [variables] - Variables for the query
 * @param {AbortSignal} [signal] - Optional abort signal for request cancellation
 * @returns {Promise<any>} - The data property of the response
 */
export async function request(query, variables = {}, signal = null) {
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
        const fetchOptions = {
            method: 'POST',
            headers: headers,
            body: JSON.stringify({ query, variables }),
        };

        // Add abort signal if provided
        if (signal) {
            fetchOptions.signal = signal;
        }

        const response = await fetch(config.GRAPHQL_ENDPOINT, fetchOptions);

        const result = await response.json();

        if (result.errors) {
            console.error('GraphQL Errors:', result.errors);
            throw new Error(result.errors[0].message);
        }

        return result.data;
    } catch (error) {
        // Don't log abort errors as they are intentional
        if (error.name !== 'AbortError') {
            console.error('API Request Failed:', error);
        }
        throw error;
    }
}
