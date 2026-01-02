# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- i18n

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

[Unreleased]: https://github.com/code-gal/namblog/compare/v0.8.0...HEAD
[0.8.0]: https://github.com/code-gal/namblog/releases/tag/v0.8.0
