using Microsoft.Extensions.DependencyInjection;
using NamBlog.API.Application.DTOs;
using NamBlog.API.Application.Services;

namespace NamBlog.API.Extensions;

/// <summary>
/// 应用层服务注册扩展（Application Layer）
/// 包含业务逻辑服务：ArticleService、AuthService、ValidationService 等
/// </summary>
public static class ApplicationExtensions
{
    /// <summary>
    /// 注册所有应用层服务
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 配置 Mapster 映射
        ArticleMappingConfig.Configure();

        // 应用层业务服务（Scoped 生命周期）
        services.AddScoped<ValidationService>();
        services.AddScoped<MetadataProcessor>();
        services.AddScoped<ArticleCommandService>();
        services.AddScoped<ArticleQueryService>();
        services.AddScoped<AuthService>();
        services.AddScoped<MarkdownService>();

        return services;
    }
}
