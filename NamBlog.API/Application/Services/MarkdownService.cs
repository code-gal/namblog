using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Mapster;
using NamBlog.API.Application.Common;
using NamBlog.API.Application.DTOs;
using NamBlog.API.Domain.Interfaces;

namespace NamBlog.API.Application.Services
{
    /// <summary>
    /// Markdown 转换服务（Application 层封装）
    /// </summary>
    public class MarkdownService(IAIService aiService)
    {
        private readonly IAIService _aiService = aiService;

        /// <summary>
        /// 将 Markdown 转换为 HTML（非流式，适用于 GraphQL）
        /// </summary>
        public async Task<Result<HtmlConversionResult>> ConvertToHtmlAsync(
            string markdown,
            string? customPrompt = null)
        {
            // 参数验证
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return Result.Failure<HtmlConversionResult>(
                    "Markdown 内容不能为空",
                    "INVALID_MARKDOWN");
            }

            // 调用 Domain 层的非流式接口
            var htmlResult = await _aiService.RenderMarkdownToHtmlAsync(markdown, customPrompt);

            if (!htmlResult.IsSuccess)
            {
                return Result.Failure<HtmlConversionResult>(
                    htmlResult.ErrorMessage ?? "HTML 生成失败",
                    htmlResult.ErrorCode ?? "HTML_GENERATION_FAILED");
            }

            // 返回成功结果
            return Result.Success(new HtmlConversionResult
            {
                Status = HtmlConversionStatus.Completed,
                Html = htmlResult.Value ?? string.Empty,
                Error = null
            });
        }

        /// <summary>
        /// 将 Markdown 转换为 HTML（流式输出，用于 MCP）
        /// </summary>
        public async IAsyncEnumerable<HtmlConversionProgress> ConvertToHtmlStreamAsync(
            string markdown,
            string? customPrompt = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // 调用 Domain 层的 AI 服务，使用 Mapster 将 ValueObject 转换为 DTO
            await foreach (var update in _aiService.RenderMarkdownToHtmlStreamAsync(markdown, customPrompt, cancellationToken))
            {
                yield return update.Adapt<HtmlConversionProgress>();
            }
        }
    }
}
