#!/bin/bash

# NamBlog 发布脚本
# 用法: ./release.sh <version> [message]
# 示例: ./release.sh 0.8.0 "Initial release"
# 示例: ./release.sh 0.9.0

set -e

# 切换到项目根目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
cd "$ROOT_DIR"

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 检查参数
if [ -z "$1" ]; then
    echo -e "${RED}错误: 请提供版本号${NC}"
    echo "用法: $0 <version> [message]"
    echo "示例: $0 0.8.0 \"Initial release\""
    exit 1
fi

VERSION=$1
MESSAGE=${2:-"Release v${VERSION}"}

# AssemblyVersion/FileVersion 不能包含 prerelease 后缀（例如 1.0.0-beta.1 无效）
BASE_VERSION="${VERSION%%-*}"

# 验证版本号格式 (简单验证)
if ! [[ $VERSION =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9.]+)?$ ]]; then
    echo -e "${RED}错误: 版本号格式不正确${NC}"
    echo "正确格式: MAJOR.MINOR.PATCH 或 MAJOR.MINOR.PATCH-prerelease"
    echo "示例: 0.8.0, 1.0.0, 1.0.0-beta.1"
    exit 1
fi

echo -e "${GREEN}=== NamBlog 发布脚本 ===${NC}"
echo ""
echo "版本号: v${VERSION}"
echo "消息: ${MESSAGE}"
echo ""

# 检查工作区状态
echo -e "${YELLOW}检查工作区状态...${NC}"
if ! git diff-index --quiet HEAD --; then
    echo -e "${RED}警告: 有未提交的更改${NC}"
    git status --short
    read -p "是否继续? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# 检查分支
CURRENT_BRANCH=$(git branch --show-current)
echo -e "${YELLOW}当前分支: ${CURRENT_BRANCH}${NC}"
if [ "$CURRENT_BRANCH" != "main" ] && [ "$CURRENT_BRANCH" != "master" ]; then
    echo -e "${YELLOW}警告: 不在主分支上${NC}"
    read -p "是否继续? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# 检查远程仓库
echo -e "${YELLOW}检查远程仓库...${NC}"
git fetch --tags

# 检查标签是否已存在
if git rev-parse "v${VERSION}" >/dev/null 2>&1; then
    echo -e "${RED}错误: 标签 v${VERSION} 已存在${NC}"
    exit 1
fi

# 更新 VERSION 文件
echo -e "${YELLOW}更新 VERSION 文件...${NC}"
echo "${VERSION}" > VERSION
git add VERSION

# 更新 .csproj 文件中的版本号
echo -e "${YELLOW}更新 .csproj 版本号...${NC}"
CSPROJ_PATH="NamBlog.API/NamBlog.API.csproj"
if [ -f "$CSPROJ_PATH" ]; then
    # 使用 sed 替换版本号（一次性处理，避免反复生成/覆盖备份文件）
    sed -i.bak \
        -e "s|<Version>[^<]*</Version>|<Version>${VERSION}</Version>|g" \
        -e "s|<AssemblyVersion>[^<]*</AssemblyVersion>|<AssemblyVersion>${BASE_VERSION}</AssemblyVersion>|g" \
        -e "s|<FileVersion>[^<]*</FileVersion>|<FileVersion>${BASE_VERSION}</FileVersion>|g" \
        -e "s|<InformationalVersion>[^<]*</InformationalVersion>|<InformationalVersion>${VERSION}</InformationalVersion>|g" \
        "$CSPROJ_PATH"
    rm -f "${CSPROJ_PATH}.bak"

    git add "$CSPROJ_PATH"
    echo -e "${GREEN}✓ .csproj 版本号已更新${NC}"
else
    echo -e "${YELLOW}⚠️  未找到 .csproj 文件${NC}"
fi

# 提示更新 CHANGELOG
echo -e "${YELLOW}请确认 CHANGELOG.md 已更新${NC}"
read -p "CHANGELOG.md 是否已更新? (y/N) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}请先更新 CHANGELOG.md，然后重新运行此脚本${NC}"
    exit 1
fi

# 提交更改
echo -e "${YELLOW}提交版本更新...${NC}"
git commit -m "chore: bump version to ${VERSION}" || true
git push origin $CURRENT_BRANCH

# 创建标签
echo -e "${YELLOW}创建标签 v${VERSION}...${NC}"
git tag -a "v${VERSION}" -m "${MESSAGE}"

# 推送标签
echo -e "${YELLOW}推送标签到远程仓库...${NC}"
git push origin "v${VERSION}"

echo ""
echo -e "${GREEN}✅ 发布成功!${NC}"
echo ""
echo "标签: v${VERSION}"
echo "GitHub Actions 将自动:"
echo "  1. 创建 GitHub Release"
echo "  2. 构建 Docker 镜像"
echo "  3. 推送到 GitHub Container Registry"
echo ""
echo "查看进度:"
echo "  Actions: https://github.com/$(git config --get remote.origin.url | sed 's/.*github.com[:/]\(.*\)\.git/\1/')/actions"
echo "  Releases: https://github.com/$(git config --get remote.origin.url | sed 's/.*github.com[:/]\(.*\)\.git/\1/')/releases"
echo ""
