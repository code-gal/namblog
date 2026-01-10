using System;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NamBlog.API.Application.DTOs;
using NamBlog.API.Application.Resources;
using NamBlog.API.Application.Services;

namespace NamBlog.API.EntryPoint.GraphiQL.Mutations
{
    /// <summary>
    /// Markdown 相关的 GraphQL Mutation
    /// </summary>
    public class AgentMutationType : ObjectGraphType<object>
    {
        public AgentMutationType()
        {
            Name = "AgentMutation";
            Description = "AI Agents操作";

            Field<HtmlConversionResultType>("convertToHtml")
                .Description("将 Markdown 转换为 HTML（使用 AI 生成，用于预览效果）")
                .Argument<NonNullGraphType<StringGraphType>>("markdown", "要转换的 Markdown 文本")
                .Argument<StringGraphType>("customPrompt", "可选的自定义提示词，可调整样式")
                .ResolveAsync(async context =>
                {
                    var markdownService = context.RequestServices?.GetRequiredService<MarkdownService>();
                    var localizer = context.RequestServices?.GetRequiredService<IStringLocalizer<SharedResource>>();
                    var logger = context.RequestServices?.GetService<ILogger<AgentMutationType>>();
                    if (markdownService == null || localizer == null)
                        return null;

                    try
                    {
                        var markdown = context.GetArgument<string>("markdown");
                        var customPrompt = context.GetArgument<string?>("customPrompt");
                        var result = await markdownService.ConvertToHtmlAsync(markdown, customPrompt);

                        if (!result.IsSuccess)
                        {
                            GraphQLHelper.AddErrorForUser(context, result, result.ErrorMessage ?? "", localizer["MarkdownConversionFailed"].Value);
                            return null;
                        }

                        return result.Value;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Markdown conversion failed");
                        context.Errors.Add(new GraphQL.ExecutionError(localizer["MarkdownConversionFailed"].Value, ex));
                        return null;
                    }
                });
        }
    }

    /// <summary>
    /// HtmlConversionResult 的 GraphQL Type
    /// </summary>
    public class HtmlConversionResultType : ObjectGraphType<HtmlConversionResult>
    {
        public HtmlConversionResultType()
        {
            Name = "HtmlConversionResult";
            Description = "HTML 转换结果";

            Field<NonNullGraphType<EnumerationGraphType<HtmlConversionStatus>>>("status")
                .Description("转换状态")
                .Resolve(context => context.Source.Status);

            Field(x => x.Html).Description("生成的 HTML 内容");
            Field(x => x.Error, nullable: true).Description("错误信息（仅在失败时有值）");
        }
    }
}
