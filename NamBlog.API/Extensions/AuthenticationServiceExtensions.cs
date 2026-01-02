using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NamBlog.API.Application.Authorization;

namespace NamBlog.API.Extensions;

/// <summary>
/// 认证授权服务注册扩展
/// </summary>
public static class AuthenticationServiceExtensions
{
    /// <summary>
    /// 注册 JWT 认证和授权服务
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT Secret 未配置");
        var jwtIssuer = configuration["Jwt:Issuer"];
        var jwtAudience = configuration["Jwt:Audience"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
                };
            });

        //全局Admin授权策略
        services.AddAuthorizationBuilder()
            .AddPolicy("AdminPolicy", policy =>
                policy.RequireRole("Admin"))
            .AddPolicy("McpPolicy", policy =>
                policy.AddRequirements(new McpTokenRequirement()));

        return services;
    }

    /// <summary>
    /// 注册 CORS 策略
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        //var allowedOrigins = configuration["Cors:AllowedOrigins"]?.Split([',','，',';','；'])?? ["*"];

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", builder =>
            {
                //builder.WithOrigins(allowedOrigins)
                //       .AllowAnyMethod()
                //       .AllowAnyHeader()
                //       .AllowCredentials();
                //       // .SetIsOriginAllowed(_ => true); 生产环境避免跨域
                if (services.BuildServiceProvider()
                    .GetRequiredService<IWebHostEnvironment>()
                    .IsDevelopment())
                {
                    // ✅ 开发环境：允许所有本地来源（便于开发）
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                }
                else
                {
                    // ✅ 生产环境：严格白名单
                    var allowedOrigins = (configuration["Cors:AllowedOrigins"] ?? "")
                        .Split([',', '，', ';', '；'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToArray();

                    if (allowedOrigins.Length == 0)
                    {
                        throw new InvalidOperationException(
                            "❌ 生产环境必须配置 Cors:AllowedOrigins");
                    }

                    builder.WithOrigins(allowedOrigins)
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
                }
            });
        });

        return services;
    }
}
