using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NamBlog.API.Application.DTOs;
using NamBlog.API.Application.Services;
using NamBlog.API.Infrastructure.Persistence;

namespace NamBlog.API.Extensions;

/// <summary>
/// 中间件配置扩展
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// 配置静态文件服务
    /// </summary>
    public static IApplicationBuilder UseStaticFileServices(this IApplicationBuilder app, IWebHostEnvironment env, string dataRootPath)
    {
        // 启用默认文件（如 index.html），必须在 UseStaticFiles 之前调用
        app.UseDefaultFiles();

        // wwwroot 目录
        app.UseStaticFiles();

        // HTML 文章目录（通过 /posts/ 路由访问）
        app.UseStaticFiles(CreateStaticFileOptions(
            env,
            Path.Combine(dataRootPath, "articles", "html"),
            "/posts",
            maxAge: 600));

        // 公开资源目录（图片、JS、CSS等，通过 /files/ 路由访问）
        app.UseStaticFiles(CreateStaticFileOptions(
            env,
            Path.Combine(dataRootPath, "resources"),
            "/resources",
            maxAge: 86400));

        return app;
    }

    /// <summary>
    /// 创建静态文件配置选项
    /// </summary>
    private static StaticFileOptions CreateStaticFileOptions(
        IWebHostEnvironment env,
        string relativePath,
        string requestPath,
        int maxAge)
    {
        var physicalPath = Path.Combine(env.ContentRootPath, relativePath);

        if (!Directory.Exists(physicalPath))
        {
            Directory.CreateDirectory(physicalPath);
        }

        return new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(physicalPath),
            RequestPath = requestPath,
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.CacheControl = $"public,max-age={maxAge}";
            }
        };
    }

    /// <summary>
    /// 初始化数据库（迁移 + 种子数据）
    /// </summary>
    public static IApplicationBuilder InitializeDatabase(this IApplicationBuilder app)
    {
        try
        {
            using var scope = app.ApplicationServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BlogContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // 应用数据库迁移
            dbContext.Database.Migrate();
            logger.LogInformation("✅ 数据库迁移完成");

            // 插入种子数据（仅在数据库为空时执行）
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            seeder.SeedData();

            // 检查 FileWatcher 是否启用
            var fileWatcherEnabled = configuration.GetValue<bool>("FileWatcher:Enabled", true);
            if (!fileWatcherEnabled)
            {
                logger.LogWarning("⚠️  FileWatcher 已禁用");
                logger.LogWarning("⚠️  种子文章的数据库记录已创建，但 HTML 版本需要手动生成");
                logger.LogWarning("⚠️  请通过 GraphQL API 或 MCP 工具为文章生成 HTML 版本");
            }
            else
            {
                logger.LogInformation("ℹ️  FileWatcher 已启用，种子文章将在后台自动生成 HTML（约 5-10 秒）");
            }
        }
        catch (Exception ex)
        {
            var logger = app.ApplicationServices
            .CreateScope()
            .ServiceProvider
            .GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "❌ 数据库迁移失败");
            throw;
        }

        return app;
    }

    /// <summary>
    /// 配置 SEO 端点（sitemap.xml、robots.txt）
    /// </summary>
    public static WebApplication UseSeoEndpoints(this WebApplication app)
    {
        // Sitemap 端点
        app.MapGet("/sitemap.xml", async (SitemapService sitemapService) =>
        {
            var xml = await sitemapService.GenerateSitemapXmlAsync();
            return Results.Content(xml, "application/xml", System.Text.Encoding.UTF8);
        });

        // Robots.txt 端点
        app.MapGet("/robots.txt", (SitemapService sitemapService) =>
        {
            var txt = sitemapService.GenerateRobotsTxt();
            return Results.Content(txt, "text/plain", System.Text.Encoding.UTF8);
        });

        return app;
    }
}
