using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NamBlog.API.Application.Common;
using NamBlog.API.Application.Services;
using NamBlog.API.Infrastructure.Common;

namespace NamBlog.API.Extensions;

/// <summary>
/// SEO 优化中间件
/// 检测爬虫访问时，将前端路由重写到静态 HTML 文件路径
/// </summary>
public static class SeoMiddleware
{
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
                && IsBot(context, userAgent))
            {
                var slug = path.Replace("/article/", "", StringComparison.OrdinalIgnoreCase).Trim('/');

                if (!string.IsNullOrWhiteSpace(slug))
                {
                    var staticPath = await GetStaticPathAsync(context, slug, userAgent);

                    if (staticPath != null)
                    {
                        // 重写路径到静态文件
                        context.Request.Path = staticPath;
                    }
                }
            }

            await next();
        });

        return app;
    }

    /// <summary>
    /// 检测是否是爬虫或分享机器人
    /// </summary>
    private static bool IsBot(HttpContext context, string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return false;

        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        var botUserAgents = configuration.GetSection("Seo:BotUserAgents").Get<string[]>() ?? [];
        var lowerUserAgent = userAgent.ToLower();

        // 使用 StringComparison.OrdinalIgnoreCase 进行不区分大小写的比较
        return botUserAgents.Any(bot => userAgent.Contains(bot, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取静态文件路径（带缓存）
    /// </summary>
    private static async Task<string?> GetStaticPathAsync(HttpContext context, string slug, string userAgent)
    {
        var cache = context.RequestServices.GetRequiredService<IMemoryCache>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var cacheKey = CacheKeys.SeoPath(slug);

        // 尝试从缓存获取
        if (cache.TryGetValue(cacheKey, out string? cachedPath))
        {
            return cachedPath;
        }

        try
        {
            // 缓存未命中，查询数据库
            var articleService = context.RequestServices.GetRequiredService<ArticleQueryService>();
            var seoInfo = await articleService.GetSeoArticleInfoAsync(slug);

            if (seoInfo == null)
            {
                logger.LogDebug("SEO 中间件：文章不存在/未发布/版本无效，slug={Slug}", slug);
                return null;
            }

            // 使用 FilePathHelper 构建正确的 HTML 相对路径
            var htmlRelativePath = FilePathHelper.GetHtmlRelativePath(
                seoInfo.FilePath,
                seoInfo.FileName,
                seoInfo.VersionName);

            // 注意：版本名称包含空格，需要 URL 编码
            var encodedPath = string.Join("/",
                htmlRelativePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Uri.EscapeDataString));

            var staticPath = $"/posts/{encodedPath}/index.html";

            // 缓存 10 分钟（平衡性能和实时性）
            cache.Set(cacheKey, staticPath, TimeSpan.FromMinutes(10));

            logger.LogInformation("SEO 路径重写: /article/{Slug} -> {StaticPath} | UA: {UserAgent}",
                slug, staticPath, GetShortUserAgent(userAgent));

            return staticPath;
        }
        catch (OperationCanceledException)
        {
            // 请求被取消（通常是客户端断开连接），直接返回null
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SEO 中间件异常: slug={Slug} | UA: {UserAgent}",
                slug, GetShortUserAgent(userAgent));
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

        // 截取前50个字符
        return userAgent.Length > 50 ? userAgent[..50] + "..." : userAgent;
    }

    /// <summary>
    /// 清除指定文章的 SEO 缓存（当文章更新时调用）
    /// </summary>
    public static void InvalidateSeoCache(IMemoryCache cache, string slug)
    {
        cache.Remove(CacheKeys.SeoPath(slug));
    }
}
