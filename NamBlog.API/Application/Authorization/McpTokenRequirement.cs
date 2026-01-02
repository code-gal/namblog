using Microsoft.AspNetCore.Authorization;

namespace NamBlog.API.Application.Authorization;

/// <summary>
/// MCP Token 授权要求
/// 用于验证请求是否包含有效的 MCP 固定 Token
/// </summary>
public class McpTokenRequirement : IAuthorizationRequirement
{
    // 标记接口，无需额外属性
}
