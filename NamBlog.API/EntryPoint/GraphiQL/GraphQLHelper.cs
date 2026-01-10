using System;
using System.Collections.Generic;
using GraphQL;
using NamBlog.API.Application.Common;

namespace NamBlog.API.EntryPoint.GraphiQL
{
    /// <summary>
    /// GraphQL 辅助工具类
    /// </summary>
    public static class GraphQLHelper
    {
        /// <summary>
        /// 检查当前用户是否为管理员
        /// </summary>
        /// <remarks>
        /// 保留此方法用于业务逻辑判断（如判断是否返回草稿内容）。
        /// 对于授权检查，请使用 .AuthorizeWithPolicy("AdminPolicy") 声明式授权。
        /// </remarks>
        public static bool IsAdmin(IResolveFieldContext context)
        {
            if (context.UserContext is Dictionary<string, object?> userContext && userContext.TryGetValue("IsAdmin", out var isAdminObj))
            {
                return isAdminObj as bool? == true;
            }

            return false;
        }

        /// <summary>
        /// 添加错误到上下文（带错误码和时间戳）
        /// </summary>
        public static void AddError(IResolveFieldContext context, string message, string? errorCode = null)
        {
            context.Errors.Add(new ExecutionError(message)
            {
                Code = errorCode ?? ErrorCodes.InternalError,
                Extensions = new Dictionary<string, object?>
                {
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            });
        }

        /// <summary>
        /// 从Result中添加错误到上下文（带错误码和时间戳）
        /// </summary>
        public static void AddErrorFromResult(IResolveFieldContext context, Result result, string defaultMessage = "Operation failed")
        {
            context.Errors.Add(new ExecutionError(result.ErrorMessage ?? defaultMessage)
            {
                Code = result.ErrorCode ?? ErrorCodes.InternalError,
                Extensions = new Dictionary<string, object?>
                {
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            });
        }

        /// <summary>
        /// 根据用户角色添加错误（管理员看详细错误，公众看通用错误）
        /// </summary>
        /// <param name="context">GraphQL解析上下文</param>
        /// <param name="result">操作结果</param>
        /// <param name="defaultMessage">默认错误消息（管理员使用）</param>
        /// <param name="publicMessage">公众用户错误消息（默认为"Operation failed"）</param>
        public static void AddErrorForUser(IResolveFieldContext context, Result result, string defaultMessage = "Operation failed", string publicMessage = "Operation failed")
        {
            if (IsAdmin(context))
            {
                // 管理员：返回详细错误信息
                AddErrorFromResult(context, result, defaultMessage);
            }
            else
            {
                // 公众用户：返回通用错误信息
                context.Errors.Add(new ExecutionError(publicMessage)
                {
                    Code = ErrorCodes.InternalError,
                    Extensions = new Dictionary<string, object?>
                    {
                        ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    }
                });
            }
        }
    }
}
