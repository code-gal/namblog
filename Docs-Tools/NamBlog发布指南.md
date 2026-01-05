# NamBlog 发布指南

## 前置准备

### Git 远程仓库配置
**重要**：发布脚本要求 GitHub 远程仓库名称必须是 `github`（不是默认的 `origin`）。

检查当前配置：
```bash
git remote -v
```

如果远程仓库名是 `origin`，需要重命名：
```bash
git remote rename origin github
```

或添加新的远程仓库：
```bash
git remote add github https://github.com/your-username/namblog.git
```

### 工作流说明

本项目有两个 GitHub Actions 工作流：

| 工作流 | 文件 | 触发条件 | 作用 |
|--------|------|---------|------|
| **CI** | `.github/workflows/ci.yml` | 推送代码到 `main`/`develop` 分支或创建 PR | ✅ 编译检查<br>✅ 构建测试 Docker 镜像<br>❌ 不发布 Release<br>❌ 不推送镜像到仓库 |
| **Release and Build** | `.github/workflows/release.yml` | 推送形如 `v*.*.*` 的 tag（如 `v0.8.2`） | ✅ 自动生成 Release Notes<br>✅ 创建 GitHub Release<br>✅ 构建多架构 Docker 镜像<br>✅ 推送到 GHCR |

**CI 工作流**：用于日常开发，确保代码质量，每次推送代码都会运行。  
**Release 工作流**：用于正式发布，只在推送版本标签时运行。

###  首次手动发布（仅首次需要）

如果是第一次设置发布流程，需要配置以下内容：

1. **确保仓库包含工作流文件**：
   - `.github/workflows/release.yml`
   - `.github/workflows/ci.yml`

2. **仓库 Actions 权限设置**：
   - 进入 GitHub 仓库  Settings  Actions  General  Workflow permissions
   - 选择 **Read and write permissions**（允许创建 Release 和推送 Packages）

3. **GHCR 镜像包可见性设置**：
   - 首次推送镜像成功后，进入仓库  Packages
   - 找到 `namblog` 镜像包，点击 Package settings
   - 将 Visibility 改为 **Public**（允许用户无需登录拉取镜像）

4. **创建首个 Release**：
   ```bash
   # 使用脚本（推荐）
   .\Docs-Tools\release.ps1 -Version "0.8.0" -Message "Initial release"
   
   # 或手动
   git tag -a v0.8.0 -m "Release v0.8.0"
   git push github v0.8.0
   ```

## 发布步骤

###  推荐方式：使用发布脚本（自动化）

**一键发布命令**：
```bash
# Windows PowerShell
.\Docs-Tools\release.ps1 -Version "0.8.3" -Message "Bug fixes and improvements"

# Linux/Mac Bash
./Docs-Tools/release.sh 0.8.3 "Bug fixes and improvements"
```

**脚本自动化功能**（一条命令完成以下所有步骤）：
1.  检查工作区状态（未提交更改会警告）
2.  检查当前分支（非主分支会警告）
3.  验证版本号格式（MAJOR.MINOR.PATCH）
4.  检查 tag 是否已存在（避免重复发布）
5.  自动更新 `NamBlog.API.csproj` 中的版本号
6.  提示确认 `CHANGELOG.md` 已更新
7.  提交版本更改到 Git
8.  推送代码到 GitHub
9.  创建并推送版本 tag（触发 Release 工作流）
10.  显示 GitHub Actions 和 Release 链接

**发布前准备**：
1. 在 `CHANGELOG.md` 中添加新版本的更新说明（中文）
2. 确保所有代码已提交（脚本会检查）

**Release Notes 生成策略**：
-  **中文内容**：从 `CHANGELOG.md` 自动提取当前版本的更新说明（手动维护，更详细）
-  **英文内容**：从 git commit 消息自动生成（Auto-generated，面向开发者）
-  **双语支持**：方便不同语言用户通过 Atom/RSS 订阅查看更新

推送 tag 后，GitHub Actions 自动执行：创建双语 Release、构建多架构 Docker 镜像（amd64/arm64）、推送到 GHCR。

---

###  手动发布（不使用脚本）

如果不使用脚本，需要手动执行以下步骤：

1. **更新 CHANGELOG.md**（添加新版本说明）
2. **更新 .csproj 版本号**（修改 `<Version>0.8.3</Version>`）
3. **提交并推送更改**：
   ```bash
   git add CHANGELOG.md NamBlog.API/NamBlog.API.csproj
   git commit -m "chore: bump version to 0.8.3"
   git push github main
   ```
4. **创建并推送 tag**：
   ```bash
   git tag -a v0.8.3 -m "Release v0.8.3"
   git push github v0.8.3
   ```

推送 tag 后，GitHub Actions 会自动触发发布流程。

---
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

## Docker 镜像使用建议

```yaml
# 生产环境（推荐）
image: ghcr.io/code-gal/namblog:stable

# 生产环境（保守）
image: ghcr.io/code-gal/namblog:0.8.0

# 测试环境
image: ghcr.io/code-gal/namblog:1

# 开发环境
image: ghcr.io/code-gal/namblog:latest
```

## 版本回滚

```bash
# 删除标签
git tag -d v0.8.1
git push github :refs/tags/v0.8.1

# 在 GitHub 删除 Release
```

