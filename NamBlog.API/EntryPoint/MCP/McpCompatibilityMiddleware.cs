using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NamBlog.API.EntryPoint.MCP
{
    /// <summary>
    /// MCP 兼容性中间件：修复某些客户端缺失或不完整的 Accept 头
    /// </summary>
    public class McpCompatibilityMiddleware(
        RequestDelegate next,
        ILogger<McpCompatibilityMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            // 仅处理 MCP 端点
            if (context.Request.Path.StartsWithSegments("/mcp"))
            {
                var accept = context.Request.Headers.Accept.ToString();

                // 修复 Accept 头：确保同时包含 application/json 和 text/event-stream
                // 某些 MCP 客户端只发送其中一种或完全缺失
                if (string.IsNullOrEmpty(accept))
                {
                    context.Request.Headers.Accept = "application/json, text/event-stream";
                    logger.LogDebug("MCP请求缺少Accept头，已自动添加");
                }
                else if (accept.Contains("text/event-stream") && !accept.Contains("application/json"))
                {
                    context.Request.Headers.Accept = "application/json, text/event-stream";
                    logger.LogDebug("MCP请求Accept头缺少application/json，已自动添加");
                }
                else if (accept.Contains("application/json") && !accept.Contains("text/event-stream"))
                {
                    context.Request.Headers.Accept = "application/json, text/event-stream";
                    logger.LogDebug("MCP请求Accept头缺少text/event-stream，已自动添加");
                }
            }

            await next(context);
        }
    }
}
