using System;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NamBlog.API.Domain.Interfaces;
using NamBlog.API.Infrastructure.Agents;
using NamBlog.API.Infrastructure.Persistence;
using NamBlog.API.Infrastructure.Persistence.Repositories;
using NamBlog.API.Infrastructure.Services;
using OpenAI;

namespace NamBlog.API.Extensions
{
    /// <summary>
    /// 基础设施层服务注册扩展（Infrastructure Layer）
    /// 包含：仓储、文件服务、AI 服务、HTTP 客户端、后台任务
    /// </summary>
    public static class InfrastructureExtensions
    {
        /// <summary>
        /// 注册所有基础设施层服务（数据访问、外部服务、后台任务）
        /// </summary>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // ====== 数据访问层（仓储、工作单元） ======
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ====== 领域服务接口实现 ======
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IAIService, OpenAIService>();

            // ====== AI 服务（Microsoft.Extensions.AI） ======
            services.AddChatClient(sp =>
            {
                // 使用 IOptionsMonitor 支持热重载
                var aiSettings = sp.GetRequiredService<IOptionsMonitor<AISettings>>().CurrentValue;
                var logger = sp.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("初始化 IChatClient - BaseUrl: {BaseUrl}, Model: {Model}",
                    aiSettings.BaseUrl, aiSettings.Model);

                // 配置超时时间为 10 分钟（生成长文章需要更多时间）
                var clientOptions = new OpenAIClientOptions 
                { 
                    Endpoint = new Uri(aiSettings.BaseUrl),
                    NetworkTimeout = TimeSpan.FromMinutes(10)
                };

                return new OpenAI.Chat.ChatClient(
                    model: aiSettings.Model,
                    credential: new System.ClientModel.ApiKeyCredential(aiSettings.ApiKey),
                    options: clientOptions)
                .AsIChatClient();
            });
            //.UseDistributedCache()    // 不用缓存，会导致创建多个一样的html
            //.UseLogging();            // 记录每次 AI 交互的详细日志

            // ====== 后台任务（文件监控等） ======
            var enableFileWatcher = configuration.GetValue<bool>("FileWatcher:Enabled", true);
            if (enableFileWatcher)
            {
                services.AddHostedService<FileWatcherService>();
            }

            return services;
        }
    }
}
