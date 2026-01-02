using GraphQL.Types;

namespace NamBlog.API.EntryPoint.GraphiQL.Queries
{
    public class GraphQLQuery : ObjectGraphType
    {
        public GraphQLQuery()
        {
            Name = "Query";
            Description = "查询入口";
            Field<BlogQueryType>("blog")
                .Description("博客入口")
                .Resolve(context => new { });
        }
    }
}
