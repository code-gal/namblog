# AI Agents 模块使用文档

## 📁 文件结构

```
Infrastructure/Agents/
├── AISettings.cs          # AI 服务配置（API Key、Model 等）
├── PromptsConfig.cs       # 提示词配置模型（映射 prompts.json）
├── OpenAIService.cs       # AI 服务实现（提示词组装、HTML 生成）
└── README.md             # 本文档
```

---

## 🎯 核心功能

### 1. 提示词组装逻辑

**优先级顺序**（从高到低）：

```
1. 根系统提示词 (Root System Prompt)      - 最高优先级，不可覆盖
   ├── 优先从 prompts.json → Prompts → MarkdownToHtml → RootSystemPrompt 读取
   └── 如果 JSON 配置缺失，使用代码内置的默认值

2. 自定义提示词 (Custom Prompt)            - 用户针对单次请求的提示词
   └── 来源：PostVersion.AiPrompt 字段

3. 用户全局提示词 (User Global Prompt)     - 仅在没有自定义提示词时生效
   └── 来源：prompts.json → Prompts → MarkdownToHtml → UserGlobalPrompt

4. 资源列表提示词 (Resources Prompt)       - 推荐的本地或 CDN 资源
   └── 来源：prompts.json → Prompts → MarkdownToHtml → Resources
```

**实现代码**：

```csharp
// 调用 AI 生成 HTML
var customPrompt = postVersion?.AiPrompt; // 从数据库读取
var result = await _aiService.RenderMarkdownToHtmlAsync(markdown, customPrompt);
```

---

## 📄 配置文件：`prompts.json`

**配置路径**：`./data/config/prompts.json`

### 配置结构

```json
{
  "Prompts": {
    "MarkdownToHtml": {
      "RootSystemPrompt": "你是专业的 Markdown 到 HTML 转换器...",
      "UserGlobalPrompt": "- 现代化扁平设计\\n- Monokai 代码配色",
      "Resources": [
        {
          "Domain": "cdn.jsdelivr.net",
          "Description": "jsDelivr CDN"
        },
        {
          "Url": "https://cdn.jsdelivr.net/npm/highlight.js@11/highlight.min.js",
          "Description": "Highlight.js 代码高亮库"
        }
      ],
      "Validation": {
        "Mode": "warning",
        "CheckExternalScripts": true,
        "TrustedDomains": [
          "cdn.jsdelivr.net",
          "cdnjs.cloudflare.com"
        ]
      }
    },
    "MetadataGeneration": {
      "TitlePrompt": "根据以下 Markdown 内容生成标题...",
      "SlugPrompt": "将标题转换为 URL 友好的 slug...",
      "TagsPrompt": "生成 1-10 个相关标签...",
      "ExcerptPrompt": "生成简洁的摘要..."
    }
  }
}
```

### 配置注册

```csharp
// Program.cs
builder.Configuration.AddJsonFile(
    path: "data/config/prompts.json",
    optional: true,
    reloadOnChange: true
);

// ApplicationServiceExtensions.cs
services.Configure<PromptsConfig>(configuration.GetSection("Prompts"));
```

**优势**：
- ✅ **命名空间隔离**：`Prompts` 节点避免与其他配置冲突
- ✅ **一致性**：与 `AI`、`Storage` 等配置风格一致
- ✅ **清晰的配置边界**：所有 AI 提示词配置都在 `Prompts` 节点下

---

## 🔐 HTML 安全验证

### 验证模式

| 模式 | 说明 | 推荐场景 |
|------|------|---------|
| `strict` | 严格阻止非可信域名的脚本 | 高安全要求场景 |
| `warning` | 警告但允许（推荐） | 一般使用场景 |
| `permissive` | 不检查（不推荐） | 测试环境 |

### 内联 JavaScript 支持

✅ **允许**内联 JavaScript（在 `<script>` 标签内）：

```html
<script>
  // 简单的交互功能
  document.addEventListener('DOMContentLoaded', function() {
    console.log('页面加载完成');
  });
</script>
```

**优势**：
- 简单功能无需依赖外部脚本
- 减少网络请求，提升性能
- 更容易实现定制化交互

### 域名信任机制

支持**域名级别匹配**（而非完整 URL）：

```json
{
  "Prompts": {
    "MarkdownToHtml": {
      "Validation": {
        "TrustedDomains": [
          "cdn.jsdelivr.net"
        ]
      }
    }
  }
}
```

**匹配规则**：
- ✅ `https://cdn.jsdelivr.net/npm/highlight.js@11/highlight.min.js`
- ✅ `https://cdn.jsdelivr.net/npm/any-library/any-version.js`
- ❌ `https://evil-cdn.com/malicious.js`

---

## 🔄 配置热重载

### 机制说明

使用 **ASP.NET Core 原生的 `IOptionsMonitor<T>`** 实现配置热重载：

1. **配置文件变化时自动生效**
   - 修改 `prompts.json` 文件
   - ASP.NET Core 的 `FileSystemWatcher` 自动检测变化
   - `IOptionsMonitor<PromptsConfig>.CurrentValue` 自动更新
   - **无需重启应用程序**

2. **实时生效**
   - 每次调用 AI 服务时，自动使用最新配置
   - 性能优化：配置缓存在内存中，仅文件变化时重新加载

### 配置合并规则

ASP.NET Core 采用**路径级别合并**策略：

```json
// appsettings.json
{
  "AI": {
    "Model": "gpt-4",
    "Temperature": 0.7
  }
}

// prompts.json（后加载）
{
  "Prompts": {
    "MarkdownToHtml": {
      "RootSystemPrompt": "..."
    }
  }
}

// 最终合并结果：
{
  "AI": {
    "Model": "gpt-4",              // ✅ 保留
    "Temperature": 0.7             // ✅ 保留
  },
  "Prompts": {                      // ✅ 新增
    "MarkdownToHtml": {
      "RootSystemPrompt": "..."
    }
  }
}
```

**关键点**：
- ✅ **无冲突**：`Prompts` 节点与其他配置节点（`AI`、`Storage`）独立
- ✅ **路径级别合并**：只覆盖冲突的键，保留非冲突的键
- ✅ **深度合并**：嵌套对象也会合并

### 配置优先级

```
高优先级（后加载）
    ↓
4. prompts.json          // 最高优先级
3. config.json           // 用户自定义配置
2. appsettings.{env}.json // 环境特定配置
1. appsettings.json      // 基础配置
    ↑
低优先级（先加载）
```

---

## 🛠️ 使用示例

### 示例 1：使用默认配置生成 HTML

```csharp
// 不传 customPrompt，使用 prompts.json → Prompts → MarkdownToHtml → UserGlobalPrompt
var result = await _aiService.RenderMarkdownToHtmlAsync(markdown);
```

### 示例 2：使用自定义提示词

```csharp
// 传入 customPrompt，覆盖 UserGlobalPrompt
var customPrompt = "生成深色主题的技术博客，代码块使用 Dracula 配色";
var result = await _aiService.RenderMarkdownToHtmlAsync(markdown, customPrompt);
```

### 示例 3：从数据库读取提示词

```csharp
// 从 PostVersion 读取 AiPrompt
var version = await _postRepository.GetVersionAsync(versionId);
var result = await _aiService.RenderMarkdownToHtmlAsync(
    markdown, 
    version.AiPrompt  // 可能为 null（使用默认配置）
);
```

### 示例 4：修改配置并热重载

```bash
# 1. 编辑配置文件
vim ./data/config/prompts.json

# 2. 修改 UserGlobalPrompt
{
  "Prompts": {
    "MarkdownToHtml": {
      "UserGlobalPrompt": "- 使用 Dracula 配色\\n- 添加动画效果"
    }
  }
}

# 3. 保存文件（ASP.NET Core 自动检测）

# 4. 调用 AI 服务（自动使用新配置）
```

---

## 📊 日志和警告

### 配置加载日志

```
✅ AI生成HTML成功 - 尝试: 1, 长度: 15234
⚠️  HTML 验证警告: 检测到来自非可信域名的外部脚本: https://evil-cdn.com/script.js
❌ HTML验证失败 - 尝试 1/3: 缺少 DOCTYPE 声明
```

---

## 📝 最佳实践

### ✅ 推荐做法

1. **配置文件管理**
   - 使用 Git 管理 `prompts.json` 配置
   - 不同环境使用不同的配置文件（通过 `config.json` 覆盖）
   - 利用热重载特性，无需重启调整提示词

2. **安全性**
   - 使用 `warning` 模式进行 HTML 验证
   - 定期更新 `TrustedDomains` 列表
   - 允许内联 JavaScript，但避免执行不可信的代码

3. **提示词设计**
   - 根系统提示词：定义核心规范和格式要求
   - 用户全局提示词：定义样式偏好
   - 自定义提示词：针对具体文章的特殊要求

4. **多行文本处理**
   - JSON 中使用 `\\n` 表示换行
   - 保持一致的格式风格

5. **配置命名空间**
   - 使用 `Prompts` 根节点避免配置冲突
   - 与其他配置节点（`AI`、`Storage`）保持一致的命名风格

### ❌ 不推荐做法

1. **不要**在代码中硬编码提示词（除了内置默认值）
2. **不要**在生产环境使用 `permissive` 模式
3. **不要**频繁修改根系统提示词
4. **不要**重启应用程序来重新加载配置（利用热重载）
5. **不要**在 `prompts.json` 以外定义 `Prompts` 节点（避免冲突）

---

## 🐛 故障排查

### 问题 1：配置文件加载失败

**症状**：使用内置默认配置

**解决方案**：
1. 检查 `prompts.json` 文件是否存在于 `./data/config/` 目录
2. 检查 JSON 语法是否正确（使用在线 JSON 验证工具）
3. 检查文件编码是否为 UTF-8
4. 检查 JSON 结构是否包含 `Prompts` 根节点

### 问题 2：配置热重载不生效

**症状**：修改 `prompts.json` 后配置未更新

**解决方案**：
1. 确认 `Program.cs` 中 `reloadOnChange: true` 已启用
2. 检查文件是否成功保存
3. 触发一次新的 AI 调用（热重载是自动的）
4. 检查日志是否有文件监控错误

### 问题 3：配置节点找不到

**症状**：`PromptsConfig` 中的值为空或使用默认值

**解决方案**：
1. 检查 `prompts.json` 是否有 `Prompts` 根节点
2. 确认配置注册使用了 `GetSection("Prompts")`
3. 检查 JSON 结构与 `PromptsConfig` 类的属性是否匹配

### 问题 4：配置冲突

**症状**：配置被意外覆盖

**解决方案**：
1. 检查是否在多个配置文件中定义了 `Prompts` 节点
2. 理解配置优先级：`prompts.json` > `config.json` > `appsettings.{env}.json` > `appsettings.json`
3. 使用专属节点名称避免冲突

---

## 🚀 配置示例

### 开发环境配置（宽松）

```json
{
  "Prompts": {
    "MarkdownToHtml": {
      "Validation": {
        "Mode": "warning",
        "CheckExternalScripts": false
      }
    }
  }
}
```

### 生产环境配置（严格）

```json
{
  "Prompts": {
    "MarkdownToHtml": {
      "Validation": {
        "Mode": "strict",
        "CheckExternalScripts": true,
        "TrustedDomains": [
          "cdn.jsdelivr.net",
          "cdnjs.cloudflare.com"
        ]
      }
    }
  }
}
```

### 自定义样式配置

```json
{
  "Prompts": {
    "MarkdownToHtml": {
      "UserGlobalPrompt": "- 使用 Dracula 配色方案\\n- 添加代码行号\\n- 启用代码复制按钮\\n- 使用衬线字体作为正文"
    }
  }
}
```

---

## 📚 相关文档

- [Microsoft.Extensions.AI 文档](https://learn.microsoft.com/en-us/dotnet/ai/get-started/)
- [IOptionsMonitor 文档](https://learn.microsoft.com/en-us/dotnet/core/extensions/options#options-interfaces)
- [ASP.NET Core Configuration 文档](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [JSON 配置提供程序](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/#json-configuration-provider)
- [HtmlAgilityPack 文档](https://html-agility-pack.net/)
