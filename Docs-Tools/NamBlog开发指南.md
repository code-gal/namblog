# NamBlog 开发文档

## 目录

- [快速开始](#快速开始)
- [技术架构](#技术架构)
- [项目结构](#项目结构)
- [开发指引](#开发指引)
- [贡献指南](#贡献指南)

---

## 快速开始

### 环境要求

- .NET 10 SDK
- Git
- 代码编辑器（推荐 VS Code 或 Visual Studio）
- Docker（可选，用于容器化开发）

### 本地运行

```bash
# 克隆仓库
git clone https://github.com/yourusername/NamBlog.git
cd NamBlog/NamBlog.API

# 配置 AI API Key
# 编辑 appsettings.Development.json，添加你的 OpenAI API Key

# 启动
dotnet restore
dotnet run
```

访问：
- http://localhost:5000/graphiql
- https://localhost:5001/ui/graphiql
- https://localhost:5001/ui/altair
- https://localhost:5001/ui/voyager

默认管理员账号：`admin` / `admin123`

### 前端配置说明

前端可以复制到后端wwwroot目录，或者独立运行。

#### 开发模式配置

采用**配置覆盖模式**（类似后端 `appsettings.Development.json`）：

**本地开发（Live Server）**：

```bash
# 1. 复制示例配置
cp NamBlog.Web/config.local.example.js NamBlog.Web/config.local.js

# 2. （可选）修改 config.local.js 自定义配置
# 3. 启动 Live Server
```

`config.local.js` 会覆盖 `index.html` 中的默认配置，且已在 `.gitignore` 中排除，不会提交到仓库。

**生产部署**：
- 无需任何配置文件
- 直接使用 `index.html` 中的默认配置（`DEV_MODE: false`）
- 零配置，自动适配生产环境

#### API 地址配置

支持两种方式配置后端地址：
1. **本地开发**：在 `config.local.js` 中设置 `API_BASE_URL`
2. **生产环境**：自动使用当前域名（`window.location.origin`）

---

## 技术架构

### 后端：DDD 分层架构

```
┌─────────────────────────────────────────┐
│    Presentation Layer (表示层)           │  GraphQL/MCP 接口
├─────────────────────────────────────────┤
│    Application Layer (应用层)            │  业务逻辑编排
├─────────────────────────────────────────┤
│    Infrastructure Layer (基础设施层)      │  数据库、文件、AI
├─────────────────────────────────────────┤
│    Domain Layer (领域层)                 │  核心实体模型
└─────────────────────────────────────────┘
```

**技术栈**：

- .NET 10.0 + ASP.NET Core
- GraphQL.NET（API 接口）
- Entity Framework Core（ORM）
- SQLite / PostgreSQL（数据库）
- OpenAI API（内容生成）
- MCP SDK（AI 工具集成）

### 前端：Vue 3 单页应用

**技术栈**：

- Vue 3 (Composition API)
- Tailwind CSS
- Vue Router
- EasyMDE (Markdown 编辑器)

---

## 项目结构

### 后端目录

```
NamBlog.API/
├── Domain/              # 核心实体
│   └── Entities/
├── Application/         # 业务逻辑
│   └── Services/
├── Infrastructure/      # 技术实现
│   ├── Persistence/     # 数据库
│   └── Services/        # 文件、AI、监控
├── EntryPoint/          # 对外接口
│   ├── GraphiQL/        # GraphQL API
│   └── MCP/             # MCP 协议
├── Extensions/          # 服务注册
├── Migrations/          # 数据库迁移
├── wwwroot/             # 前端静态文件
└── data/                # 运行时数据
    ├── articles/
    ├── resources/
    └── config/
```

### 前端目录

```
NamBlog.Web/
├── index.html
├── css/
│   └── style.css
└── js/
    ├── main.js          # 入口
    ├── App.js           # 根组件
    ├── store.js         # 状态管理
    ├── api/             # API 封装
    ├── components/      # 可复用组件
    └── views/           # 页面组件
```

---

## 开发指引

### 添加新功能

#### 1. 添加实体


```bash
# 在 Domain/Entities 目录创建实体类
# 例如：Comment.cs

# 在 BlogContext 中注册
# Infrastructure/Persistence/BlogContext.cs
public DbSet<Comment> Comments { get; set; }

# 生成迁移
dotnet ef migrations add AddCommentEntity --project NamBlog.API

# 应用迁移
dotnet ef database update --project NamBlog.API
```

#### 2. 添加 GraphQL API

```csharp
// 创建 Type 定义（EntryPoint/GraphiQL/Types/）
// 添加 Query 或 Mutation（EntryPoint/GraphiQL/Queries/ 或 Mutations/）
// 调用 Application 层的服务
```

#### 3. 添加配置项

```json
// appsettings.json
{
  "YourFeature": {
    "Enabled": true
  }
}
```

```csharp
// Infrastructure/Settings/YourFeatureSettings.cs
// Extensions/ConfigurationExtensions.cs 中注册配置
```

### 数据库迁移常用命令

```bash
# 查看迁移列表
dotnet ef migrations list --project NamBlog.API

# 添加迁移
dotnet ef migrations add MigrationName --project NamBlog.API

# 应用迁移
dotnet ef database update --project NamBlog.API

# 回滚迁移
dotnet ef database update PreviousMigrationName --project NamBlog.API

# 删除最后一个迁移（未应用时）
dotnet ef migrations remove --project NamBlog.API
```

### 注意事项

修改 `Domain/Entities` 中的实体类后，**务必**生成迁移，具体见：[数据库迁移指南](数据库迁移指南.md)

### 前端开发

添加新页面：

```javascript
// 1. 创建 js/views/YourPage.js
// 2. 在 js/main.js 中注册路由
// 3. 调用 API（参考 js/api/ 目录的现有代码）
```

---

## 贡献指南

### 代码规范

**C#**：
- 遵循 [Microsoft C# 编码约定](https://docs.microsoft.com/zh-cn/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- 使用 PascalCase 命名类、方法、属性
- 使用 camelCase 命名局部变量、参数
- 为 public 成员添加 XML 注释

**JavaScript**：
- 使用 ES6+ 语法
- 使用 `const` 和 `let`，避免 `var`
- 使用箭头函数
- 添加 JSDoc 注释

### Git 提交规范

遵循 [Conventional Commits](https://www.conventionalcommits.org/)：

- `feat`: 新功能
- `fix`: Bug 修复
- `docs`: 文档更新
- `style`: 代码格式（不影响功能）
- `refactor`: 重构
- `test`: 测试相关
- `chore`: 构建/工具链相关

示例：
```bash
git commit -m "feat: add comment system"
git commit -m "fix: resolve login token expiration issue"
git commit -m "docs: update installation guide"
```

### Pull Request 流程

1. Fork 仓库
2. 创建功能分支：`git checkout -b feature/your-feature`
3. 开发和测试
4. 提交代码（遵循提交规范）
5. 推送到 GitHub
6. 创建 Pull Request

### 提交前检查

- [ ] 代码通过编译，无错误和警告
- [ ] 添加了必要的注释
- [ ] 更新了相关文档
- [ ] 提交信息符合规范
- [ ] （如有实体变更）生成了数据库迁移

---

## 相关文档

- [后端架构设计](Docs-Tools/后端架构设计说明.md)
- [API 接口规范](Docs-Tools/后端API接口规范文档.md)
- [配置系统](Docs-Tools/配置系统说明.md)
- [数据库迁移](Docs-Tools/数据库迁移指南.md)
- [Docker 部署](DOCKER_DEPLOYMENT.md)

---

## 常见问题

**Q: 如何修改管理员密码？**

A: 使用 [Bcrypt 在线工具](https://bcrypt-generator.com/) 生成哈希，修改配置文件 `data/config/config.json` 的 `Admin.PasswordHash`，重启应用。

**Q: 为什么修改配置后没有生效？**

A: 部分配置需要重启应用。`Blog`、`AI`、`Cors`、`Logging` 支持热重载，其他配置（`Jwt`、`MCP`、`Admin`）需要重启。

**Q: GraphQL Schema 在哪里查看？**

A: 访问 http://localhost:5000/ui/voyager 可视化查看，或参考 [API 接口规范](Docs-Tools/后端API接口规范文档.md)。

**Q: 数据库迁移失败怎么办？**

A: 参考 [数据库迁移指南](Docs-Tools/数据库迁移指南.md) 的故障排查章节。

