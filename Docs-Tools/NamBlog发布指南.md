# NamBlog 发布指南

## 发布步骤

```bash
# 使用脚本（推荐）
.\release.ps1 -Version "0.8.0" -Message "Initial release"  # Windows
./release.sh 0.8.0 "Initial release"                       # Linux/Mac

# 或手动
git tag -a v0.8.0 -m "Release v0.8.0"
git push origin v0.8.0
```

GitHub Actions 自动执行：创建 Release、构建 Docker 镜像（amd64/arm64）、推送到 GHCR。

## Docker 标签策略

| 版本 | 标签 | 说明 |
|-----|------|------|
| v0.8.0 | `0.8.0`, `0.8`, `latest`, `stable` | 次版本 |
| v0.8.1 | `0.8.1`, `0.8`, `latest` | 补丁 |
| v1.0.0 | `1.0.0`, `1.0`, `1`, `latest`, `stable` | 主版本 |
| v1.0.1 | `1.0.1`, `1.0`, `1`, `latest` | 补丁 |

**标签说明**：
- `stable` - 只有 x.y.0 才打，生产环境推荐
- `latest` - 每次都打，包含补丁
- `:1` - 追踪 1.x.x 所有版本
- `0.8` - 追踪 0.8.x 所有版本
- `0.8.0` - 精确版本，永不变

## 使用建议

```yaml
# 生产（推荐）
image: ghcr.io/code-gal/namblog:stable

# 生产（保守）
image: ghcr.io/code-gal/namblog:0.8.0

# 测试
image: ghcr.io/code-gal/namblog:1

# 开发
image: ghcr.io/code-gal/namblog:latest
```

## 语义化版本

**MAJOR.MINOR.PATCH**
- MAJOR: 不兼容的 API 变更
- MINOR: 新功能（兼容）
- PATCH: Bug 修复

## 版本回滚

```bash
# 删除标签
git tag -d v0.8.1
git push origin :refs/tags/v0.8.1

# 在 GitHub 删除 Release
```
