/**
 * SSE 流式生成 HTML 客户端
 * 使用 Fetch API + ReadableStream 处理 Server-Sent Events
 *
 * 注意：不使用 EventSource API，因为它不支持自定义 HTTP Headers（无法传递 JWT）
 */

import { config } from '../config.js';

/**
 * 流式生成 HTML
 * @param {Object} options - 配置选项
 * @param {string} options.markdown - Markdown 内容
 * @param {string} [options.customPrompt] - 自定义 Prompt
 * @param {Function} options.onProgress - 进度回调 (progress: number) => void
 * @param {Function} [options.onChunk] - 内容块回调 (chunk: string) => void
 * @param {Function} options.onComplete - 完成回调 (html: string) => void
 * @param {Function} options.onError - 错误回调 (error: Error) => void
 * @param {AbortSignal} [options.signal] - 取消信号
 * @returns {Promise<void>}
 */
export async function streamGenerateHtml(options) {
    const {
        markdown,
        customPrompt = null,
        onProgress,
        onChunk,
        onComplete,
        onError,
        signal
    } = options;

    // 验证必需参数
    if (!markdown) {
        throw new Error('markdown 参数不能为空');
    }
    if (!onProgress || !onComplete || !onError) {
        throw new Error('onProgress, onComplete, onError 回调函数是必需的');
    }

    // 构建 URL（使用配置的端点）
    const SSE_ENDPOINT = `${config.API_BASE_URL}/api/stream/generate-html`;
    const url = new URL(SSE_ENDPOINT);
    url.searchParams.append('markdown', markdown);
    if (customPrompt) {
        url.searchParams.append('customPrompt', customPrompt);
    }

    // 获取认证 Token
    const token = localStorage.getItem('auth_token');
    if (!token) {
        onError(new Error('未登录，请先登录'));
        return;
    }

    // 累积 HTML 内容
    let accumulatedHtml = '';

    // SSE 消息缓冲区（处理跨chunk的消息）
    let buffer = '';

    try {
        const response = await fetch(url.toString(), {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'text/event-stream'
            },
            signal
        });

        if (!response.ok) {
            if (response.status === 401) {
                throw new Error('认证失败，请重新登录');
            }
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        if (!response.body) {
            throw new Error('响应体为空');
        }

        // 读取流
        const reader = response.body.getReader();
        const decoder = new TextDecoder();

        while (true) {
            const { done, value } = await reader.read();

            if (done) {
                break;
            }

            // 解码数据块
            const chunk = decoder.decode(value, { stream: true });
            buffer += chunk;

            // 处理 SSE 消息（格式：data: {...}\n\n）
            const messages = buffer.split('\n\n');

            // 保留最后一个不完整的消息
            buffer = messages.pop() || '';

            for (const message of messages) {
                if (!message.trim()) continue;

                // 解析 SSE data 行
                const lines = message.split('\n');
                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        const jsonStr = line.substring(6); // 移除 "data: " 前缀

                        try {
                            const data = JSON.parse(jsonStr);

                            // 更新进度
                            if (typeof data.progress === 'number') {
                                onProgress(data.progress);
                            }

                            // 处理内容块
                            if (data.chunk) {
                                accumulatedHtml += data.chunk;
                                if (onChunk) {
                                    onChunk(data.chunk);
                                }
                            }

                            // 处理错误
                            if (data.error) {
                                throw new Error(data.error);
                            }

                            // 处理完成状态
                            if (data.status === 'completed') {
                                // 使用累积的 HTML 或最后返回的完整 HTML
                                const finalHtml = data.chunk || accumulatedHtml;
                                onComplete(finalHtml);
                                return;
                            }

                        } catch (parseError) {
                            console.warn('解析 SSE 消息失败:', line, parseError);
                        }
                    }
                }
            }
        }

        // 流结束但没有收到 completed 状态
        if (accumulatedHtml) {
            onComplete(accumulatedHtml);
        } else {
            throw new Error('流式传输意外结束，未收到完整数据');
        }

    } catch (error) {
        if (error.name === 'AbortError') {
            onError(new Error('生成已取消'));
        } else {
            console.error('流式生成失败:', error);
            onError(error);
        }
    }
}

/**
 * 使用 Promise 方式调用流式生成（简化版本，仅获取最终 HTML）
 * @param {string} markdown - Markdown 内容
 * @param {string} [customPrompt] - 自定义 Prompt
 * @param {Function} [onProgress] - 可选的进度回调
 * @returns {Promise<string>} 生成的 HTML
 */
export function generateHtmlStream(markdown, customPrompt = null, onProgress = null) {
    return new Promise((resolve, reject) => {
        streamGenerateHtml({
            markdown,
            customPrompt,
            onProgress: onProgress || (() => {}),
            onComplete: resolve,
            onError: reject
        });
    });
}
