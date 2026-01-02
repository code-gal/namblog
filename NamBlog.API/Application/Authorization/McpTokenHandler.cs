using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NamBlog.API.Application.Authorization;

/// <summary>
/// MCP Token 授权处理器
/// 验证 MCP 请求中的 Bearer Token 是否匹配配置中的固定 Token
/// </summary>
public class McpTokenHandler(IConfiguration configuration, ILogger<McpTokenHandler> logger) : AuthorizationHandler<McpTokenRequirement>
{
    private readonly string _validToken = configuration["MCP:AuthToken"]
            ?? throw new InvalidOperationException("MCP:AuthToken 未在 appsettings.json 中配置");

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        McpTokenRequirement requirement)
    {
        // 获取 HttpContext
        if (context.Resource is not HttpContext httpContext)
        {
            logger.LogWarning("MCP 授权失败：无法获取 HttpContext");
            return Task.CompletedTask;
        }

        // 获取 Authorization 头
        var authHeader = httpContext.Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader))
        {
            logger.LogWarning("MCP 请求缺少 Authorization 头：{Path}", httpContext.Request.Path);
            return Task.CompletedTask;
        }

        // 解析 Bearer Token
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("MCP 请求使用了无效的认证方案：{AuthHeader}", authHeader);
            return Task.CompletedTask;
        }

        var token = authHeader["Bearer ".Length..].Trim();

        // 验证 Token
        if (token == _validToken)
        {
            logger.LogDebug("MCP Token 验证成功");
            context.Succeed(requirement);
        }
        else
        {
            logger.LogWarning("MCP 请求使用了无效的 Token");
        }

        return Task.CompletedTask;
    }
}
