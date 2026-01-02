using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NamBlog.API.Application.Services;

namespace NamBlog.API.Extensions;

/// <summary>
/// SEO 优化中间件
/// 检测爬虫访问时，将前端路由重写到静态 HTML 文件路径
/// </summary>
public static class SeoMiddleware
{
    /// <summary>
    /// 爬虫和社交分享机器人的 User-Agent 特征
    /// </summary>
    private static readonly string[] _botUserAgents =
    [
        // 搜索引擎爬虫
        "googlebot", "bingbot", "slurp", "duckduckbot",
        "baiduspider", "yandexbot", "sogou", "exabot",
        "yahoo", "msn", "teoma",

        // 社交分享预览
        "facebookexternalhit", "facebookcatalog",
        "twitterbot", "linkedinbot", "slackbot",
        "whatsapp", "telegram", "discordbot",

        // 其他工具
        "archive.org_bot", "ia_archiver"
    ];

    /// <summary>
    /// 启用 SEO 优化中间件
    /// </summary>
    public static IApplicationBuilder UseSeoOptimization(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var path = context.Request.Path.Value;

            // 只处理文章详情页 + 爬虫/分享机器人
            if (path?.StartsWith("/article/", StringComparison.OrdinalIgnoreCase) == true
                && IsBot(userAgent))
            {
                var slug = path.Replace("/article/", "", StringComparison.OrdinalIgnoreCase).Trim('/');

                if (!string.IsNullOrWhiteSpace(slug))
                {
                    var staticPath = await GetStaticPathAsync(context, slug);

                    if (staticPath != null)
                    {
                        // 重写路径到静态文件
                        context.Request.Path = staticPath;

                        // 记录日志（改为 Information 级别，确保可见）
                        // var logger = context.RequestServices.GetService<ILogger<Program>>();
                        // logger?.LogDebug(
                        //     "SEO 路径重写: {OriginalPath} -> {StaticPath} | UA: {UserAgent}",
                        //     path, staticPath, GetShortUserAgent(userAgent));
                    }
                    // else
                    // {
                    //     var logger = context.RequestServices.GetService<ILogger<Program>>();
                    //     logger?.LogWarning(
                    //         "SEO 中间件：未找到文章主版本，slug={Slug} | UA: {UserAgent}",
                    //         slug, GetShortUserAgent(userAgent));
                    // }
                }
            }

            await next();
        });

        return app;
    }

    /// <summary>
    /// 检测是否是爬虫或分享机器人
    /// </summary>
    private static bool IsBot(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return false;

        var lowerUserAgent = userAgent.ToLower();
        return _botUserAgents.Any(bot => lowerUserAgent.Contains(bot));
    }

    /// <summary>
    /// 获取静态文件路径（带缓存）
    /// </summary>
    private static async Task<string?> GetStaticPathAsync(HttpContext context, string slug)
    {
        var cache = context.RequestServices.GetRequiredService<IMemoryCache>();
        var cacheKey = $"seo:path:{slug}";

        // 尝试从缓存获取
        if (cache.TryGetValue(cacheKey, out string? cachedPath))
        {
            return cachedPath;
        }

        try
        {
            // 缓存未命中，查询数据库
            var articleService = context.RequestServices.GetRequiredService<ArticleQueryService>();
            var versionName = await articleService.GetMainVersionNameAsync(slug);

            if (versionName == null)
                return null;

            // 构建静态文件路径（匹配 MiddlewareExtensions 中的 /posts 配置）
            // 注意：versionName 包含空格，需要 URL 编码
            var encodedVersionName = Uri.EscapeDataString(versionName);
            var staticPath = $"/posts/{slug}/{encodedVersionName}/index.html";

            // 缓存 10 分钟（平衡性能和实时性）
            cache.Set(cacheKey, staticPath, TimeSpan.FromMinutes(10));

            return staticPath;
        }
        catch (OperationCanceledException)
        {
            // 请求被取消（通常是客户端断开连接），直接返回null
            return null;
        }
        catch (Exception)
        {
            // 其他异常也返回null，让请求继续处理（不影响正常访问）
            return null;
        }
    }

    /// <summary>
    /// 获取简短的 User-Agent（用于日志）
    /// </summary>
    private static string GetShortUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown";

        // 提取关键词
        foreach (var bot in _botUserAgents)
        {
            if (userAgent.Contains(bot, StringComparison.OrdinalIgnoreCase))
                return bot;
        }

        // 截取前50个字符
        return userAgent.Length > 50 ? userAgent[..50] + "..." : userAgent;
    }

    /// <summary>
    /// 清除指定文章的 SEO 缓存（当文章更新时调用）
    /// </summary>
    public static void InvalidateSeoCache(IMemoryCache cache, string slug)
    {
        cache.Remove($"seo:path:{slug}");
    }
}
