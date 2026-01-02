using System.Collections.Generic;
using GraphQL.Types;
using NamBlog.API.Application.DTOs;

namespace NamBlog.API.EntryPoint.GraphiQL.Queries
{
    /// <summary>
    /// 博客基本信息类型
    /// 按照 GraphQL.NET 官方最佳实践：
    /// - GraphType 本身不注入 Scoped 服务
    /// - 利用 context.Source (BlogSettings) 获取数据
    /// - 如果需要 Scoped 服务，在 Resolve 中通过 context.RequestServices 获取
    /// </summary>
    public class BlogBasicType : ObjectGraphType<BlogInfo>
    {
        public BlogBasicType()
        {
            Name = "BlogBasic";
            Description = "博客基本信息查询";
            // ============================================================
            // 方案 A: 使用 context.Source (最佳实践，当前启用)
            // 利用父字段传递的 BlogSettings 实例
            // ============================================================

            Field<StringGraphType>("blogName")
                .Description("博客名")
                .Resolve(ctx => ctx.Source.BlogName);

            Field<StringGraphType>("blogger")
                .Description("博主名")
                .Resolve(ctx => ctx.Source.Blogger);

            Field<StringGraphType>("icon")
                .Description("网站图标")
                .Resolve(ctx => $"{ctx.Source.Domain?.TrimEnd('/')}/{ctx.Source.Icon?.TrimStart('/')}");

            Field<StringGraphType>("avatar")
                .Description("博客头像")
                .Resolve(ctx => $"{ctx.Source.Domain?.TrimEnd('/')}/{ctx.Source.Avatar?.TrimStart('/')}");

            Field<StringGraphType>("slogan")
                .Description("博客简介")
                .Resolve(ctx => ctx.Source.Slogan);

            Field<ListGraphType<OuterChainType>>("outerChains")
                .Description("博客外链（名称、链接、SVG")
                .Resolve(ctx =>
                {
                    var outerChains = new List<OuterChain>();
                    foreach (var chain in ctx.Source.OuterChains ?? [])
                    {
                        outerChains.Add(new OuterChain
                        {
                            Name = chain.Name,
                            Link = chain.Link,
                            Svg = $"{ctx.Source.Domain?.TrimEnd('/')}/{chain.Svg?.TrimStart('/')}"
                        });
                    }

                    return outerChains;
                });

            #region 废弃
            // ============================================================
            // 方案 B: 使用 context.RequestServices (如果父字段没有传递 Source)
            // 注释掉以备测试切换
            // ============================================================
            /*
            Field<StringGraphType>("blogName")
                .Description("博客名")
                .Resolve(ctx =>
                {
                    var blogInfo = ctx.RequestServices?.GetRequiredService<IOptionsSnapshot<BlogSettings>>();
                    return blogInfo?.Value.BlogName;
                });

            Field<StringGraphType>("blogger")
                .Description("博主")
                .Resolve(ctx =>
                {
                    var blogInfo = ctx.RequestServices?.GetRequiredService<IOptionsSnapshot<BlogSettings>>();
                    return blogInfo?.Value.Blogger;
                });

            Field<StringGraphType>("icon")
                .Description("网站图标")
                .Resolve(ctx =>
                {
                    var blogInfo = ctx.RequestServices?.GetRequiredService<IOptionsSnapshot<BlogSettings>>();
                    return blogInfo != null ? $"{blogInfo.Value.Domain}{blogInfo.Value.Icon}" : null;
                });

            Field<StringGraphType>("avatar")
                .Description("博客头像")
                .Resolve(ctx =>
                {
                    var blogInfo = ctx.RequestServices?.GetRequiredService<IOptionsSnapshot<BlogSettings>>();
                    return blogInfo != null ? $"{blogInfo.Value.Domain}{blogInfo.Value.Avatar}" : null;
                });

            Field<StringGraphType>("slogan")
                .Description("博客简介")
                .Resolve(ctx =>
                {
                    var blogInfo = ctx.RequestServices?.GetRequiredService<IOptionsSnapshot<BlogSettings>>();
                    return blogInfo?.Value.Slogan;
                });

            Field<ListGraphType<OuterChainType>>("outerChains")
                .Description("博主外链")
                .Resolve(ctx =>
                {
                    var blogInfo = ctx.RequestServices?.GetRequiredService<IOptionsSnapshot<BlogSettings>>();
                    if (blogInfo == null) return new List<OuterChain>();

                    var outerChains = new List<OuterChain>();
                    foreach (var chain in blogInfo.Value.OuterChains ?? [])
                    {
                        outerChains.Add(new OuterChain
                        {
                            Name = chain.Name,
                            Link = chain.Link,
                            Svg = $"{blogInfo.Value.Domain}{chain.Svg}"
                        });
                    }
                    return outerChains;
                });
            */

            // ============================================================
            // 方案 C: 使用 ResolveScopedAsync (需要 GraphQL.MicrosoftDI 包)
            // 注释掉以备测试切换
            // ============================================================
            /*
            Field<StringGraphType>("blogName")
                .Description("博客名")
                .ResolveScopedAsync(ctx =>
                {
                    var blogInfo = ctx.RequestServices.GetRequiredService<IOptionsSnapshot<BlogSettings>>();
                    return Task.FromResult(blogInfo.Value.BlogName);
                });

            Field<StringGraphType>("blogger")
                .Description("博主")
                .ResolveScopedAsync(ctx =>
                {
                    var blogInfo = ctx.RequestServices.GetRequiredService<IOptionsSnapshot<BlogSettings>>();
                    return Task.FromResult(blogInfo.Value.Blogger);
                });

            Field<StringGraphType>("icon")
                .Description("网站图标")
                .ResolveScopedAsync(ctx =>
                {
                    var blogInfo = ctx.RequestServices.GetRequiredService<IOptionsSnapshot<BlogSettings>>();
                    return Task.FromResult($"{blogInfo.Value.Domain}{blogInfo.Value.Icon}");
                });

            Field<StringGraphType>("avatar")
                .Description("博客头像")
                .ResolveScopedAsync(ctx =>
                {
                    var blogInfo = ctx.RequestServices.GetRequiredService<IOptionsSnapshot<BlogSettings>>();
                    return Task.FromResult($"{blogInfo.Value.Domain}{blogInfo.Value.Avatar}");
                });

            Field<StringGraphType>("slogan")
                .Description("博客简介")
                .ResolveScopedAsync(ctx =>
                {
                    var blogInfo = ctx.RequestServices.GetRequiredService<IOptionsSnapshot<BlogSettings>>();
                    return Task.FromResult(blogInfo.Value.Slogan);
                });

            Field<ListGraphType<OuterChainType>>("outerChains")
                .Description("博主外链")
                .ResolveScopedAsync(ctx =>
                {
                    var blogInfo = ctx.RequestServices.GetRequiredService<IOptionsSnapshot<BlogSettings>>();
                    var outerChains = new List<OuterChain>();
                    foreach (var chain in blogInfo.Value.OuterChains ?? [])
                    {
                        outerChains.Add(new OuterChain
                        {
                            Name = chain.Name,
                            Link = chain.Link,
                            Svg = $"{blogInfo.Value.Domain}{chain.Svg}"
                        });
                    }
                    return Task.FromResult<IEnumerable<OuterChain>>(outerChains);
                });
            */

            // ============================================================
            // 方案 D: 使用构建器模式 WithScope + WithService (最优雅)
            // 需要 GraphQL.MicrosoftDI 包，注释掉以备测试切换
            // ============================================================
            /*
            Field<StringGraphType>("blogName")
                .Description("博客名")
                .Resolve()
                .WithScope()
                .WithService<IOptionsSnapshot<BlogSettings>>()
                .ResolveAsync(async (ctx, blogInfo) =>
                {
                    await Task.CompletedTask; // 占位，实际可能不需要 async
                    return blogInfo.Value.BlogName;
                });

            Field<StringGraphType>("blogger")
                .Description("博主")
                .Resolve()
                .WithScope()
                .WithService<IOptionsSnapshot<BlogSettings>>()
                .ResolveAsync(async (ctx, blogInfo) =>
                {
                    await Task.CompletedTask;
                    return blogInfo.Value.Blogger;
                });

            Field<StringGraphType>("icon")
                .Description("网站图标")
                .Resolve()
                .WithScope()
                .WithService<IOptionsSnapshot<BlogSettings>>()
                .ResolveAsync(async (ctx, blogInfo) =>
                {
                    await Task.CompletedTask;
                    return $"{blogInfo.Value.Domain}{blogInfo.Value.Icon}";
                });

            Field<StringGraphType>("avatar")
                .Description("博客头像")
                .Resolve()
                .WithScope()
                .WithService<IOptionsSnapshot<BlogSettings>>()
                .ResolveAsync(async (ctx, blogInfo) =>
                {
                    await Task.CompletedTask;
                    return $"{blogInfo.Value.Domain}{blogInfo.Value.Avatar}";
                });

            Field<StringGraphType>("slogan")
                .Description("博客简介")
                .Resolve()
                .WithScope()
                .WithService<IOptionsSnapshot<BlogSettings>>()
                .ResolveAsync(async (ctx, blogInfo) =>
                {
                    await Task.CompletedTask;
                    return blogInfo.Value.Slogan;
                });

            Field<ListGraphType<OuterChainType>>("outerChains")
                .Description("博主外链")
                .Resolve()
                .WithScope()
                .WithService<IOptionsSnapshot<BlogSettings>>()
                .ResolveAsync(async (ctx, blogInfo) =>
                {
                    await Task.CompletedTask;
                    var outerChains = new List<OuterChain>();
                    foreach (var chain in blogInfo.Value.OuterChains ?? [])
                    {
                        outerChains.Add(new OuterChain
                        {
                            Name = chain.Name,
                            Link = chain.Link,
                            Svg = $"{blogInfo.Value.Domain}{chain.Svg}"
                        });
                    }
                    return outerChains;
                });
            */
            #endregion
        }
    }

    /// <summary>
    /// 外链类型
    /// </summary>
    public class OuterChainType : ObjectGraphType<OuterChain>
    {
        public OuterChainType()
        {
            Name = "OuterChain";
            Description = "博客外链查询";
            Field(x => x.Name).Description("外链名称");
            Field(x => x.Link).Description("外链地址");
            Field(x => x.Svg).Description("外链图标");
        }
    }
}
