using System;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NamBlog.API.Application.DTOs;
using NamBlog.API.Application.Resources;
using NamBlog.API.Application.Services;
using NamBlog.API.EntryPoint.GraphiQL.Queries;

namespace NamBlog.API.EntryPoint.GraphiQL.Mutations
{
    /// <summary>
    /// 文章相关的 GraphQL Mutation
    /// 直接使用业务层 DTO 和 Command，无需额外的 InputType
    /// </summary>
    public class ArticleMutationType : ObjectGraphType<object>
    {
        public ArticleMutationType()
        {
            Name = "ArticleMutation";
            Description = "文章变更操作";

            Field<ArticleMetadataDtoType>("saveArticle")
                .Description("保存文章（创建时生成HTML版本，更新时不生成版本，需要管理员权限）")
                .AuthorizeWithPolicy("AdminPolicy")
                .Argument<NonNullGraphType<SaveArticleInputType>>("input", "保存文章参数")
                .ResolveAsync(async context =>
                {
                    var articleService = context.RequestServices?.GetRequiredService<ArticleCommandService>();
                    var localizer = context.RequestServices?.GetRequiredService<IStringLocalizer<SharedResource>>();
                    var logger = context.RequestServices?.GetService<ILogger<ArticleMutationType>>();
                    if (articleService == null || localizer == null)
                        return null;

                    try
                    {
                        var command = context.GetArgument<SaveArticleCommand>("input");
                        var result = await articleService.SaveArticleAsync(command);

                        if (!result.IsSuccess)
                        {
                            GraphQLHelper.AddErrorForUser(context, result, result.ErrorMessage ?? "", localizer["SaveArticleFailed"].Value);
                            return null;
                        }

                        return result.Value;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Save article failed");
                        context.Errors.Add(new GraphQL.ExecutionError(localizer["SaveArticleFailed"].Value, ex));
                        return null;
                    }
                });

            Field<ArticleVersionSubmitDtoType>("submitArticle")
                .Description("提交文章（生成HTML并创建版本，支持用户提供HTML或AI生成，需要管理员权限）")
                .AuthorizeWithPolicy("AdminPolicy")
                .Argument<NonNullGraphType<SubmitArticleInputType>>("input", "提交文章参数")
                .ResolveAsync(async context =>
                {
                    var articleService = context.RequestServices?.GetRequiredService<ArticleCommandService>();
                    var localizer = context.RequestServices?.GetRequiredService<IStringLocalizer<SharedResource>>();
                    var logger = context.RequestServices?.GetService<ILogger<ArticleMutationType>>();
                    if (articleService == null || localizer == null)
                        return null;

                    try
                    {
                        var command = context.GetArgument<SubmitArticleCommand>("input");
                        var result = await articleService.SubmitArticleAsync(command);

                        if (!result.IsSuccess)
                        {
                            GraphQLHelper.AddErrorForUser(context, result, result.ErrorMessage ?? "", localizer["SubmitArticleFailed"].Value);
                            return null;
                        }

                        return result.Value;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Submit article failed");
                        context.Errors.Add(new GraphQL.ExecutionError(localizer["SubmitArticleFailed"].Value, ex));
                        return null;
                    }
                });

            // 删除版本（如果是最后一个版本则删除整篇文章）
            Field<BooleanGraphType>("deleteVersion")
                .Description("删除指定版本（如果是最后一个版本则删除整篇文章，需要管理员权限）")
                .AuthorizeWithPolicy("AdminPolicy")
                .Argument<NonNullGraphType<IntGraphType>>("id", "文章ID")
                .Argument<NonNullGraphType<StringGraphType>>("versionName", "版本名")
                .ResolveAsync(async context =>
                {
                    var articleService = context.RequestServices?.GetRequiredService<ArticleCommandService>();
                    var localizer = context.RequestServices?.GetRequiredService<IStringLocalizer<SharedResource>>();
                    var logger = context.RequestServices?.GetService<ILogger<ArticleMutationType>>();
                    if (articleService == null || localizer == null)
                        return false;

                    try
                    {
                        var id = context.GetArgument<int>("id");
                        var versionName = context.GetArgument<string>("versionName");
                        var result = await articleService.DeleteVersionAsync(id, versionName);

                        if (!result.IsSuccess)
                        {
                            GraphQLHelper.AddErrorForUser(context, result, result.ErrorMessage ?? "", localizer["DeleteVersionFailed"].Value);
                            return false;
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Delete version failed");
                        context.Errors.Add(new GraphQL.ExecutionError(localizer["DeleteVersionFailed"].Value, ex));
                        return false;
                    }
                });

            // 切换发布状态
            Field<ArticleDetailDtoType>("togglePublish")
                .Description("切换文章发布状态（需要管理员权限）")
                .AuthorizeWithPolicy("AdminPolicy")
                .Argument<NonNullGraphType<IntGraphType>>("id", "文章ID")
                .ResolveAsync(async context =>
                {
                    var articleService = context.RequestServices?.GetRequiredService<ArticleCommandService>();
                    var localizer = context.RequestServices?.GetRequiredService<IStringLocalizer<SharedResource>>();
                    var logger = context.RequestServices?.GetService<ILogger<ArticleMutationType>>();
                    if (articleService == null || localizer == null)
                        return null;

                    try
                    {
                        var id = context.GetArgument<int>("id");
                        var result = await articleService.TogglePublishAsync(id);

                        if (!result.IsSuccess)
                        {
                            GraphQLHelper.AddErrorForUser(context, result, result.ErrorMessage ?? "", localizer["TogglePublishFailed"].Value);
                            return null;
                        }

                        return result.Value;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Toggle publish failed");
                        context.Errors.Add(new GraphQL.ExecutionError(localizer["TogglePublishFailed"].Value, ex));
                        return null;
                    }
                });

            // 删除文章
            Field<BooleanGraphType>("deleteArticle")
                .Description("删除文章（包括所有版本，需要管理员权限）")
                .AuthorizeWithPolicy("AdminPolicy")
                .Argument<NonNullGraphType<IntGraphType>>("id", "文章ID")
                .ResolveAsync(async context =>
                {
                    var articleService = context.RequestServices?.GetRequiredService<ArticleCommandService>();
                    var localizer = context.RequestServices?.GetRequiredService<IStringLocalizer<SharedResource>>();
                    var logger = context.RequestServices?.GetService<ILogger<ArticleMutationType>>();
                    if (articleService == null || localizer == null)
                        return false;

                    try
                    {
                        var id = context.GetArgument<int>("id");
                        var result = await articleService.DeleteArticleAsync(id);

                        if (!result.IsSuccess)
                        {
                            GraphQLHelper.AddErrorForUser(context, result, result.ErrorMessage ?? "", localizer["DeleteArticleFailed"].Value);
                            return false;
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Delete article failed");
                        context.Errors.Add(new GraphQL.ExecutionError(localizer["DeleteArticleFailed"].Value, ex));
                        return false;
                    }
                });
        }
    }

    /// <summary>
    /// ArticleMetadataDto 的 GraphQL Type（内联定义）
    /// </summary>
    public class ArticleMetadataDtoType : ObjectGraphType<ArticleMetadataDto>
    {
        public ArticleMetadataDtoType()
        {
            Name = "ArticleMetadata";
            Description = "文章元数据（包含管理员编辑页所需的完整数据）";

            // 基础字段
            Field(x => x.PostId).Description("文章ID");
            Field(x => x.Title, nullable: true).Description("标题");
            Field(x => x.Slug, nullable: true).Description("URL标识");
            Field(x => x.Excerpt, nullable: true).Description("摘要");
            Field(x => x.Category).Description("分类");
            Field(x => x.Tags, nullable: true).Description("标签");

            // 管理员编辑页扩展字段
            Field<ListGraphType<CategoryStatisticType>>("allCategories")
                .Description("所有分类列表（含统计）")
                .Resolve(context => context.Source.AllCategories);
            Field<ListGraphType<TagStatisticType>>("allTags")
                .Description("所有标签列表（含统计）")
                .Resolve(context => context.Source.AllTags);
            Field(x => x.AiPrompts).Description("AI提示词历史");
            Field(x => x.VersionNames).Description("版本名称列表");
            Field<MainVersionDetailType, MainVersionDetail?>("mainVersion").Description("主版本详细信息");
            Field(x => x.IsPublished).Description("是否已发布");
            Field(x => x.IsFeatured).Description("是否为精选");

            // 时间字段
            Field(x => x.CreateTime).Description("创建时间");
            Field(x => x.LastModified).Description("最后修改时间");
        }
    }

    /// <summary>
    /// ArticleVersionSubmitDto 的 GraphQL Type（内联定义）
    /// </summary>
    public class ArticleVersionSubmitDtoType : ObjectGraphType<ArticleVersionSubmitDto>
    {
        public ArticleVersionSubmitDtoType()
        {
            Name = "ArticleVersionSubmit";
            Description = "文章版本提交结果";

            Field(x => x.Slug).Description("文章 Slug（用于前端跳转）");
        }
    }

    /// <summary>
    /// MainVersionDetail 的 GraphQL Type（内联定义）
    /// </summary>
    public class MainVersionDetailType : ObjectGraphType<MainVersionDetail>
    {
        public MainVersionDetailType()
        {
            Name = "MainVersionDetail";
            Description = "主版本详细信息";

            Field(x => x.VersionName).Description("版本名称");
            Field(x => x.Html).Description("HTML内容");
            Field(x => x.ValidationStatus).Description("HTML验证状态");
            Field(x => x.HtmlValidationError, nullable: true).Description("HTML验证错误信息");
        }
    }

    /// <summary>
    /// 保存文章输入类型（GraphQL）- Save按钮
    /// 说明：支持创建和更新，id为null表示创建，否则更新
    /// </summary>
    public class SaveArticleInputType : InputObjectGraphType<SaveArticleCommand>
    {
        public SaveArticleInputType()
        {
            Name = "SaveArticleInput";
            Description = "保存文章输入参数（创建时生成HTML版本，更新时不生成版本）";

            Field(x => x.Markdown).Description("Markdown 内容（必填）");
            Field(x => x.Id, nullable: true).Description("文章ID（null表示创建新文章，否则更新现有文章）");
            Field(x => x.Title, nullable: true).Description("文章标题（可选，未提供时AI自动生成）");
            Field(x => x.Slug, nullable: true).Description("URL标识（可选，未提供时AI自动生成）");
            Field(x => x.Category, nullable: true).Description("分类（可选，默认\"未分类\"）");
            Field(x => x.Tags, nullable: true).Description("标签数组（可选，未提供时AI自动生成）");
            Field(x => x.Excerpt, nullable: true).Description("摘要（可选，未提供时AI自动生成）");
            Field(x => x.IsFeatured, nullable: true).Description("是否为精选文章（可选）");
            Field(x => x.IsPublished, nullable: true).Description("是否发布（可选）");
            Field(x => x.MainVersion, nullable: true).Description("HTML主版本（可选）");
            Field(x => x.CustomPrompt, nullable: true).Description("自定义AI提示词（可选）");
        }
    }

    /// <summary>
    /// 提交文章输入类型（GraphQL）- Submit按钮
    /// 说明：id为null表示创建并生成首个版本，否则为现有文章创建新版本
    /// </summary>
    public class SubmitArticleInputType : InputObjectGraphType<SubmitArticleCommand>
    {
        public SubmitArticleInputType()
        {
            Name = "SubmitArticleInput";
            Description = "提交文章输入参数（生成新版本HTML并设置为主版本，支持用户提供HTML或AI生成）";

            Field(x => x.Markdown).Description("Markdown 内容（必填）");
            Field(x => x.Id, nullable: true).Description("文章ID（null表示创建新文章，否则为现有文章创建新版本）");
            Field(x => x.Html, nullable: true).Description("HTML 内容（可选，未提供时AI生成）");
            Field(x => x.Title, nullable: true).Description("文章标题（可选，未提供时AI自动生成）");
            Field(x => x.Slug, nullable: true).Description("URL标识（可选，未提供时AI自动生成）");
            Field(x => x.Category, nullable: true).Description("分类（可选，默认\"未分类\"）");
            Field(x => x.Tags, nullable: true).Description("标签数组（可选，未提供时AI自动生成）");
            Field(x => x.Excerpt, nullable: true).Description("摘要（可选，未提供时AI自动生成）");
            Field(x => x.IsFeatured, nullable: true).Description("是否为精选文章（可选）");
            Field(x => x.IsPublished, nullable: true).Description("是否发布（可选）");
            Field(x => x.CustomPrompt, nullable: true).Description("自定义AI提示词（可选）");
        }
    }
}
