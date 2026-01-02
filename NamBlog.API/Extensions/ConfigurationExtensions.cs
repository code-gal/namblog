using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NamBlog.API.Application.DTOs;
using NamBlog.API.Infrastructure.Agents;
using NamBlog.API.Infrastructure.Services;

namespace NamBlog.API.Extensions;

/// <summary>
/// 配置选项注册扩展（Configuration Options）
/// 使用 IOptions/IOptionsSnapshot/IOptionsMonitor 模式绑定 appsettings.json 配置
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// 注册所有配置选项（Options Pattern）
    /// </summary>
    public static IServiceCollection AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // ====== 博客基础配置 ======
        // 从 Blog 节点读取（对应 appsettings.json 中的 "Blog" 节点）
        services.Configure<BlogInfo>(configuration.GetSection("Blog"));

        // ====== AI 配置 ======
        services.Configure<AISettings>(configuration.GetSection("AI"));
        services.Configure<PromptsConfig>(configuration.GetSection("Prompts"));

        // ====== 存储配置 ======
        services.Configure<StorageSettings>(configuration.GetSection("Storage"));

        // ====== 后台服务配置 ======
        services.Configure<FileWatcherSettings>(configuration.GetSection("FileWatcher"));

        return services;
    }
}
