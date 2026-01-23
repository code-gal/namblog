# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- Skills

## [0.10.1] - 2026-01-23

### Fixed
- 🐛 登录令牌过期时清理登录状态并跳转到登录页
- 🐛 桌面端列表页侧边栏滚动条出现时左右间距不对称的问题

## [0.10.0] - 2026-01-23

### Added
- ✨ 实现文章分享功能
- ✨ 添加管理员登录速率限制（基于 IP 地址的内存限流）
- ✨ 实现 SitemapService，自动生成 sitemap.xml 和 robots.txt
- 📝 添加 SEO 配置文档（中英双语）

### Fixed
- 🐛 修复 MCP 工具更新文章元数据时必须提供 markdown 内容的问题（现在仅创建新文章时需要 markdown，更新元数据不再需要）
- 🐛 禁用文件变更监控以防止冗余 I/O 操作
- 🐛 改进 SEO 爬虫支持和静态 HTML 服务可靠性
  - SEO 中间件路径重写逻辑与 FilePathHelper 保持一致
  - 添加已发布文章和有效 HTML 版本的严格验证
  - 爬虫 User-Agent 列表移至 appsettings.json 以提升可维护性
  - 增强 SEO 路径解析的日志记录和缓存机制

### Changed
- 🔧 速率限制配置项添加到 appsettings.json

## [0.9.0] - 2026-01-16

### Changed
- 📝 更新英文文档图片展示方式，提升文档可读性

### Fixed
- 🐛 修复移动端编辑器清除草稿或删除版本后工具栏消失的问题
- 🐛 修复提示词 UI 的若干问题

### PWA
- ♻️ 刷新缓存资源并在删除文章版本时清理对应缓存
- ♻️ /resources/* 路径采用 stale-while-revalidate 策略，支持后台更新
- ♻️ 删除文章版本时自动清理 /posts/* 缓存
- ⬆️ 发布时自动提升 Service Worker CACHE_VERSION，避免 UI 旧缓存
- 🔄 每次访问检测 SW 更新，移除定时轮询

## [0.8.11] - 2026-01-11

### Fixed
- 🐛 修复分页按钮需要双击才生效的问题，并避免并发点击导致的竞态
- 🐛 修复保存后“版本切换”状态不持久化的问题
- 🐛 修复移动端 HTML 面板缺失“生成 HTML”按钮的问题
- 🐛 修复编辑页分类列表在移动端无法正确隐藏/收起的问题

### Changed
- ♻️ 移动端编辑器分类输入：用自定义下拉菜单替换 datalist，提升可用性与兼容性
- ♻️ HTML 文章渲染：通过 iframe `srcdoc` 渲染完整 HTML，并移除 sandbox 限制以改善下载等能力
- 🔧 优化 AI 生成 HTML 的提示词：增强 iframe 兼容性与标签输出质量

## [0.8.10] - 2026-01-11

### Fixed
- 🐛 修复文章 iframe 沙箱权限不足导致下载按钮、弹窗、外部链接等功能无法使用
  - 添加 `allow-downloads` 支持文件下载
  - 添加 `allow-popups-to-escape-sandbox` 支持外部链接打开
  - 添加 `allow-top-navigation-by-user-activation` 支持用户触发的页面导航
  - 添加 `allow-pointer-lock` 支持 3D 游戏和绘图工具
  - 添加 `allow-presentation` 支持演示 API
  - 添加 `allow-orientation-lock` 支持移动设备横屏游戏
  - 添加 `allow-top-navigation-to-custom-protocols` 支持 mailto:、tel: 等自定义协议链接

## [0.8.9] - 2026-01-11

### Added
- ✨ HTML 全屏编辑功能：在全屏预览模式下可直接编辑 HTML 代码
  - 新增编辑/关闭编辑切换按钮（仅在全屏时可见）
  - 支持 iframe 预览和 textarea 编辑模式无缝切换
  - HTML 为空时自动进入编辑模式
  - 编辑内容实时双向绑定，自动保存到状态

### Fixed
- 🐛 修复全屏预览按钮在 HTML 为空时不显示的问题
- 🐛 修复编辑器操作按钮并发问题：执行保存/生成/删除等操作时，其他修改按钮现在会被禁用

### Changed
- ♻️ 优化全屏预览刷新按钮：移除旋转动画，静默刷新 iframe 内容

## [0.8.8] - 2026-01-11

### Fixed
- 🐛 修复编辑器清除草稿功能因 `updateShadowContent` 未定义导致的错误

## [0.8.7] - 2026-01-11

### Fixed
- 🐛 修复文章页面 Vue 应用挂载冲突
- 🐛 修复页面切换后定时器和动画循环泄漏
- 🐛 修复 HTTPS 环境下 Trusted Types 阻止脚本执行

### Changed
- ♻️ 重构文章渲染：完整 HTML 文档使用 iframe 隔离
- ♻️ 移除冗余代码：定时器劫持、脚本执行函数
- ✨ 支持深色模式在 iframe 中自动同步

## [0.8.6] - 2026-01-11

### Fixed
- 🐛 修复文章页面在 SPA 导航时的脚本重复声明错误（自动转换 const/let/class 为可重复声明形式）
- 🐛 修复生产环境刷新文章页面时资源路径 404 错误（静态 base 标签）
- 🔧 优化开发环境配置（添加 .vscode/settings.json 配置 Live Server 路径映射）

### Changed
- 📝 更新开发指南文档，补充 Live Server 使用说明和常见问题解答

## [0.8.5] - 2026-01-11

### Added
- ✨ 实现 i18n 国际化支持（中英双语）
  - 后端本地化基础设施，使用资源文件
  - 前端使用 vue-i18n，支持浏览器语言自动检测
  - 核心 UI 组件翻译（导航栏、登录、文章等）
  - 创建英文文档（README.md 和配置指南）
  - 所有文档添加双语交叉引用
  - 支持 localStorage 语言偏好持久化
- ✨ 支持可配置的隐藏分类功能
  - 在 config.js 中添加 HIDDEN_CATEGORIES 常量集中管理分类过滤
  - 从导航栏、文章导航面板和编辑器分类列表中过滤配置的分类（默认：'pages'）
  - 隐藏分类的文章仍可通过主页、标签页和直接链接访问
  - 适用于"关于"或"隐私政策"等不应出现在分类导航中的特殊页面

### Changed
- 🔧 AI 生成超时时间现在可通过 TimeoutSeconds 配置项自定义

### Fixed
- 🐛 修复 SPA 路由刷新时的资源 404 错误
  - 优化 index.html 中脚本加载顺序，确保在加载其他资源前设置 `<base href="/">`
  - 生产环境：添加 base 标签以正确解析刷新 /article/* 路由时的相对路径
  - 开发环境：config.local.js 设置 DEV_MODE=true，跳过 base 标签
- 🐛 统一环境检测并优化 base 标签插入
  - 使用 DOM API 替换 document.write() 以消除浏览器警告
  - 使用 APP_CONFIG.DEV_MODE 代替基于 IP 的检测来选择路由模式
  - 修复本地测试环境（127.0.0.1）的路由模式问题
- 🐛 防止 FileWatcherService 覆盖用户创建的文章 HTML
  - 在 ArticleCommandService 保存 HTML 后调用 MarkAsValid()（3 处）
  - 增强 FileWatcherService 扫描逻辑，使用双重验证（ValidationStatus + 文件存在性检查）
  - 确保所有文章版本的 ValidationStatus 正确设置为 Valid
  - 修复 HandleFileCreatedAsync 中 HTML 文件路径构造，添加 index.html
- 🐛 改进 Article.js 动态资源管理
  - 为动态添加的脚本/样式添加清理机制
  - 用 IIFE 包装内联脚本以防止变量重复声明错误
  - 离开或加载新文章时清理资源

## [0.8.4] - 2026-01-05

### Fixed
- 🐛 修复文章提交时因精选状态设置时机错误导致的"post has no versions"异常
- 🐛 修正 SaveArticleAsync 和 SubmitArticleAsync 中精选/发布状态的业务流程顺序
- 🐛 修正 FileWatcherService 中自动发布功能的执行顺序
- 🔧 统一所有场景下的版本创建和状态设置流程：创建文章 → 创建版本 → 设置状态 → 持久化

### Technical
- 确保所有领域规则得到正确执行（Feature/Publish 必须在版本创建后调用）
- 消除可能导致前端收到 HTML 错误页面而非 JSON 响应的异常场景

## [0.8.3] - 2026-01-05

### Changed
- 🔧 改进发布工作流，GitHub Release 现在同时显示中文和英文说明
- 🔧 修复发布脚本使用正确的 Git 远程仓库名称（`github` 而非 `origin`）
- 🔧 移除 VERSION 文件，版本信息统一由 Git tags 和 .csproj 管理
- 📝 重构发布指南文档，详细说明自动化脚本的 10 项功能
- 🔧 恢复 .csproj 中的完整版本号配置（AssemblyVersion、FileVersion、InformationalVersion）

### Technical
- Release Notes 现在从 CHANGELOG.md 提取中文内容，从 git commit 生成英文内容
- 发布脚本自动化：检查状态、更新版本号、提交推送、创建标签
- 双语 Release Notes 支持 Atom/RSS 订阅

## [0.8.2] - 2026-01-05

### Added
- ✨ 支持在页脚注入网站统计脚本（如百度统计、Google Analytics 等）

### Fixed
- 🐛 改进 AI 提示助手的用户体验，统一编辑器滚动条样式
- 🐛 修复移动端 HTML 面板在删除确认时过早关闭的问题
- 🐛 支持前端独立开发模式（配置覆盖模式和 Live Server 支持）
- 🐛 优化版本和分类输入框的内边距，防止文本被截断
- 🐛 防止意外触发文章侧边栏
- 🐛 改进移动端侧边栏可访问性，解决手势冲突问题
- 🐛 统一版本选择器宽度与分类输入框，优化移动端布局一致性
- 🐛 使用后端动态值替换硬编码的博客标题
- 🐛 增加 AI 服务超时时间，防止生成长文章时失败

## [0.8.0] - 2026-01-03

### Added
- ✨ 初始发布版本
- 🔐 完整的用户认证系统（JWT）
- 📝 基于 GraphQL 的 API 接口
- 🤖 AI 辅助写作功能（支持 MCP 协议）
- 📊 文章管理（Markdown/HTML 双格式支持）
- 🏷️ 分类和标签系统
- 🔍 全文搜索功能
- 📱 PWA 支持
- 🐳 Docker 容器化部署
- 🔄 配置热重载
- 💾 支持 SQLite 和 PostgreSQL 数据库
- 🎨 响应式前端设计

### Technical
- .NET 10.0 后端
- GraphQL.NET 8.x
- Entity Framework Core 10.x
- 原生 JavaScript 前端（无框架依赖）
- Docker 多架构支持（amd64/arm64）

---

## 版本说明

### 版本号规范
- **MAJOR.MINOR.PATCH** (例如：1.2.3)
- **MAJOR**: 不兼容的 API 变更
- **MINOR**: 向下兼容的新增功能
- **PATCH**: 向下兼容的 Bug 修复

### 变更类型
- `Added` - 新功能
- `Changed` - 现有功能的变更
- `Deprecated` - 即将废弃的功能
- `Removed` - 已删除的功能
- `Fixed` - Bug 修复
- `Security` - 安全性改进

[Unreleased]: https://github.com/code-gal/namblog/compare/v0.10.1...HEAD
[0.10.1]: https://github.com/code-gal/namblog/releases/tag/v0.10.1
[0.10.0]: https://github.com/code-gal/namblog/releases/tag/v0.10.0
[0.9.0]: https://github.com/code-gal/namblog/releases/tag/v0.9.0
[0.8.11]: https://github.com/code-gal/namblog/releases/tag/v0.8.11
[0.8.10]: https://github.com/code-gal/namblog/releases/tag/v0.8.10
[0.8.9]: https://github.com/code-gal/namblog/releases/tag/v0.8.9
[0.8.8]: https://github.com/code-gal/namblog/releases/tag/v0.8.8
[0.8.7]: https://github.com/code-gal/namblog/releases/tag/v0.8.7
[0.8.6]: https://github.com/code-gal/namblog/releases/tag/v0.8.6
[0.8.5]: https://github.com/code-gal/namblog/releases/tag/v0.8.5
[0.8.4]: https://github.com/code-gal/namblog/releases/tag/v0.8.4
[0.8.3]: https://github.com/code-gal/namblog/releases/tag/v0.8.3
[0.8.2]: https://github.com/code-gal/namblog/releases/tag/v0.8.2
[0.8.0]: https://github.com/code-gal/namblog/releases/tag/v0.8.0
