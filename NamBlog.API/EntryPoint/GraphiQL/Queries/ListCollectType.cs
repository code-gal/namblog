using System;
using System.Collections.Generic;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NamBlog.API.Application.DTOs;
using NamBlog.API.Application.Services;

namespace NamBlog.API.EntryPoint.GraphiQL.Queries
{
    /// <summary>
    /// 列表集合类型（标签和分类统计）
    /// </summary>
    public class ListCollectType : ObjectGraphType<QueryList>
    {
        public ListCollectType()
        {
            Name = "ListCollect";
            Description = "数量统计相关查询";
            Field<ListGraphType<TagStatisticType>>("tags")
                .Description("标签列表及相应文章数量统计（按数量-名称排序）")
                .ResolveAsync(async context =>
                {
                    var articleService = context.RequestServices?.GetRequiredService<ArticleQueryService>();
                    var logger = context.RequestServices?.GetService<ILogger<ListCollectType>>();

                    if (articleService == null)
                    {
                        logger?.LogError("无法获取 ArticleService");
                        return null;
                    }

                    try
                    {
                        var category = context.Source.Category?.Trim();
                        var isAdmin = GraphQLHelper.IsAdmin(context);

                        return await articleService.GetTagsStatisticsAsync(category, isAdmin);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "查询标签统计失败");
                        return null;
                    }
                }
            );

            Field<ListGraphType<CategoryStatisticType>>("categorys")
                .Description("分类列表及相应文章数量统计（按数量-名称排序）")
                .ResolveAsync(async context =>
                {
                    var articleService = context.RequestServices?.GetRequiredService<ArticleQueryService>();
                    var logger = context.RequestServices?.GetService<ILogger<ListCollectType>>();

                    if (articleService == null)
                    {
                        logger?.LogError("无法获取 ArticleService");
                        return new List<CategoryStatistic>();
                    }

                    try
                    {
                        var tags = context.Source.Tags?.ToArray();
                        var isAdmin = GraphQLHelper.IsAdmin(context);

                        return await articleService.GetCategoriesStatisticsAsync(tags, isAdmin);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "查询分类统计失败");
                        return new List<CategoryStatistic>();
                    }
                }
            );
        }
    }

    /// <summary>
    /// 标签统计 GraphQL Type
    /// </summary>
    public class TagStatisticType : ObjectGraphType<TagStatistic>
    {
        public TagStatisticType()
        {
            Name = "TagStatistic";
            Description = "标签统计信息";

            Field(x => x.Name).Description("标签名称");
            Field(x => x.Count).Description("文章数量");
        }
    }

    /// <summary>
    /// 分类统计 GraphQL Type
    /// </summary>
    public class CategoryStatisticType : ObjectGraphType<CategoryStatistic>
    {
        public CategoryStatisticType()
        {
            Name = "CategoryStatistic";
            Description = "分类统计信息";

            Field(x => x.Name).Description("分类名称");
            Field(x => x.Count).Description("文章数量");
        }
    }

    /// <summary>
    /// QueryList 数据模型（用于列表查询参数）
    /// </summary>
    public class QueryList
    {
        /// <summary>
        /// 分类
        /// </summary>
        public string? Category { get; set; }
        /// <summary>
        /// 标签
        /// </summary>
        public List<string>? Tags { get; set; }
    }
}
