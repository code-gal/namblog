# GraphQL 复杂度限制配置说明

## 📊 当前配置

### 开发环境（当前）
```csharp
config.MaxDepth = 15;
config.MaxComplexity = 600000;
```

### 为什么需要 600000？

#### Introspection 查询的复杂度计算
根据实际测试，项目的 Schema 的 introspection 查询复杂度为 **571087**。

**复杂度计算公式**：
```
总复杂度 = Σ (类型数 × 字段数 × 参数数 × 嵌套深度 × 类型引用)
```

**你的 Schema 结构**（根据自动生成的文档）：
- **输出类型**：约 15 个（ArticleDetail, ArticleListItem, BlogBasic, ArticleVersion, PageInfo 等）
- **输入类型**：约 5 个（SaveArticleInput, PageInfoInput 等）
- **枚举类型**：约 3 个（HtmlValidationStatus 等）
- **平均字段数**：每个类型 5-15 个字段
- **嵌套深度**：最深 7 层（Query → BlogQuery → ArticleQuery → PagedResult → Items → ArticleVersion → etc.）

**Introspection 查询特点**：
- 需要查询 `__schema` → `types` → 每个 `type` → 所有 `fields` → 每个 `field` 的 `args` 和 `type`
- 每个类型引用需要递归查询 7 层（`ofType.ofType.ofType...`）
- 总查询节点数：23 个类型 × 平均 8 个字段 × 7 层嵌套 ≈ **1288 节点**
- 加上参数、接口、枚举值等，最终复杂度达到 **571087**

### 为什么不能只设置 300？

**300 的限制适用于**：
- ✅ 普通查询：`{ blog { article { list { items { id title } } } } }`（复杂度约 10-50）
- ✅ 嵌套查询：查询文章 + 版本 + 分类统计（复杂度约 100-200）
- ✅ 复杂列表：分页查询 + 过滤 + 多字段（复杂度约 200-300）

**300 不够的场景**：
- ❌ Introspection 查询（571087）
- ❌ GraphiQL UI 加载（需要 introspection）
- ❌ 文档生成工具（需要 introspection）
- ❌ Voyager 可视化（需要 introspection）

## 🔐 生产环境安全配置

### 方案 1：禁用 Introspection（推荐）

在生产环境**完全禁用** introspection，普通业务查询使用 500 的复杂度限制。

#### 实现方式

修改 [`PresentationExtensions.cs`](../NamBlog.API/Extensions/PresentationExtensions.cs)：

```csharp
.AddGraphQL(builder => builder
    .AddSystemTextJson()
    .AddSchema<BlogGraphQLSchema>()
    .AddGraphTypes(typeof(BlogGraphQLSchema).Assembly)
    .AddDataLoader()
    .AddAuthorizationRule()
    .ConfigureExecutionOptions(options =>
    {
        var env = services.BuildServiceProvider().GetRequiredService<IWebHostEnvironment>();
        options.ThrowOnUnhandledException = env.IsDevelopment();
        
        // ⭐ 生产环境禁用 Introspection
        options.EnableMetrics = env.IsDevelopment();
        if (env.IsProduction())
        {
            options.Query = new GraphQLQuery
            {
                // 禁止所有以 __ 开头的系统查询（introspection）
                ValidationRules = new[] 
                { 
                    new NoIntrospectionValidationRule() 
                }
            };
        }
    })
    .AddComplexityAnalyzer(config =>
    {
        var env = services.BuildServiceProvider().GetRequiredService<IWebHostEnvironment>();
        
        config.MaxDepth = 15;
        // 开发环境：允许 introspection（600000）
        // 生产环境：禁用 introspection，仅需 500
        config.MaxComplexity = env.IsDevelopment() ? 600000 : 500;
    })
    .AddUserContextBuilder(BuildUserContext));
```

**需要添加的验证规则**：

```csharp
// 在 PresentationExtensions.cs 底部添加
internal class NoIntrospectionValidationRule : IValidationRule
{
    public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context)
    {
        return new ValueTask<INodeVisitor?>(new NoIntrospectionVisitor(context));
    }
}

internal class NoIntrospectionVisitor : INodeVisitor
{
    private readonly ValidationContext _context;

    public NoIntrospectionVisitor(ValidationContext context)
    {
        _context = context;
    }

    public void Enter(ASTNode node, ValidationContext context)
    {
        if (node is Field field && field.Name.Value.StartsWith("__"))
        {
            context.ReportError(new ValidationError(
                "Introspection is disabled",
                code: "INTROSPECTION_DISABLED",
                nodes: field));
        }
    }

    public void Leave(ASTNode node, ValidationContext context) { }
}
```

### 方案 2：动态复杂度限制（次优）

检测 introspection 查询并动态调整限制。

```csharp
.AddComplexityAnalyzer(config =>
{
    config.MaxDepth = 15;
    config.MaxComplexity = 600000; // 开发环境默认允许
    
    // 在生产环境，通过中间件检测查询内容
    // 如果是 introspection，返回错误
    // 否则使用 500 的限制
})
```

**缺点**：
- 实现复杂，需要自定义中间件
- 仍然暴露了 Schema 结构
- 性能略有损耗

### 方案 3：仅允许已认证用户 Introspection

```csharp
.AddComplexityAnalyzer(config =>
{
    config.MaxDepth = 15;
    config.MaxComplexity = 600000;
    
    // 在 BuildUserContext 中检查
    // 如果是 introspection 查询且用户未登录，拒绝请求
})
```

## 📋 复杂度对照表

| 查询类型 | 示例 | 预估复杂度 | 300 限制 | 600000 限制 |
|---------|------|-----------|---------|------------|
| 简单查询 | 获取博客名称 | 5 | ✅ | ✅ |
| 文章列表 | 10 篇文章，5 个字段 | 50 | ✅ | ✅ |
| 文章详情 | 包含版本信息 | 80 | ✅ | ✅ |
| 分页查询 | 20 篇 + 分类统计 | 150 | ✅ | ✅ |
| 复杂嵌套 | 文章 + 多版本 + 统计 | 280 | ✅ | ✅ |
| **Introspection** | **完整 Schema** | **571087** | ❌ | ✅ |

## 🎯 推荐配置

### 当前开发阶段
```csharp
config.MaxComplexity = 600000;  // 支持 GraphiQL、Voyager、文档生成
```

### 准备上线前
```csharp
// appsettings.Production.json
{
  "GraphQL": {
    "EnableIntrospection": false,
    "MaxComplexity": 500
  }
}

// 代码实现
var config = services.BuildServiceProvider()
    .GetRequiredService<IConfiguration>();
var enableIntrospection = config.GetValue<bool>("GraphQL:EnableIntrospection", true);
var maxComplexity = config.GetValue<int>("GraphQL:MaxComplexity", 500);

config.MaxComplexity = maxComplexity;
```

## 🛡️ 安全建议

1. **开发环境**：
   - ✅ MaxComplexity = 600000
   - ✅ 启用 Introspection
   - ✅ 启用 GraphiQL、Voyager

2. **测试环境**：
   - ✅ MaxComplexity = 600000
   - ✅ 启用 Introspection（内网访问）
   - ⚠️ 限制访问 IP

3. **生产环境**：
   - ✅ MaxComplexity = 500
   - ❌ **禁用 Introspection**
   - ❌ 禁用 GraphiQL（或要求管理员认证）
   - ✅ 启用请求日志和监控

## 📚 参考资料

- [GraphQL Best Practices - Security](https://graphql.org/learn/best-practices/#security)
- [Production Ready GraphQL - Disable Introspection](https://productionreadygraphql.com/)
- [OWASP GraphQL Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/GraphQL_Cheat_Sheet.html)

## 🔄 未来优化

如果 Schema 继续增长（例如添加更多模块、字段），可以考虑：

1. **拆分 Schema**：按功能模块拆分成多个 GraphQL 端点
2. **Schema Stitching**：合并多个小 Schema
3. **自动禁用**：在 CI/CD 中自动检测环境并配置限制
