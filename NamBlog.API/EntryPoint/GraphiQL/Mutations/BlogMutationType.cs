using GraphQL.Types;

namespace NamBlog.API.EntryPoint.GraphiQL.Mutations
{
    /// <summary>
    /// 博客变更入口类型
    /// 注意：deletePost 和 savePost 字段已移除，统一使用 ArticleMutationType
    /// </summary>
    public class BlogMutationType : ObjectGraphType<object>
    {
        public BlogMutationType()
        {
            Name = "BlogMutation";
            Description = "博客变更入口";

            // Phase 2: 文章变更入口
            // 重要：嵌套的 ObjectGraphType 字段应该返回空对象
            // GraphQL.NET 会自动解析 ArticleMutationType 并处理其字段
            Field<ArticleMutationType>("article")
                .Description("文章变更入口")
                .Resolve(ctx =>
                {
                    return new { }; // 返回空对象，让 GraphQL.NET 处理字段解析
                });
        }
    }
}
