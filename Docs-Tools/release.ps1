# NamBlog 发布脚本 (Windows PowerShell 版本)
# 用法: .\release.ps1 -Version "0.8.0" [-Message "Initial release"]
# 示例: .\release.ps1 -Version "0.8.0"
# 示例: .\release.ps1 -Version "0.9.0" -Message "New features and bug fixes"

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,

    [Parameter(Mandatory=$false)]
    [string]$Message = ""
)

# 设置错误时停止
$ErrorActionPreference = "Stop"

# 切换到项目根目录
$ScriptDir = Split-Path -Parent $PSCommandPath
$RootDir = Split-Path -Parent $ScriptDir
Set-Location $RootDir

# 如果未提供消息，使用默认消息
if ([string]::IsNullOrWhiteSpace($Message)) {
    $Message = "Release v$Version"
}

# AssemblyVersion/FileVersion 不能包含 prerelease 后缀（例如 1.0.0-beta.1 无效）
$BaseVersion = ($Version -split "-", 2)[0]

# 验证版本号格式
if ($Version -notmatch '^\d+\.\d+\.\d+(-[a-zA-Z0-9.]+)?$') {
    Write-Host "❌ 错误: 版本号格式不正确" -ForegroundColor Red
    Write-Host "正确格式: MAJOR.MINOR.PATCH 或 MAJOR.MINOR.PATCH-prerelease"
    Write-Host "示例: 0.8.0, 1.0.0, 1.0.0-beta.1"
    exit 1
}

Write-Host "=== NamBlog 发布脚本 ===" -ForegroundColor Green
Write-Host ""
Write-Host "版本号: v$Version"
Write-Host "消息: $Message"
Write-Host ""

# 检查工作区状态
Write-Host "检查工作区状态..." -ForegroundColor Yellow
$status = git status --porcelain
if ($status) {
    Write-Host "⚠️  警告: 有未提交的更改" -ForegroundColor Red
    git status --short
    $continue = Read-Host "是否继续? (y/N)"
    if ($continue -ne "y" -and $continue -ne "Y") {
        exit 1
    }
}

# 检查分支
$currentBranch = git branch --show-current
Write-Host "当前分支: $currentBranch" -ForegroundColor Yellow
if ($currentBranch -ne "main" -and $currentBranch -ne "master") {
    Write-Host "⚠️  警告: 不在主分支上" -ForegroundColor Yellow
    $continue = Read-Host "是否继续? (y/N)"
    if ($continue -ne "y" -and $continue -ne "Y") {
        exit 1
    }
}

# 检查远程仓库
Write-Host "检查远程仓库..." -ForegroundColor Yellow
git fetch --tags

# 检查标签是否已存在
$tagExists = git tag -l "v$Version"
if ($tagExists) {
    Write-Host "❌ 错误: 标签 v$Version 已存在" -ForegroundColor Red
    exit 1
}

# 更新 .csproj 文件中的版本号
Write-Host "更新 .csproj 版本号..." -ForegroundColor Yellow
$csprojPath = "NamBlog.API\NamBlog.API.csproj"
if (Test-Path $csprojPath) {
    $csprojContent = Get-Content $csprojPath -Raw

    # 替换各种版本标签
    $csprojContent = $csprojContent -replace '<Version>[\d\.]+(-[a-zA-Z0-9.]+)?</Version>', "<Version>$Version</Version>"
    $csprojContent = $csprojContent -replace '<AssemblyVersion>[\d\.]+(-[a-zA-Z0-9.]+)?</AssemblyVersion>', "<AssemblyVersion>$BaseVersion</AssemblyVersion>"
    $csprojContent = $csprojContent -replace '<FileVersion>[\d\.]+(-[a-zA-Z0-9.]+)?</FileVersion>', "<FileVersion>$BaseVersion</FileVersion>"
    $csprojContent = $csprojContent -replace '<InformationalVersion>[\d\.]+(-[a-zA-Z0-9.]+)?</InformationalVersion>', "<InformationalVersion>$Version</InformationalVersion>"

    Set-Content -Path $csprojPath -Value $csprojContent
    git add $csprojPath
    Write-Host "✓ .csproj 版本号已更新" -ForegroundColor Green
} else {
    Write-Host "⚠️  未找到 .csproj 文件" -ForegroundColor Yellow
}

# 更新 Service Worker 缓存版本号（用于前端缓存桶版本化）
Write-Host "更新 Service Worker CACHE_VERSION..." -ForegroundColor Yellow
$swPath = "NamBlog.Web\sw.js"
if (Test-Path $swPath) {
    $swContent = Get-Content $swPath -Raw

    # 更新 CACHE_VERSION 常量
    $swContent = $swContent -replace "const\s+CACHE_VERSION\s*=\s*'[^']*';", "const CACHE_VERSION = '$Version';"

    Set-Content -Path $swPath -Value $swContent
    git add $swPath
    Write-Host "✓ Service Worker CACHE_VERSION 已更新" -ForegroundColor Green
} else {
    Write-Host "⚠️  未找到 Service Worker 文件: $swPath" -ForegroundColor Yellow
}

# 提示更新 CHANGELOG
Write-Host "请确认 CHANGELOG.md 已更新" -ForegroundColor Yellow
$changelogUpdated = Read-Host "CHANGELOG.md 是否已更新? (y/N)"
if ($changelogUpdated -ne "y" -and $changelogUpdated -ne "Y") {
    Write-Host "请先更新 CHANGELOG.md，然后重新运行此脚本" -ForegroundColor Yellow
    exit 1
}

# 提交更改
Write-Host "提交版本更新..." -ForegroundColor Yellow
try {
    git commit -m "chore: bump version to $Version"
    git push github $currentBranch
} catch {
    Write-Host "⚠️  提交可能已存在，继续..." -ForegroundColor Yellow
}

# 创建标签
Write-Host "创建标签 v$Version..." -ForegroundColor Yellow
git tag -a "v$Version" -m $Message

# 推送标签
Write-Host "推送标签到远程仓库..." -ForegroundColor Yellow
git push github "v$Version"

Write-Host ""
Write-Host "✅ 发布成功!" -ForegroundColor Green
Write-Host ""
Write-Host "标签: v$Version"
Write-Host "GitHub Actions 将自动:"
Write-Host "  1. 创建 GitHub Release"
Write-Host "  2. 构建 Docker 镜像"
Write-Host "  3. 推送到 GitHub Container Registry"
Write-Host ""

# 获取仓库 URL
$remoteUrl = git config --get remote.origin.url
$repoPath = ""
if ($remoteUrl -match 'github\.com[:/](.+?)(\.git)?$') {
    $repoPath = $matches[1] -replace '\.git$', ''
}

if ($repoPath) {
    Write-Host "查看进度:"
    Write-Host "  Actions: https://github.com/$repoPath/actions"
    Write-Host "  Releases: https://github.com/$repoPath/releases"
    Write-Host ""
}
