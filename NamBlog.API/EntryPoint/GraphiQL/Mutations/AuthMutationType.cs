using System;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using NamBlog.API.Application.Common;
using NamBlog.API.Application.DTOs;
using NamBlog.API.Application.Services;

namespace NamBlog.API.EntryPoint.GraphiQL.Mutations
{
    /// <summary>
    /// 认证相关的 GraphQL Mutation
    /// 按照 GraphQL.NET 最佳实践：GraphType 不注入 Scoped 服务，在 Resolve 中获取
    /// </summary>
    public class AuthMutationType : ObjectGraphType<object>
    {
        public AuthMutationType()
        {
            Name = "AuthMutation";
            Description = "认证相关操作";

            // 登录
            Field<LoginResultType>("login")
                .Description("管理员登录，返回登录结果")
                .Argument<NonNullGraphType<StringGraphType>>("username", "用户名")
                .Argument<NonNullGraphType<StringGraphType>>("password", "密码")
                .Resolve(ctx =>
                {
                    var authService = ctx.RequestServices?.GetRequiredService<AuthService>();
                    if (authService == null)
                    {
                        return new LoginResult
                        {
                            Success = false,
                            Message = "系统异常，请稍后重试",
                            ErrorCode = "SYSTEM_ERROR"
                        };
                    }

                    try
                    {
                        var username = ctx.GetArgument<string>("username");
                        var password = ctx.GetArgument<string>("password");

                        // 业务验证：输入为空
                        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                        {
                            return new LoginResult
                            {
                                Success = false,
                                Message = "用户名和密码不能为空",
                                ErrorCode = "INVALID_INPUT"
                            };
                        }

                        // 调用认证服务
                        var token = authService.Login(username, password);

                        // 业务失败：认证失败
                        if (token == null)
                        {
                            return new LoginResult
                            {
                                Success = false,
                                Message = "用户名或密码错误",
                                ErrorCode = "INVALID_CREDENTIALS"
                            };
                        }

                        // 业务成功
                        return new LoginResult
                        {
                            Success = true,
                            Token = token
                        };
                    }
                    catch (Exception ex)
                    {
                        // 只捕获技术异常（如数据库连接失败、配置错误等）
                        GraphQLHelper.AddError(ctx, $"登录服务异常: {ex.Message}", ErrorCodes.InternalError);
                        return new LoginResult
                        {
                            Success = false,
                            Message = "系统异常，请稍后重试",
                            ErrorCode = "SYSTEM_ERROR"
                        };
                    }
                });
        }
    }

    /// <summary>
    /// 登录结果 GraphQL 类型
    /// </summary>
    public class LoginResultType : ObjectGraphType<LoginResult>
    {
        public LoginResultType()
        {
            Name = "LoginResult";
            Description = "登录结果";

            Field(x => x.Success).Description("是否成功");
            Field(x => x.Token, nullable: true).Description("JWT Token（成功时返回）");
            Field(x => x.Message, nullable: true).Description("错误消息（失败时返回）");
            Field(x => x.ErrorCode, nullable: true).Description("错误代码");
        }
    }
}
