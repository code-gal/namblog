# NamBlog 发布指南

## 发布步骤

1. **代码推送到 GitHub**（至少默认分支 `main` ），确保仓库里已经存在：
	- `.github/workflows/release.yml`
	- `.github/workflows/ci.yml`

2. **推送Tag**。
	- 只 push 分支时，只会触发 `CI`（`ci.yml`）
	- 只有 push 形如 `vX.Y.Z` 的 tag（例如 `v0.8.0`）才会触发 `Release and Build`（`release.yml`）

3. **确保 tag 指向的提交里包含 `release.yml`**。
	- GitHub Actions 运行时会使用“tag 指向的那次提交”里的 workflow 文件。

4. **仓库 Actions 权限设置**：
	- 进入 GitHub 仓库 → Settings → Actions → General → Workflow permissions
	- 建议选 **Read and write permissions**（否则即使 workflow 里写了 `contents: write` / `packages: write` 也可能受限）

5. **GHCR 镜像包可见性**：
	- 首次推送镜像成功后，Packages 里可能默认是 Private。
	- 如果你希望“用户无需登录即可拉取”，需要把该 Package 的 Visibility 改为 **Public**。

```bash
# 使用脚本（推荐）
.\release.ps1 -Version "0.8.0" -Message "Initial release"  # Windows
./release.sh 0.8.0 "Initial release"                       # Linux/Mac

# 或手动
git tag -a v0.8.0 -m "Release v0.8.0"
git push github v0.8.0
```

GitHub Actions 自动执行：创建 Release、构建 Docker 镜像（amd64/arm64）、推送到 GHCR。

## 常见问题：为什么只看到 CI 成功，但没有 Release 和镜像？

这通常意味着只 push 了分支提交（触发 `CI`），但**没有 push tag**（不会触发 `release.yml`）。

请检查：
- GitHub 仓库页面 → Tags 是否存在 `v0.8.0`（或你期望的 tag）
- Actions 页面是否出现名为 `Release and Build` 的工作流运行记录

如果 tag 存在但仍然没有 `Release and Build`：
- 确认该 tag 指向的提交包含 `.github/workflows/release.yml`
- 检查仓库 Settings → Actions → Workflow permissions 是否允许写入（Release / Packages 需要写权限）

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
git push github :refs/tags/v0.8.1

# 在 GitHub 删除 Release
```
