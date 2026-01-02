#!/usr/bin/env dotnet
#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net10.0
#:property LangVersion=preview
#:property NoWarn=IL2026;IL3050

using System.Text.Json;
using System.Text;

// ========== 配置 ==========
const string GraphQLEndpoint = "http://localhost:5000/graphql";
string OutputFile = $"后端API接口规范文档（{DateTime.Now.ToString("yyyy-MM-dd HHmm")}）.md";

// ========== GraphQL Introspection 查询 ==========
const string IntrospectionQuery = @"
{
  __schema {
    queryType { name }
    mutationType { name }
    types {
      kind
      name
      description
      fields(includeDeprecated: true) {
        name
        description
        args {
          name
          description
          type { ...TypeRef }
          defaultValue
        }
        type { ...TypeRef }
        isDeprecated
        deprecationReason
      }
      inputFields {
        name
        description
        type { ...TypeRef }
        defaultValue
      }
      interfaces { ...TypeRef }
      enumValues(includeDeprecated: true) {
        name
        description
        isDeprecated
        deprecationReason
      }
      possibleTypes { ...TypeRef }
    }
  }
}

fragment TypeRef on __Type {
  kind
  name
  ofType {
    kind
    name
    ofType {
      kind
      name
      ofType {
        kind
        name
        ofType {
          kind
          name
          ofType {
            kind
            name
            ofType {
              kind
              name
              ofType {
                kind
                name
              }
            }
          }
        }
      }
    }
  }
}
";

// ========== 主程序 ==========
Console.WriteLine("🚀 NamBlog GraphQL 文档自动生成工具");
Console.WriteLine($"📡 GraphQL 端点: {GraphQLEndpoint}");
Console.WriteLine($"📄 输出文件: {OutputFile}\n");

try
{
    // 1. 检查服务是否运行
    Console.Write("检查 GraphQL 服务...");
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

    // 构建 JSON 请求体（手动转义，避免使用反射序列化）
    var escapedQuery = IntrospectionQuery.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
    var requestBody = $"{{\"query\":\"{escapedQuery}\"}}";
    var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
    var response = await client.PostAsync(GraphQLEndpoint, content);

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine(" ❌");
        Console.WriteLine($"\n错误: 无法连接到 GraphQL 服务 (HTTP {(int)response.StatusCode})");
        Console.WriteLine($"提示: 请先运行 'dotnet run --project NamBlog.API' 启动服务");
        return 1;
    }

    Console.WriteLine(" ✅");

    // 2. 解析 Introspection 结果
    Console.Write("解析 Schema...");
    var jsonText = await response.Content.ReadAsStringAsync();
    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    var result = JsonSerializer.Deserialize<Dictionary<string, IntrospectionResult>>(jsonText, options);
    var schema = result?["data"].__schema ?? throw new Exception("无法解析 Schema");
    Console.WriteLine(" ✅");

    // 3. 生成 Markdown 文档
    Console.Write("生成文档...");
    var markdown = GenerateMarkdown(schema);
    await File.WriteAllTextAsync(OutputFile, markdown, Encoding.UTF8);
    Console.WriteLine(" ✅");

    Console.WriteLine($"\n✨ 文档生成成功！");
    Console.WriteLine($"📂 {Path.GetFullPath(OutputFile)}");
    return 0;
}
catch (HttpRequestException ex)
{
    Console.WriteLine(" ❌");
    Console.WriteLine($"\n网络错误: {ex.Message}");
    Console.WriteLine($"提示: 请确保 NamBlog.API 正在运行 (dotnet run --project NamBlog.API)");
    return 1;
}
catch (Exception ex)
{
    Console.WriteLine(" ❌");
    Console.WriteLine($"\n错误: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    return 1;
}

// ========== 文档生成逻辑 ==========
string GenerateMarkdown(Schema schema)
{
    var sb = new StringBuilder();
    var userTypes = schema.types.Where(t =>
        !t.name.StartsWith("__") &&
        t.kind is "OBJECT" or "INPUT_OBJECT" or "ENUM" or "INTERFACE"
    ).ToList();

    // 标题和概述
    sb.AppendLine("# NamBlog GraphQL API 接口规范（自动生成）");
    sb.AppendLine();
    sb.AppendLine($"> 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    sb.AppendLine($"> GraphQL 端点: {GraphQLEndpoint}");
    sb.AppendLine();
    sb.AppendLine("**主要端点**:");
    sb.AppendLine("- **GraphQL**: `/graphql` (主接口)");
    sb.AppendLine("- **GraphiQL 调试**: `/ui/graphiql`");
    sb.AppendLine("- **Altair Client**: `/ui/altair`");
    sb.AppendLine("- **Voyager 可视化**: `/ui/voyager`");
    sb.AppendLine();

    // 生成 Mermaid 结构图
    GenerateMermaidDiagram(sb, schema, userTypes);

    sb.AppendLine("---");
    sb.AppendLine();

    // Query 类型
    var queryType = userTypes.FirstOrDefault(t => t.name == schema.queryType.name);
    if (queryType != null)
    {
        sb.AppendLine("## 🔍 Query（查询操作）");
        sb.AppendLine();
        GenerateOperationSection(sb, queryType, userTypes, "查询");
        sb.AppendLine();
    }

    // Mutation 类型
    if (schema.mutationType != null)
    {
        var mutationType = userTypes.FirstOrDefault(t => t.name == schema.mutationType.name);
        if (mutationType != null)
        {
            sb.AppendLine("## ✏️ Mutation（修改操作）");
            sb.AppendLine();
            GenerateOperationSection(sb, mutationType, userTypes, "变更");
            sb.AppendLine();
        }
    }

    // 其他类型（按类别分组）
    sb.AppendLine("## 📦 类型定义");
    sb.AppendLine();

    var objectTypes = userTypes.Where(t =>
        t.kind == "OBJECT" &&
        t.name != schema.queryType.name &&
        t.name != schema.mutationType?.name
    ).OrderBy(t => t.name);

    var inputTypes = userTypes.Where(t => t.kind == "INPUT_OBJECT").OrderBy(t => t.name);
    var enumTypes = userTypes.Where(t => t.kind == "ENUM").OrderBy(t => t.name);

    if (objectTypes.Any())
    {
        sb.AppendLine("### 📋 输出类型");
        sb.AppendLine();
        foreach (var type in objectTypes)
        {
            GenerateTypeSection(sb, type, userTypes);
            sb.AppendLine();
        }
    }

    if (inputTypes.Any())
    {
        sb.AppendLine("### 📥 输入类型");
        sb.AppendLine();
        foreach (var type in inputTypes)
        {
            GenerateTypeSection(sb, type, userTypes);
            sb.AppendLine();
        }
    }

    if (enumTypes.Any())
    {
        sb.AppendLine("### 🏷️ 枚举类型");
        sb.AppendLine();
        foreach (var type in enumTypes)
        {
            GenerateTypeSection(sb, type, userTypes);
            sb.AppendLine();
        }
    }

    return sb.ToString();
}

void GenerateMermaidDiagram(StringBuilder sb, Schema schema, List<FullType> allTypes)
{
    sb.AppendLine("## 📊 API 结构总览");
    sb.AppendLine();
    sb.AppendLine("```mermaid");
    sb.AppendLine("graph TB");
    sb.AppendLine();

    var queryType = allTypes.FirstOrDefault(t => t.name == schema.queryType.name);
    var mutationType = schema.mutationType != null ?
        allTypes.FirstOrDefault(t => t.name == schema.mutationType.name) : null;

    // Query 部分
    if (queryType?.fields?.Any() == true)
    {
        sb.AppendLine("    Query[\"🔍 Query<br/>查询入口\"]");

        foreach (var field in queryType.fields.OrderBy(f => f.name))
        {
            var nodeId = $"Q_{field.name}";
            var label = field.name;
            sb.AppendLine($"    Query --> {nodeId}[\"{label}\"]");

            // 如果字段有子查询，展示一级子字段
            var fieldType = GetBaseTypeName(field.type);
            var fieldTypeObj = allTypes.FirstOrDefault(t => t.name == fieldType && t.kind == "OBJECT");

            if (fieldTypeObj?.fields?.Any() == true && fieldTypeObj.fields.Count <= 6)
            {
                foreach (var subField in fieldTypeObj.fields.Take(5))
                {
                    var subNodeId = $"{nodeId}_{subField.name}";
                    sb.AppendLine($"    {nodeId} -.-> {subNodeId}[\"{subField.name}\"]");
                }

                if (fieldTypeObj.fields.Count > 5)
                {
                    sb.AppendLine($"    {nodeId} -.-> {nodeId}_more[\"...\"]");
                }
            }
        }

        sb.AppendLine();
    }

    // Mutation 部分
    if (mutationType?.fields?.Any() == true)
    {
        sb.AppendLine("    Mutation[\"✏️ Mutation<br/>修改入口\"]");

        foreach (var field in mutationType.fields.OrderBy(f => f.name))
        {
            var nodeId = $"M_{field.name}";
            var label = field.name;
            sb.AppendLine($"    Mutation --> {nodeId}[\"{label}\"]");

            // 如果字段有子操作，展示一级子字段
            var fieldType = GetBaseTypeName(field.type);
            var fieldTypeObj = allTypes.FirstOrDefault(t => t.name == fieldType && t.kind == "OBJECT");

            if (fieldTypeObj?.fields?.Any() == true && fieldTypeObj.fields.Count <= 6)
            {
                foreach (var subField in fieldTypeObj.fields.Take(5))
                {
                    var subNodeId = $"{nodeId}_{subField.name}";
                    var opIcon = subField.name.Contains("create") ? "➕" :
                                 subField.name.Contains("update") ? "✏️" :
                                 subField.name.Contains("delete") ? "🗑️" : "⚙️";
                    sb.AppendLine($"    {nodeId} -.-> {subNodeId}[\"{opIcon} {subField.name}\"]");
                }

                if (fieldTypeObj.fields.Count > 5)
                {
                    sb.AppendLine($"    {nodeId} -.-> {nodeId}_more[\"...\"]");
                }
            }
        }

        sb.AppendLine();
    }

    // 样式
    sb.AppendLine("    classDef queryStyle fill:#e3f2fd,stroke:#1976d2,stroke-width:2px");
    sb.AppendLine("    classDef mutationStyle fill:#fff3e0,stroke:#f57c00,stroke-width:2px");
    sb.AppendLine("    class Query queryStyle");
    sb.AppendLine("    class Mutation mutationStyle");

    sb.AppendLine("```");
    sb.AppendLine();
}

void GenerateOperationSection(StringBuilder sb, FullType type, List<FullType> allTypes, string operationType)
{
    if (type.fields?.Any() != true) return;

    foreach (var field in type.fields)
    {
        var returnType = FormatType(field.type);

        sb.AppendLine($"### `{field.name}`");

        if (!string.IsNullOrEmpty(field.description))
        {
            sb.AppendLine();
            sb.AppendLine($"> {field.description}");
        }

        sb.AppendLine();

        // 返回类型
        sb.AppendLine($"**返回类型**: `{returnType}`");
        sb.AppendLine();

        // 参数列表
        if (field.args?.Any() == true)
        {
            sb.AppendLine($"**输入参数**:");
            sb.AppendLine();

            // 使用表格展示参数
            sb.AppendLine("| 参数名 | 类型 | 必填 | 说明 | 默认值 |");
            sb.AppendLine("|--------|------|------|------|--------|");

            foreach (var arg in field.args)
            {
                var argType = FormatType(arg.type);
                var isRequired = argType.EndsWith("!") ? "✅" : "❌";
                var description = arg.description?.Replace("\n", " ").Replace("|", "\\|") ?? "-";
                var defaultValue = string.IsNullOrEmpty(arg.defaultValue) ? "-" : $"`{arg.defaultValue}`";

                sb.AppendLine($"| `{arg.name}` | `{argType}` | {isRequired} | {description} | {defaultValue} |");
            }

            sb.AppendLine();
        }
        else
        {
            sb.AppendLine($"**输入参数**: 无");
            sb.AppendLine();
        }

        // 展示返回类型的结构
        var returnTypeName = GetBaseTypeName(field.type);
        var returnTypeObj = allTypes.FirstOrDefault(t => t.name == returnTypeName);
        if (returnTypeObj?.fields?.Any() == true)
        {
            sb.AppendLine("**返回类型结构**:");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine($"{returnTypeName} {{");
            foreach (var f in returnTypeObj.fields.Take(10))
            {
                var fType = FormatType(f.type);
                sb.AppendLine($"  {f.name}: {fType}");
            }

            if (returnTypeObj.fields.Count > 10)
            {
                sb.AppendLine($"  ... 还有 {returnTypeObj.fields.Count - 10} 个字段");
            }
            
            sb.AppendLine("}");
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // 废弃警告
        if (field.isDeprecated)
        {
            sb.AppendLine($"> ⚠️ **已废弃**: {field.deprecationReason}");
            sb.AppendLine();
        }

        // 添加分隔线
        sb.AppendLine("---");
        sb.AppendLine();
    }
}

void GenerateTypeSection(StringBuilder sb, FullType type, List<FullType> allTypes)
{
    sb.AppendLine($"### `{type.name}`");
    if (!string.IsNullOrEmpty(type.description))
    {
        sb.AppendLine();
        sb.AppendLine($"> {type.description}");
    }

    sb.AppendLine();

    // 字段
    if (type.fields?.Any() == true)
    {
        foreach (var field in type.fields)
        {
            var fieldType = FormatType(field.type);
            sb.Append($"- **`{field.name}`**: `{fieldType}`");

            if (!string.IsNullOrEmpty(field.description))
            {
                sb.Append($" - {field.description}");
            }

            sb.AppendLine();

            // 参数
            if (field.args?.Any() == true)
            {
                foreach (var arg in field.args)
                {
                    var argType = FormatType(arg.type);
                    sb.Append($"  - **`{arg.name}`**: `{argType}`");

                    if (!string.IsNullOrEmpty(arg.description))
                    {
                        sb.Append($" - {arg.description}");
                    }

                    if (!string.IsNullOrEmpty(arg.defaultValue))
                    {
                        sb.Append($" (默认: `{arg.defaultValue}`)");
                    }

                    sb.AppendLine();
                }
            }

            if (field.isDeprecated)
            {
                sb.AppendLine($"  - ⚠️ **已废弃**: {field.deprecationReason}");
            }
        }

        sb.AppendLine();
    }

    // 输入字段
    if (type.inputFields?.Any() == true)
    {
        foreach (var field in type.inputFields)
        {
            var fieldType = FormatType(field.type);
            sb.Append($"- **`{field.name}`**: `{fieldType}`");

            if (!string.IsNullOrEmpty(field.description))
            {
                sb.Append($" - {field.description}");
            }

            if (!string.IsNullOrEmpty(field.defaultValue))
            {
                sb.Append($" (默认: `{field.defaultValue}`)");
            }

            sb.AppendLine();
        }

        sb.AppendLine();
    }

    // 枚举值
    if (type.enumValues?.Any() == true)
    {
        foreach (var enumValue in type.enumValues)
        {
            sb.Append($"- **`{enumValue.name}`**");

            if (!string.IsNullOrEmpty(enumValue.description))
            {
                sb.Append($" - {enumValue.description}");
            }

            if (enumValue.isDeprecated)
            {
                sb.Append($" ⚠️ 已废弃: {enumValue.deprecationReason}");
            }

            sb.AppendLine();
        }

        sb.AppendLine();
    }
}

string FormatType(TypeRef typeRef)
{
    return typeRef.kind switch
    {
        "NON_NULL" => $"{FormatType(typeRef.ofType!)}!",
        "LIST" => $"[{FormatType(typeRef.ofType!)}]",
        _ => typeRef.name ?? "Unknown"
    };
}

string GetBaseTypeName(TypeRef typeRef)
{
    // 递归获取最基础的类型名称（去除 NON_NULL 和 LIST 包装）
    return typeRef.kind switch
    {
        "NON_NULL" => GetBaseTypeName(typeRef.ofType!),
        "LIST" => GetBaseTypeName(typeRef.ofType!),
        _ => typeRef.name ?? "Unknown"
    };
}

// ========== 数据模型（必须在顶级语句之后定义） ==========
#pragma warning disable IDE1006 // 命名样式 - JSON 序列化需要小写属性名
file record IntrospectionResult(Schema __schema);
file record Schema(NamedType queryType, NamedType? mutationType, List<FullType> types);
file record NamedType(string name);
file record FullType(
    string kind,
    string name,
    string? description,
    List<Field>? fields,
    List<InputValue>? inputFields,
    List<TypeRef>? interfaces,
    List<EnumValue>? enumValues,
    List<TypeRef>? possibleTypes
);
file record Field(
    string name,
    string? description,
    List<InputValue>? args,
    TypeRef type,
    bool isDeprecated,
    string? deprecationReason
);
file record InputValue(string name, string? description, TypeRef type, string? defaultValue);
file record EnumValue(string name, string? description, bool isDeprecated, string? deprecationReason);
file record TypeRef(string kind, string? name, TypeRef? ofType);
#pragma warning restore IDE1006
