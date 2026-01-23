using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NamBlog.API.Application.Common;
using NamBlog.API.Application.DTOs;
using NamBlog.API.Domain.Entities;
using NamBlog.API.Domain.Interfaces;

namespace NamBlog.API.Application.Services
{
    /// <summary>
    /// Sitemap 服务
    /// 负责生成 sitemap.xml 和 robots.txt
    /// </summary>
    public class SitemapService(
        IPostRepository postRepository,
        IMemoryCache cache,
        IOptionsSnapshot<BlogInfo> blogSettings,
        ILogger<SitemapService> logger)
    {
        private readonly IPostRepository _postRepository = postRepository;
        private readonly IMemoryCache _cache = cache;
        private readonly BlogInfo _blogSettings = blogSettings.Value;
        private readonly ILogger<SitemapService> _logger = logger;

        /// <summary>
        /// 生成 sitemap.xml
        /// </summary>
        public async Task<string> GenerateSitemapXmlAsync()
        {
            // 尝试从缓存获取
            if (_cache.TryGetValue(CacheKeys.SitemapXml, out string? cachedXml))
            {
                _logger.LogDebug("Sitemap 缓存命中");
                return cachedXml!;
            }

            _logger.LogInformation("生成 Sitemap XML");

            // 查询所有已发布且主版本有效的文章
            // 注意：SQLite 不支持 DateTimeOffset 的 ORDER BY，需要先查询再在内存中排序
            var posts = await _postRepository.GetAll()
                .Where(p => p.IsPublished && p.MainVersion != null && p.MainVersion.ValidationStatus == HtmlValidationStatus.Valid)
                .ToListAsync();

            // 在内存中按最后修改时间排序
            posts = posts.OrderByDescending(p => p.LastModified).ToList();

            // 获取站点域名
            var baseUrl = GetBaseUrl();

            // 构建 XML
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            // 添加首页
            xml.AppendLine("  <url>");
            xml.AppendLine($"    <loc>{baseUrl}</loc>");
            xml.AppendLine("    <changefreq>daily</changefreq>");
            xml.AppendLine("    <priority>1.0</priority>");
            xml.AppendLine("  </url>");

            // 添加文章
            foreach (var post in posts)
            {
                if (string.IsNullOrWhiteSpace(post.Slug))
                    continue;

                xml.AppendLine("  <url>");
                xml.AppendLine($"    <loc>{baseUrl}/article/{Uri.EscapeDataString(post.Slug)}</loc>");
                xml.AppendLine($"    <lastmod>{post.LastModified:yyyy-MM-dd}</lastmod>");
                xml.AppendLine("    <changefreq>weekly</changefreq>");
                xml.AppendLine("    <priority>0.8</priority>");
                xml.AppendLine("  </url>");
            }

            xml.AppendLine("</urlset>");

            var result = xml.ToString();

            // 缓存 10 分钟
            _cache.Set(CacheKeys.SitemapXml, result, TimeSpan.FromMinutes(10));

            _logger.LogInformation("Sitemap 生成完成，包含 {Count} 篇文章", posts.Count);

            return result;
        }

        /// <summary>
        /// 生成 robots.txt
        /// </summary>
        public string GenerateRobotsTxt()
        {
            // 尝试从缓存获取
            if (_cache.TryGetValue(CacheKeys.SitemapRobots, out string? cachedTxt))
            {
                _logger.LogDebug("Robots.txt 缓存命中");
                return cachedTxt!;
            }

            _logger.LogInformation("生成 Robots.txt");

            var baseUrl = GetBaseUrl();

            var txt = new StringBuilder();
            txt.AppendLine("User-agent: *");
            txt.AppendLine("Allow: /");
            txt.AppendLine();
            txt.AppendLine("# 禁止爬取 API 和 GraphQL 端点");
            txt.AppendLine("Disallow: /api/");
            txt.AppendLine("Disallow: /graphql");
            txt.AppendLine("Disallow: /graphql/");
            txt.AppendLine();
            txt.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");

            var result = txt.ToString();

            // 缓存 10 分钟
            _cache.Set(CacheKeys.SitemapRobots, result, TimeSpan.FromMinutes(10));

            return result;
        }

        /// <summary>
        /// 获取站点基础 URL（从配置读取）
        /// </summary>
        private string GetBaseUrl()
        {
            if (string.IsNullOrWhiteSpace(_blogSettings.Domain))
            {
                throw new InvalidOperationException("Blog.Domain 配置未设置，无法生成 sitemap");
            }

            var domain = _blogSettings.Domain.TrimEnd('/');

            // 如果域名没有协议，添加 https
            if (!domain.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                domain = $"https://{domain}";
            }

            return domain;
        }
    }
}
