namespace NamBlog.API.Application.Common;

/// <summary>
/// 缓存键常量定义
/// 统一管理所有缓存键，避免硬编码导致的不一致问题
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// SEO 路径缓存键：seo:path:{slug}
    /// </summary>
    /// <param name="slug">文章 slug</param>
    /// <returns>缓存键</returns>
    public static string SeoPath(string slug) => $"seo:path:{slug}";

    /// <summary>
    /// Sitemap XML 缓存键
    /// </summary>
    public const string SitemapXml = "sitemap:xml";

    /// <summary>
    /// Robots.txt 缓存键
    /// </summary>
    public const string SitemapRobots = "sitemap:robots";

    /// <summary>
    /// 登录限流缓存键：login:ratelimit:{ipAddress}
    /// </summary>
    /// <param name="ipAddress">客户端 IP 地址</param>
    /// <returns>缓存键</returns>
    public static string LoginRateLimit(string ipAddress) => $"login:ratelimit:{ipAddress}";
}
