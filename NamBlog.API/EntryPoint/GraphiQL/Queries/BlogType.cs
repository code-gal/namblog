using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NamBlog.API.Application.DTOs;

namespace NamBlog.API.EntryPoint.GraphiQL.Queries
{
    /// <summary>
    /// 博客查询入口类型
    /// 注意：post 和 slugVerify 字段已移除，统一使用 ArticleQueryType
    /// 按照 GraphQL.NET 最佳实践：GraphType 不注入 Scoped 服务，在 Resolve 中获取
    /// </summary>
    public class BlogQueryType : ObjectGraphType
    {
        public BlogQueryType()
        {
            Name = "BlogQuery";
            Description = "博客查询操作";
            // Phase 2: 文章查询入口
            Field<ArticleQueryType>("article")
                .Description("文章查询入口")
                .Resolve(ctx => new object());  // 返回一个对象，ArticleQueryType 会处理实际查询

            Field<BlogBasicType>("baseInfo")
                .Description("博客基本信息")
                .Resolve(ctx =>
                {
                    // 从 RequestServices 获取 Scoped 的 BlogSettings
                    var blogSettings = ctx.RequestServices?.GetRequiredService<IOptionsSnapshot<BlogInfo>>();
                    return blogSettings?.Value ?? new BlogInfo();
                });

            Field<ListCollectType>("listCollection")
               .Description("标签/专栏列表及相应文章数量的统计")
               .Argument<StringGraphType>("category", "专栏名")
               .Argument<ListGraphType<StringGraphType>>("tags", "标签集合")
               .Resolve(ctx =>
               {
                   var category = ctx.GetArgument<string>("category")?.Trim();
                   List<string> tags = ctx.GetArgument<List<string>>("tags");
                   return new QueryList { Category = category, Tags = tags };
               });
        }
    }
}
