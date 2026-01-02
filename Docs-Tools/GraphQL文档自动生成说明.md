# GraphQL 文档自动生成工具使用说明

## 📁 文件位置

- **脚本**: [`generate-graphql-docs.cs`](../generate-graphql-docs.cs)
- **输出**: [`DevDocs/后端API接口规范文档（自动生成）.md`](后端API接口规范文档（自动生成）.md)

## 🚀 使用方法

 需安装 .NET 10 SDK

### 方式 1: 直接运行（推荐）

```powershell
# 1. 启动 NamBlog.API
cd NamBlog.API
dotnet run

# 2. 在另一个终端运行生成脚本
cd ..
dotnet run generate-graphql-docs.cs
```

### 方式 2: Linux/macOS 可执行

```bash
chmod +x generate-graphql-docs.cs
./generate-graphql-docs.cs
```


### 核心功能

- ✅ **GraphQL Introspection**: 自动读取完整 Schema
- ✅ **智能分类**: Query、Mutation、输出类型、输入类型、枚举
- ✅ **详细文档**: 自动提取字段描述、参数、类型信息
- ✅ **准确性保证**: 直接读取运行时 Schema，不会遗漏更新

## 📋 生成的文档结构

```markdown
# NamBlog GraphQL API 接口规范（自动生成）

> 生成时间: yyyy-MM-dd HH:mm:ss
> GraphQL 端点: http://localhost:5000/graphql

## 🔍 Query（查询操作）
- 列出所有查询字段及参数

## ✏️ Mutation（修改操作）
- 列出所有修改操作及参数

## 📦 类型定义
### 📋 输出类型
- 所有 ObjectGraphType

### 📥 输入类型
- 所有 InputObjectGraphType

### 🏷️ 枚举类型
- 所有 EnumerationGraphType
```

## 🔧 配置说明

### 脚本配置（可修改）

```csharp
// ========== 配置 ==========
const string GraphQLEndpoint = "http://localhost:5000/graphql";
const string OutputFile = "DevDocs/后端API接口规范文档（自动生成）.md";
```

### 项目配置（已优化）

在 [`PresentationExtensions.cs`](../NamBlog.API/Extensions/PresentationExtensions.cs#L46-L56) 中：

```csharp
.AddComplexityAnalyzer(config =>
{
    config.MaxDepth = 15;
    // Introspection 查询复杂度约 571087，需要 600000
    // 普通业务查询复杂度通常 < 500
    config.MaxComplexity = 600000;
})
```

⚠️ **安全说明**：
- **开发环境**：600000（支持 introspection）
- **生产环境**：建议禁用 introspection 或限制为 500
- 详见 [`GraphQL复杂度限制配置说明.md`](GraphQL复杂度限制配置说明.md)

## 🛠️ 故障排除

### 问题 1: 连接失败

```
❌ 错误: 无法连接到 GraphQL 服务
```

**解决方案**: 确保 NamBlog.API 正在运行
```powershell
cd NamBlog.API
dotnet run
```

### 问题 2: 复杂度超限

```
Query is too complex to execute. Complexity is xxxxx
```

**原因**: Introspection 查询需要递归查询整个 Schema，复杂度高

**解决方案**: 在 `PresentationExtensions.cs` 中将限制调搞

**详细说明**: 参见 [`GraphQL复杂度限制配置说明.md`](GraphQL复杂度限制配置说明.md)


## 📝 维护建议

### 何时重新生成文档？

✅ **推荐场景**:
- 添加新的 GraphQL Query/Mutation
- 修改字段类型或参数
- 添加新的输入/输出类型
- 更新字段描述（Description）

❌ **无需重新生成**:
- 仅修改业务逻辑（不影响 Schema）
- 数据库 Migration
- 配置调整

### 自动化建议

可以在 CI/CD 流程中添加：

```yaml
# .github/workflows/docs.yml
- name: Generate GraphQL Docs
  run: |
    cd NamBlog.API
    dotnet run &
    sleep 10
    cd ..
    dotnet run generate-graphql-docs.cs
    git add DevDocs/后端API接口规范文档（自动生成）.md
    git commit -m "docs: 更新 GraphQL API 文档" || true
```

## 🎯 优势对比

| 维护方式 | 优点 | 缺点 |
|---------|------|------|
| **手动维护** | 可自定义格式 | 容易遗漏、耗时、易出错 |
| **自动生成** | 准确、快速、始终同步 | 格式固定、需运行服务 |

**结论**: 推荐使用自动生成，Schema 变更后运行一次即可。

## 📚 参考资料

- [GraphQL Introspection 规范](https://spec.graphql.org/draft/#sec-Introspection)
- [C# 14 File-based Programs](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
- [GraphQL.NET 文档](https://graphql-dotnet.github.io/)
