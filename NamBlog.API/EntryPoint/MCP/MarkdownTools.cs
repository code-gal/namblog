using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using NamBlog.API.Application.DTOs;
using NamBlog.API.Application.Services;

namespace NamBlog.API.EntryPoint.MCP
{
    /// <summary>
    /// MCP 工具集合 - Markdown 转换
    /// 提供 Markdown 到 HTML 的实时转换功能，不涉及文章业务逻辑
    /// 作为独立工具，用于预览或测试 Markdown 渲染效果
    /// 支持 MCP 进度通知机制（客户端需提供 progressToken）
    /// </summary>
    [McpServerToolType]
    public class MarkdownTools(MarkdownService markdownService)
    {
        private readonly MarkdownService _markdownService = markdownService;

        /// <summary>
        /// 将 Markdown 转换为 HTML
        /// 支持 MCP 进度通知（需客户端提供 progressToken）
        /// 独立的转换工具，不依赖文章业务逻辑，不保存任何数据
        /// </summary>
        [McpServerTool(Name = "convert_markdown_to_html")]
        [Description("将 Markdown 文本转换为格式化的 HTML。独立工具，不涉及文章保存。适用于预览 Markdown 渲染效果或测试转换结果。如果客户端提供了 progressToken，将通过 MCP 进度通知发送生成进度。参数：markdown（必填，要转换的 Markdown 文本）、customPrompt（可选，自定义 AI 提示词）。返回 HtmlConversionResult 对象，包含：Status（状态）、Html（完整 HTML）、Error（错误信息）。")]
        public async Task<HtmlConversionResult> ConvertMarkdownToHtml(
            McpServer server,
            RequestContext<CallToolRequestParams> context,
            [Description("要转换的 Markdown 文本，必填。支持标准 Markdown 语法及扩展语法（表格、代码块、数学公式等）。")] string markdown,
            [Description("可选的自定义提示词，用于控制 HTML 生成风格（如简洁风格、学术风格等）。不传则使用系统默认提示词。")] string? customPrompt = null,
            CancellationToken cancellationToken = default)
        {
            // 参数验证
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return new HtmlConversionResult
                {
                    Status = HtmlConversionStatus.Failed,
                    Html = string.Empty,
                    Error = "Markdown 内容不能为空"
                };
            }

            // 获取进度令牌（客户端可选提供）
            var progressToken = context.Params?.ProgressToken;

            // 收集所有流式输出，拼接成完整 HTML
            var htmlBuilder = new StringBuilder();
            HtmlConversionStatus finalStatus = HtmlConversionStatus.Generating;
            string? errorMessage = null;
            int lastProgress = 0;

            try
            {
                await foreach (var update in _markdownService.ConvertToHtmlStreamAsync(markdown, customPrompt, cancellationToken))
                {
                    finalStatus = update.Status;

                    // 处理错误状态
                    if (update.Status == HtmlConversionStatus.Failed)
                    {
                        errorMessage = update.Error ?? "HTML 生成失败";
                        break;
                    }

                    // 拼接 HTML 片段
                    if (!string.IsNullOrEmpty(update.Chunk))
                    {
                        htmlBuilder.Append(update.Chunk);
                    }

                    // 发送 MCP 进度通知（如果客户端提供了 progressToken）
                    if (progressToken is not null && update.Progress > lastProgress)
                    {
                        lastProgress = update.Progress;
                        await server.SendNotificationAsync(
                            NotificationMethods.ProgressNotification,
                            new ProgressNotificationParams
                            {
                                ProgressToken = progressToken.Value,
                                Progress = new ProgressNotificationValue
                                {
                                    Progress = update.Progress,
                                    Total = 100,
                                    Message = update.Status == HtmlConversionStatus.Generating
                                        ? $"正在生成 HTML... {update.Progress}%"
                                        : "HTML 生成完成",
                                },
                            });
                    }

                    // 如果完成，退出
                    if (update.Status == HtmlConversionStatus.Completed)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return new HtmlConversionResult
                {
                    Status = HtmlConversionStatus.Failed,
                    Html = htmlBuilder.ToString(),
                    Error = "操作已取消"
                };
            }
            catch (Exception ex)
            {
                return new HtmlConversionResult
                {
                    Status = HtmlConversionStatus.Failed,
                    Html = string.Empty,
                    Error = $"转换过程中发生异常：{ex.Message}"
                };
            }

            // 返回最终结果
            return new HtmlConversionResult
            {
                Status = finalStatus,
                Html = htmlBuilder.ToString(),
                Error = errorMessage
            };
        }
    }
}
