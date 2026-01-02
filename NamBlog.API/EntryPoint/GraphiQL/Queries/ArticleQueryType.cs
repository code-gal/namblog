using System;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NamBlog.API.Application.DTOs;
using NamBlog.API.Application.Services;

namespace NamBlog.API.EntryPoint.GraphiQL.Queries
{
    /// <summary>
    /// 文章相关的 GraphQL Query
    /// 直接使用业务层 DTO，无需额外的 GraphQL Type 类
    /// </summary>
    public class ArticleQueryType : ObjectGraphType<object>
    {
        public ArticleQueryType()
        {
            Name = "ArticleQuery";
            Description = "文章查询操作";

            // 分页查询文章列表
            Field<PagedArticleListItemResultType>("articles")
                .Description("分页查询文章列表（支持发布状态、分类、标签、精选过滤）")
                .Argument<IntGraphType>("page", "页码（从1开始，默认1）")
                .Argument<IntGraphType>("pageSize", "每页大小（默认10，最大100）")
                .Argument<BooleanGraphType>("isPublished", "是否已发布（管理员可选，游客固定为已发布）")
                .Argument<BooleanGraphType>("isFeatured", "是否为精选文章（不传则返回全部）")
                .Argument<StringGraphType>("category", "按分类过滤（精确匹配）")
                .Argument<ListGraphType<StringGraphType>>("tags", "按标签过滤（包含任意一个标签即可）")
                .ResolveAsync(async context =>
                {
                    var articleService = context.RequestServices?.GetRequiredService<ArticleQueryService>();
                    var logger = context.RequestServices?.GetService<ILogger<ArticleQueryType>>();

                    if (articleService == null)
                        return null;

                    try
                    {
                        var page = context.GetArgument("page", 1);
                        var pageSize = context.GetArgument("pageSize", 10);
                        var isPublished = context.GetArgument<bool?>("isPublished");
                        var isFeatured = context.GetArgument<bool?>("isFeatured");
                        var category = context.GetArgument<string?>("category");
                        var tags = context.GetArgument<string[]?>("tags");

                        // 权限控制：非管理员强制只能查询已发布文章
                        var isAdmin = GraphQLHelper.IsAdmin(context);
                        if (!isAdmin)
                            isPublished = true;

                        var command = new QueryArticlesCommand
                        {
                            Page = page,
                            PageSize = pageSize,
                            IsPublished = isPublished,
                            IsFeatured = isFeatured,
                            Category = category,
                            Tags = tags
                        };

                        // 直接返回业务层的 PagedResult<ArticleListItemDto>
                        return await articleService.QueryArticlesAsync(command);
                    }
                    catch (Exception ex)
                    {
                        // 公开API：记录日志但不暴露错误给前端
                        logger?.LogError(ex, "查询文章列表失败");
                        return null;
                    }
                });

            // 获取单个文章详情
            Field<ArticleDetailDtoType>("article")
                .Description("获取文章详情（支持指定版本，非主版本需要管理员权限）")
                .Argument<NonNullGraphType<StringGraphType>>("slug", "文章 slug")
                .Argument<StringGraphType>("versionName", "版本名称（可选，默认返回主版本）")
                .ResolveAsync(async context =>
                {
                    var articleService = context.RequestServices?.GetRequiredService<ArticleQueryService>();
                    var logger = context.RequestServices?.GetService<ILogger<ArticleQueryType>>();

                    if (articleService == null)
                        return null;

                    try
                    {
                        var slug = context.GetArgument<string>("slug");
                        var versionName = context.GetArgument<string?>("versionName");
                        var isAdmin = GraphQLHelper.IsAdmin(context);

                        // 直接返回业务层的 ArticleDetailDto
                        return await articleService.GetArticleBySlugAsync(slug, versionName, isAdmin);
                    }
                    catch (Exception ex)
                    {
                        // 公开API：记录日志但不暴露错误给前端
                        logger?.LogError(ex, "获取文章详情失败，slug={Slug}", context.GetArgument<string>("slug"));
                        return null;
                    }
                });
            // 获取指定版本的HTML内容（用于版本预览）
            Field<StringGraphType>("getVersionHtml")
                .Description("获取指定版本的HTML内容（用于版本预览，不修改数据库）")
                .Argument<NonNullGraphType<IntGraphType>>("id", "文章ID")
                .Argument<NonNullGraphType<StringGraphType>>("versionName", "版本名称")
                .ResolveAsync(async context =>
                {
                    var articleService = context.RequestServices?.GetRequiredService<ArticleQueryService>();
                    var logger = context.RequestServices?.GetService<ILogger<ArticleQueryType>>();

                    if (articleService == null)
                        return null;

                    try
                    {
                        var id = context.GetArgument<int>("id");
                        var versionName = context.GetArgument<string>("versionName");

                        var result = await articleService.GetVersionHtmlAsync(id, versionName);

                        // 公开Query：失败返回null，不暴露错误详情
                        return result.IsSuccess ? result.Value : null;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "获取版本HTML失败，id={Id}, versionName={VersionName}",
                            context.GetArgument<int>("id"), context.GetArgument<string>("versionName"));
                        return null;
                    }
                });

            // 获取文章 Markdown 源文件（管理员专用，支持 slug/id/title）
            Field<StringGraphType>("getArticleMarkdown")
                .Description("获取文章的 Markdown 源文件（仅管理员，至少提供一个参数：slug、id 或 title）")
                .Argument<StringGraphType>("slug", "文章 slug（可选）")
                .Argument<IntGraphType>("id", "文章 id（可选）")
                .Argument<StringGraphType>("title", "文章标题（可选，模糊匹配）")
                .ResolveAsync(async context =>
                {
                    var articleService = context.RequestServices?.GetRequiredService<ArticleQueryService>();
                    var logger = context.RequestServices?.GetService<ILogger<ArticleQueryType>>();

                    // 权限检查：仅管理员
                    var isAdmin = GraphQLHelper.IsAdmin(context);
                    if (!isAdmin)
                    {
                        context.Errors.Add(new ExecutionError("无权访问 Markdown 源文件"));
                        return null;
                    }

                    if (articleService == null)
                        return null;

                    try
                    {
                        var slug = context.GetArgument<string?>("slug");
                        var id = context.GetArgument<int?>("id");
                        var title = context.GetArgument<string?>("title");

                        var result = await articleService.GetArticleMarkdownAsync(slug, id, title);

                        if (!result.IsSuccess)
                        {
                            context.Errors.Add(new ExecutionError(result.ErrorMessage ?? "获取 Markdown 失败"));
                            return null;
                        }

                        return result.Value;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "获取 Markdown 失败");
                        context.Errors.Add(new ExecutionError($"获取 Markdown 失败：{ex.Message}"));
                        return null;
                    }
                });
        }
    }

    /// <summary>
    /// ArticleListItemDto 的 GraphQL Type（内联定义，无需单独文件）
    /// </summary>
    public class ArticleListItemDtoType : ObjectGraphType<ArticleListItemDto>
    {
        public ArticleListItemDtoType()
        {
            Name = "ArticleListItem";
            Description = "文章列表项";

            Field(x => x.Id).Description("文章ID");
            Field(x => x.Title).Description("标题");
            Field(x => x.Slug).Description("URL标识");
            Field(x => x.Excerpt).Description("摘要");
            Field(x => x.Category).Description("分类");
            Field(x => x.Tags).Description("标签数组");
            Field(x => x.IsPublished).Description("是否已发布");
            Field(x => x.IsFeatured).Description("是否为精选");
            Field(x => x.PublishedAt, nullable: true).Description("发布时间");
            Field(x => x.LastModified).Description("最后修改时间");
        }
    }

    /// <summary>
    /// ArticleDetailDto 的 GraphQL Type（内联定义）
    /// </summary>
    public class ArticleDetailDtoType : ObjectGraphType<ArticleDetailDto>
    {
        public ArticleDetailDtoType()
        {
            Name = "ArticleDetail";
            Description = "文章详情";

            Field(x => x.Id).Description("文章ID");
            Field(x => x.Title).Description("标题");
            Field(x => x.Slug).Description("URL标识");
            Field(x => x.Author).Description("作者");
            Field(x => x.Excerpt).Description("摘要");
            Field(x => x.Category).Description("分类");
            Field(x => x.Tags).Description("标签数组");
            Field(x => x.AiPrompts).Description("AI 提示词历史");
            Field(x => x.IsPublished).Description("是否已发布");
            Field(x => x.IsFeatured).Description("是否为精选");
            Field(x => x.PublishedAt, nullable: true).Description("发布时间");
            Field(x => x.CreateTime).Description("创建时间");
            Field(x => x.LastModified).Description("最后修改时间");

            // 嵌套版本列表
            Field<ListGraphType<ArticleVersionDtoType>>("versions")
                .Description("版本列表")
                .Resolve(context => context.Source.Versions);

            // 当前发布的主版本（可为空）
            Field<ArticleVersionDtoType>("mainVersion")
                .Description("当前发布的主版本")
                .Resolve(context => context.Source.MainVersion);

            // 嵌套 Resolver：按需加载 Markdown（仅管理员）
            Field<StringGraphType>("markdown")
                .Description("Markdown 源文件（仅管理员可见，按需查询）")
                .ResolveAsync(async context =>
                {
                    var isAdmin = GraphQLHelper.IsAdmin(context);
                    if (!isAdmin)
                    {
                        context.Errors.Add(new ExecutionError("无权访问 Markdown 源文件"));
                        return null;
                    }

                    var articleService = context.RequestServices?.GetRequiredService<ArticleQueryService>();
                    var logger = context.RequestServices?.GetService<ILogger<ArticleDetailDtoType>>();
                    var source = context.Source;

                    if (articleService == null)
                        return null;

                    try
                    {
                        var result = await articleService.ReadMarkdownByPathAsync(source.FilePath, source.FileName);
                        if (!result.IsSuccess)
                        {
                            context.Errors.Add(new ExecutionError(result.ErrorMessage ?? "读取 Markdown 失败"));
                            return null;
                        }

                        return result.Value;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "读取 Markdown 失败：Slug={Slug}", source.Slug);
                        context.Errors.Add(new ExecutionError($"读取 Markdown 失败：{ex.Message}"));
                        return null;
                    }
                });

            // 嵌套 Resolver：按需加载主版本 HTML
            Field<StringGraphType>("mainVersionHtml")
                .Description("主版本 HTML 内容（按需查询，已发布文章公开可见）")
                .ResolveAsync(async context =>
                {
                    var articleService = context.RequestServices?.GetRequiredService<ArticleQueryService>();
                    var logger = context.RequestServices?.GetService<ILogger<ArticleDetailDtoType>>();
                    var source = context.Source;

                    if (articleService == null || source.MainVersion == null)
                        return null;

                    try
                    {
                        // 复用 GetVersionHtmlAsync 方法
                        var result = await articleService.GetVersionHtmlAsync(source.Id, source.MainVersion.VersionName);

                        // 公开 API：失败返回 null，不暴露错误详情
                        return result.IsSuccess ? result.Value : null;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "读取主版本 HTML 失败：Slug={Slug}, Version={Version}",
                            source.Slug, source.MainVersion.VersionName);
                        return null;
                    }
                });
        }
    }

    /// <summary>
    /// ArticleVersionDto 的 GraphQL Type（内联定义）
    /// </summary>
    public class ArticleVersionDtoType : ObjectGraphType<ArticleVersionDto>
    {
        public ArticleVersionDtoType()
        {
            Name = "ArticleVersion";
            Description = "文章版本";

            Field(x => x.VersionId).Description("版本ID");
            Field(x => x.VersionName).Description("版本名称");
            Field(x => x.AiPrompt, nullable: true).Description("AI 提示词");
            Field(x => x.ValidationStatus).Description("HTML 验证状态");
            Field(x => x.ValidationError, nullable: true).Description("验证错误信息");
            Field(x => x.CreatedAt).Description("创建时间");
        }
    }

    /// <summary>
    /// PageInfo 的 GraphQL Type（内联定义）
    /// </summary>
    public class PageInfoType : ObjectGraphType<PageInfo>
    {
        public PageInfoType()
        {
            Name = "PageInfo";
            Description = "分页信息";

            Field(x => x.CurrentPage).Description("当前页码");
            Field(x => x.PageSize).Description("每页大小");
            Field(x => x.TotalCount).Description("总记录数");
            Field(x => x.TotalPages).Description("总页数");
            Field(x => x.HasPreviousPage).Description("是否有上一页");
            Field(x => x.HasNextPage).Description("是否有下一页");
        }
    }

    /// <summary>
    /// PagedResult&lt;ArticleListItemDto&gt; 的 GraphQL Type（内联定义）
    /// </summary>
    public class PagedArticleListItemResultType : ObjectGraphType<PagedResult<ArticleListItemDto>>
    {
        public PagedArticleListItemResultType()
        {
            Name = "PagedArticleListItemResult";
            Description = "分页文章列表结果";

            Field<ListGraphType<ArticleListItemDtoType>>("items")
                .Description("当前页的文章列表")
                .Resolve(context => context.Source.Items);

            Field<PageInfoType>("pageInfo")
                .Description("分页信息")
                .Resolve(context => context.Source.PageInfo);
        }
    }
}
