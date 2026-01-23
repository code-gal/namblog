using System.Collections.Generic;
using System.IO;
using GraphQL;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NamBlog.API.EntryPoint.GraphiQL;
using NamBlog.API.EntryPoint.MCP;
using NamBlog.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 加载用户自定义配置（优先级高于 appsettings.json）
var dataRootPath = builder.Configuration["Storage:DataRootPath"] ?? "./data";
var userConfigPath = Path.Combine(dataRootPath, "config", "config.json");
if (File.Exists(userConfigPath))
{
    builder.Configuration.AddJsonFile(userConfigPath, optional: true, reloadOnChange: true);
}

// 加载 AI Prompts 配置（支持热重载）
var promptsConfigPath = Path.Combine(dataRootPath, "config", "prompts.json");
if (File.Exists(promptsConfigPath))
{
    builder.Configuration.AddJsonFile(promptsConfigPath, optional: true, reloadOnChange: true);
}

//让 configuration 变量拥有更多配置相关的功能。
var configuration = builder.Configuration as ConfigurationManager;
var services = builder.Services;
ConfigureServices(services, configuration);
var app = builder.Build();
ConfigureMiddleware(app, configuration);
app.Run();

/// <summary>
/// 配置所有服务（按 DDD 分层顺序注册）
/// </summary>
static void ConfigureServices(IServiceCollection services, ConfigurationManager configuration)
{
    // ====== 0. 系统基础（与业务无关） ======
    services.AddMemoryCache();                      // 内存缓存（用于 SEO 路径缓存、登录限流等）
    services.AddHttpContextAccessor();              // HTTP 上下文访问器（用于获取客户端 IP）
    // services.AddDistributedMemoryCache();        // 共享缓存池，用于AI服务
    services.AddConfigurationOptions(configuration); // 配置绑定（Options Pattern）

    // Localization（国际化）
    services.AddLocalization(); // 资源文件与类文件在同一目录，无需设置 ResourcesPath
    services.Configure<RequestLocalizationOptions>(options =>
    {
        options.SetDefaultCulture("en-US")
            .AddSupportedCultures(_supportedCultures)
            .AddSupportedUICultures(_supportedCultures);
    });

    // ====== 1. 领域层（Domain Layer） ======
    // 领域服务通常不需要注册（纯逻辑类）
    // 如果有需要注入的领域服务，在此注册

    // ====== 2. 基础设施层（Infrastructure Layer） ======
    services.AddDatabaseServices(configuration);    // 数据库上下文（EF Core）
    services.AddInfrastructureServices(configuration); // 仓储、文件服务、AI 服务、后台任务

    // ====== 3. 应用层（Application Layer） ======
    services.AddApplicationServices();              // 业务服务（ArticleService, AuthService 等）

    // ====== 4. 表示层（Presentation Layer） ======
    services.AddGraphQLServices();                  // GraphQL 端点
    services.AddMCPServices();                      // MCP 端点

    // ====== 5. 横切关注点（Cross-Cutting Concerns） ======
    services.AddJwtAuthentication(configuration);   // JWT 认证
    services.AddCorsPolicy(configuration);          // CORS 策略
}

/// <summary>
/// 配置中间件管道
/// </summary>
static void ConfigureMiddleware(WebApplication app, ConfigurationManager configuration)
{
    var dataRootPath = configuration["Storage:DataRootPath"] ?? "./data";

    // 开发环境配置
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    // 初始化数据库
    app.InitializeDatabase();

    // SEO 优化（必须在静态文件之前，先重写路径）
    app.UseSeoOptimization();

    // 静态文件服务
    app.UseStaticFileServices(app.Environment, dataRootPath);

    // 请求本地化（根据 Accept-Language 头自动设置 Culture）
    app.UseRequestLocalization();

    // 路由和认证
    app.UseRouting(); //启用路由识别

    // 处理跨域请求（仅开发环境需要，生产环境同源部署不需要）
    if (app.Environment.IsDevelopment())
    {
        app.UseCors("AllowFrontend");
    }

    app.UseAuthentication(); //验证用户身份
    app.UseAuthorization(); //检查用户权限

    // GraphQL 端点
    app.UseGraphQL<BlogGraphQLSchema>("/graphql");

    // ⚠️ GraphQL UI 工具仅在开发环境启用（生产环境禁用以提高安全性）
    if (app.Environment.IsDevelopment())
    {
        app.UseGraphQLGraphiQL("/ui/graphiql", new GraphQL.Server.Ui.GraphiQL.GraphiQLOptions
        {
            GraphQLEndPoint = "/graphql"
        });

        app.UseGraphQLAltair("/ui/altair", new GraphQL.Server.Ui.Altair.AltairOptions
        {
            GraphQLEndPoint = "/graphql",
            Settings = new Dictionary<string, object?>
            {
                { "language", "zh-CN" },
                { "theme", "system" }
            }
        });

        app.UseGraphQLVoyager("/ui/voyager", new GraphQL.Server.Ui.Voyager.VoyagerOptions
        {
            GraphQLEndPoint = "/graphql"
        });
    }
    // MCP 端点（使用兼容性中间件 + 授权策略）
    app.UseWhen(
        context => context.Request.Path.StartsWithSegments("/mcp"),
        appBuilder =>
        {
            appBuilder.UseMiddleware<McpCompatibilityMiddleware>();
        });

    app.MapMcp("/mcp").RequireAuthorization("McpPolicy");

    // 健康检查
    app.MapGet("/health", () => $"OK - {configuration["Blog:BlogName"]} is running");

    // SEO 端点（sitemap.xml、robots.txt）
    app.UseSeoEndpoints();

    // SPA 回退路由（必须放在最后）
    // 当请求不匹配任何 API 或静态文件时，返回 index.html 让前端路由处理
    app.MapFallbackToFile("index.html");
}

/// <summary>
/// Program 类（用于定义常量和共享字段）
/// </summary>
internal partial class Program
{
    /// <summary>
    /// 支持的语言文化列表（用于国际化配置）
    /// </summary>
    private static readonly string[] _supportedCultures = ["zh-CN", "en-US"];
}
