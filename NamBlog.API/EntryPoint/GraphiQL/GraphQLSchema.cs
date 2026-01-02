using System;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using NamBlog.API.EntryPoint.GraphiQL.Mutations;
using NamBlog.API.EntryPoint.GraphiQL.Queries;

namespace NamBlog.API.EntryPoint.GraphiQL
{
    public class BlogGraphQLSchema : Schema
    {
        public BlogGraphQLSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = serviceProvider.GetRequiredService<GraphQLQuery>();
            Mutation = serviceProvider.GetRequiredService<GraphQLMutation>();
            // 流式输出已移至 MCP 实现，不再使用 GraphQL Subscription
        }
    }
}
