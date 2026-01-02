using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NamBlog.API.Application.Authorization;
using NamBlog.API.EntryPoint.GraphiQL;
using NamBlog.API.EntryPoint.MCP;

namespace NamBlog.API.Extensions;

/// <summary>
/// 表示层服务注册扩展（Presentation Layer）
/// 包含：GraphQL 端点、MCP 端点
/// </summary>
public static class PresentationExtensions
{
    /// <summary>
    /// 注册所有 GraphQL 服务
    /// 按照 GraphQL.NET 官方最佳实践：
    /// - Schema 注册为 Singleton
    /// - GraphType 通过 AddGraphTypes 自动扫描注册
    /// - 不需要手动注册每个 GraphType
    /// </summary>
    public static IServiceCollection AddGraphQLServices(this IServiceCollection services)
    {
        // 获取环境信息（用于区分开发/生产环境配置）
        var serviceProvider = services.BuildServiceProvider();
        var env = serviceProvider.GetRequiredService<IWebHostEnvironment>();

        // GraphQL 核心服务
        services.AddGraphQL(builder => builder
            .AddSystemTextJson()
            .AddSchema<BlogGraphQLSchema>()
            .AddGraphTypes(typeof(BlogGraphQLSchema).Assembly) // 自动扫描程序集里所有的 GraphType (包括 Query, Mutation, Input 等)
            .AddDataLoader() // 启用 DataLoader 解决 N+1 查询性能问题
            .AddAuthorizationRule() // 添加授权规则支持
            .ConfigureExecutionOptions(options =>
            {
                // 生产环境应保持 false，将错误封装在 JSON 返回
                options.ThrowOnUnhandledException = env.IsDevelopment();

                // 🔒 生产环境禁用 Introspection（安全最佳实践）
                // Introspection 会暴露完整的 Schema 结构，应仅在开发环境启用
                options.EnableMetrics = env.IsDevelopment();
            })
            .AddComplexityAnalyzer(config =>
            {
                // 1. 限制查询深度（建议 10-15）
                config.MaxDepth = 15;

                // 2. 限制总复杂度（根据环境区分）
                //
                // 开发环境：
                // - MaxComplexity = 600000
                // - 支持 Introspection 查询（复杂度 ~571087）
                // - 启用 GraphiQL、Altair、Voyager 工具
                //
                // 生产环境：
                // - MaxComplexity = 500
                // - 禁用 Introspection（通过 EnableMetrics 控制）
                // - 禁用所有 UI 工具
                // - 仅允许正常业务查询（复杂度通常 < 300）
                config.MaxComplexity = env.IsDevelopment() ? 600000 : 500;
            })
            .AddUserContextBuilder(BuildUserContext));

        // ✅ 移除所有手动注册
        // AddGraphTypes 会自动扫描并注册所有 IGraphType 实现
        // GraphType 会被注册为 Transient，但在 Singleton Schema 下实际表现为 Singleton

        return services;
    }

    /// <summary>
    /// 构建 GraphQL UserContext
    /// </summary>
    private static Dictionary<string, object?> BuildUserContext(HttpContext httpContext)
    {
        var logger = httpContext.RequestServices?.GetService<ILogger<Program>>();
        var user = httpContext.User;
        var userName = user?.Identity?.Name;
        var isAuthenticated = user?.Identity?.IsAuthenticated ?? false;
        var isAdmin = user?.IsInRole("Admin") ?? false;
        // 获取所有角色字符串，用于日志
        var roles = string.Join(", ", user?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? []);

        logger?.LogDebug(
            "GraphQL Request - User: {User}, Auth: {IsAuthenticated}, Admin: {IsAdmin}, Roles: [{Roles}]",
            userName ?? "Anonymous", isAuthenticated, isAdmin, roles);

        return new Dictionary<string, object?>
        {
            ["User"] = user as ClaimsPrincipal,
            ["IsAdmin"] = isAdmin
        };
    }

    /// <summary>
    /// 注册 MCP (Model Context Protocol) 服务
    /// </summary>
    public static IServiceCollection AddMCPServices(this IServiceCollection services)
    {
        // 注册 MCP Token 授权处理器
        services.AddSingleton<IAuthorizationHandler, McpTokenHandler>();

        // MCP 工具注册
        services.AddScoped<MarkdownTools>();
        services.AddScoped<BlogManagementTools>();

        // MCP 资源注册
        services.AddScoped<BlogResources>();

        // MCP 提示词注册
        services.AddScoped<BlogPrompts>();

        // MCP Server（使用官方 SDK），测试：MCP Inspector
        services.AddMcpServer()
            // 支持streamableHttp（/mcp）和sse（mcp/sse，不支持https自签名证书？）
            .WithHttpTransport()
            .WithToolsFromAssembly()      // 自动发现并注册带 McpServerToolType 属性的类其中 McpServerTool 标记的工具
            .WithResourcesFromAssembly()  // 自动发现并注册带 McpServerResourceType 属性的类其中 McpServerResource 标记的资源
            .WithPromptsFromAssembly();   // 自动发现并注册带 McpServerPromptType 属性的类其中 McpServerPrompt 标记的提示词

        return services;
    }
}
