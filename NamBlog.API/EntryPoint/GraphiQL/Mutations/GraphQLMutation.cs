using GraphQL;
using GraphQL.Types;

namespace NamBlog.API.EntryPoint.GraphiQL.Mutations
{
    public class GraphQLMutation : ObjectGraphType<object>
    {
        public GraphQLMutation()
        {

            //this.AuthorizeWith("AdminPolicy");
            Name = "mutation";
            Description = "变更入口";

            // 认证入口（无需权限验证）
            Field<AuthMutationType>("auth")
                .Description("认证入口（登录）")
                .Resolve(context =>
                {
                    return new { }; // 传递空对象给下一层
                });

            // 博客入口（需要管理员权限的操作）
            Field<BlogMutationType>("blog")
                .Description("博客入口（仅限管理员）")
                .Resolve(context =>
                {
                    return new { };
                });

            // AI智能体入口
            Field<AgentMutationType>("aiAgentTools")
                .Description("AI智能体工具入口（仅限管理员）")
                .AuthorizeWithPolicy("AdminPolicy")
                .Resolve(ctx =>
                {
                    return new { }; // 返回空对象，让 GraphQL.NET 处理字段解析
                });
        }
    }
}
