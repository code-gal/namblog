using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NamBlog.API.Application.DTOs;
using NamBlog.API.Domain.Entities;
using NamBlog.API.Infrastructure.Common;
using NamBlog.API.Infrastructure.Services;

namespace NamBlog.API.Infrastructure.Persistence
{
    /// <summary>
    /// 数据库种子数据服务（DDD - Infrastructure Layer）
    /// 职责：创建种子 Markdown 文件 + 数据库记录，不生成 HTML（由 FileWatcher 或用户手动生成）
    /// </summary>
    public class DatabaseSeeder(
        BlogContext dbContext,
        ILogger<DatabaseSeeder> logger,
        IOptions<StorageSettings> storageSettings,
        IOptions<BlogInfo> blogInfo,
        IWebHostEnvironment env)
    {
        private readonly StorageSettings _storageSettings = storageSettings.Value;
        private readonly string _blogName = blogInfo.Value.BlogName ?? "NamBlog";
        private readonly string _blogger = blogInfo.Value.Blogger ?? "Ningal";

        /// <summary>
        /// 执行数据播种（仅在数据库为空时）
        /// </summary>
        public void SeedData()
        {
            if (dbContext.Posts.Any())
            {
                logger.LogInformation("数据库已包含数据，跳过种子数据插入");
                return;
            }

            logger.LogInformation("开始插入种子数据...");

            try
            {
                // 1. 确保默认配置文件存在（从 wwwroot/config 移动到 data/config）
                EnsureDefaultConfigsExist();

                // 2. 确保默认图标存在
                EnsureDefaultIconsExist();

                // 3. 创建种子文章（Markdown + 数据库记录，不生成 HTML）
                SeedPosts();

                logger.LogInformation("✅ 种子数据插入完成");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ 种子数据插入失败");
                throw;
            }
        }

        /// <summary>
        /// 插入示例文章数据（使用领域模型）
        /// </summary>
        private void SeedPosts()
        {
            logger.LogInformation("开始创建默认页面...");

            // 使用领域模型的工厂方法创建文章（不创建版本，避免循环依赖）
            var post1 = CreateAboutPost();
            var post2 = CreateDisclaimerPost();

            // 创建 Markdown 文件
            CreateMarkdownFiles([post1, post2]);

            // 第一步：先保存 Post（此时 MainVersionId 为 null）
            dbContext.Posts.AddRange(post1, post2);
            dbContext.SaveChanges();

            // 第二步：为每篇文章添加版本（此时 PostId 已生成）
            var version1 = post1.SubmitNewVersion("生成简洁现代的关于页面样式，清晰友好");
            var version2 = post2.SubmitNewVersion("生成专业的免责声明页面，结构清晰，分点说明");

            // 第三步：保存版本并更新 Post.MainVersionId
            dbContext.SaveChanges();

            // 第四步：现在可以安全发布了（已有版本）
            post1.Publish();  // 发布
            post2.Publish();  // 发布

            // 第五步：保存发布状态
            dbContext.SaveChanges();

            logger.LogInformation("✅ 已插入关于和免责声明页面到数据库（Markdown 文件已创建，HTML 将由 FileWatcher 或用户手动生成）");
        }

        /// <summary>
        /// 创建关于页面
        /// </summary>
        private Post CreateAboutPost()
        {
            // 1. 使用静态工厂方法创建文章
            var post = Post.CreateFromFileSystem(
                fileName: "关于本站",
                filePath: "pages",
                author: _blogger);

            // 2. 应用 AI 生成的元数据
            var tags = new[] {
                PostTag.Create("关于")
            };

            post.ApplyAiGeneratedMetadata(
                title: "关于本站",
                slug: "about",
                filename: "关于本站",
                excerpt: $"欢迎来到 {_blogName}！这是一个由 AI 智能体渲染 Markdown 文档成 HTML 的现代化博客系统。",
                tags: tags);

            return post;
        }

        /// <summary>
        /// 创建免责声明页面（已发布）
        /// </summary>
        private Post CreateDisclaimerPost()
        {
            var post = Post.CreateFromFileSystem(
                fileName: "免责声明",
                filePath: "pages",
                author: _blogger);

            var tags = new[] {
                PostTag.Create("免责声明")
            };

            post.ApplyAiGeneratedMetadata(
                title: "免责声明",
                slug: "disclaimer",
                filename: "免责声明",
                excerpt: "本博客的内容声明、版权声明、准确性声明和外部链接相关的免责声明。",
                tags: tags);

            return post;
        }

        /// <summary>
        /// 创建 Markdown 源文件
        /// </summary>
        private void CreateMarkdownFiles(Post[] posts)
        {
            foreach (var post in posts)
            {
                // 使用 FilePathHelper 构建正确的路径
                var relativePath = FilePathHelper.GetMarkdownRelativePath(post.FilePath, post.FileName);
                var fullPath = Path.Combine(_storageSettings.MarkdownPath, relativePath);

                // 确保目录存在
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (File.Exists(fullPath))
                {
                    logger.LogWarning("Markdown 文件已存在，跳过：{FilePath}", fullPath);
                    continue;
                }

                var content = GenerateMarkdownContent(post);
                File.WriteAllText(fullPath, content);
                logger.LogInformation("✅ 创建 Markdown 文件：{FilePath}", fullPath);
            }
        }

        /// <summary>
        /// 生成 Markdown 文件内容
        /// </summary>
        private string GenerateMarkdownContent(Post post)
        {
            var slug = post.Slug ?? string.Empty;

            return slug switch
            {
                "about" => @$"# 关于本站

欢迎来到 **{_blogName}**！

## 关于博客

这是一个由 AI 智能体渲染 Markdown 文档成 HTML 的现代化博客系统。

### 技术栈

本站使用以下技术构建：

- **前端**: Vue 3 + Tailwind CSS
- **后端**: ASP.NET Core + GraphQL
- **AI集成**: Model Context Protocol (MCP)
- **架构**: 领域驱动设计 (DDD)

### 核心功能

- ✨ Markdown 自动转换为精美 HTML
- 🎨 支持自定义 AI 提示词定制页面风格
- 📦 文章版本管理
- 🏷️ 分类和标签系统
- 🌓 深色模式支持
- 📱 响应式设计

## 关于作者

博主：**{_blogger}**

感谢您的访问！

---

如果您有任何问题或建议，欢迎通过页脚的链接联系我。
",

                "disclaimer" => @"# 免责声明

## 内容声明

本博客所有内容仅代表作者个人观点，不代表任何组织或机构的立场。文章内容仅供参考，读者应自行判断其准确性和适用性。

## 版权声明

本站原创内容版权归博主所有，转载请注明出处。引用的第三方内容版权归原作者所有。

如果您认为本站内容侵犯了您的权益，请及时联系我们，我们会在核实后及时处理。

## 准确性声明

本站力求内容准确，但不保证完整性和时效性。对于因使用本站内容而导致的任何损失，本站不承担责任。

技术文章中的代码和方案仅供学习参考，在生产环境使用前请充分测试。

## 外部链接

本站可能包含指向外部网站的链接，这些链接仅为方便读者而提供。本站不对外部网站的内容负责，也不代表本站认可这些网站的观点或立场。

访问外部链接的风险由您自行承担。

## AI 生成内容声明

本站部分内容（包括文章 HTML 页面）由 AI 辅助生成。虽然我们会尽力审核，但 AI 生成的内容可能存在不准确或不完整的情况。

我们建议读者对所有内容保持批判性思考。

## 数据收集

本站不使用 Cookie，不收集用户的个人信息。您的浏览行为完全私密。

## 免责声明的更新

我们保留随时修改本免责声明的权利。修改后的免责声明将在本页面发布，请定期查看。

---

**最后更新时间**: {DateTime.Now:yyyy年MM月dd日}
",

                _ => $"# {post.Title}\n\n这是一篇示例文章。\n\n## 内容\n\n编写你的内容..."
            };
        }

        /// <summary>
        /// 确保默认图标存在（从 wwwroot/images/icon/ 复制到 data/resources/icon/）
        /// </summary>
        private void EnsureDefaultIconsExist()
        {
            var targetDir = Path.Combine(_storageSettings.DataRootPath, "resources", "icon");

            // 如果目标目录已存在且包含文件，跳过复制
            if (Directory.Exists(targetDir) && Directory.GetFiles(targetDir).Length > 0)
            {
                logger.LogInformation("默认图标已存在，跳过复制");
                return;
            }

            // 创建目标目录
            Directory.CreateDirectory(targetDir);

            // 源目录：wwwroot/images/icon/
            var sourceDir = Path.Combine(env.WebRootPath, "images", "icon");

            if (!Directory.Exists(sourceDir))
            {
                logger.LogWarning("源图标目录不存在：{SourceDir}，跳过默认图标复制", sourceDir);
                return;
            }

            // 复制所有默认图标文件（跳过运行时生成的压缩文件）
            foreach (var sourceFile in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(sourceFile);

                // 跳过运行时生成的压缩文件（.br 和 .gz）
                if (fileName.EndsWith(".br", StringComparison.OrdinalIgnoreCase) ||
                    fileName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var targetFile = Path.Combine(targetDir, fileName);

                try
                {
                    File.Copy(sourceFile, targetFile, overwrite: false);
                    logger.LogInformation("✅ 复制默认图标：{FileName}", fileName);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "复制图标文件失败：{FileName}", fileName);
                }
            }
        }

        /// <summary>
        /// 确保默认配置文件存在（从 wwwroot/config 复制到 data/config）
        /// 逻辑：
        /// 1. 如果 data/config 中缺少文件，从 wwwroot 复制过去
        /// 2. 如果 data/config 中已有文件，删除 wwwroot 中对应的文件
        /// </summary>
        private void EnsureDefaultConfigsExist()
        {
            // 目标目录：data/config
            var targetDir = Path.Combine(_storageSettings.DataRootPath, "config");

            // 源目录：wwwroot/config
            var sourceDir = Path.Combine(env.WebRootPath, "config");

            if (!Directory.Exists(sourceDir))
            {
                logger.LogInformation("默认配置源目录不存在，跳过配置文件初始化");
                return;
            }

            // 确保目标目录存在
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // 处理所有配置文件（.json, .txt, .yaml, .yml）
            var configExtensions = new[] { "*.json", "*.txt", "*.yaml", "*.yml", "*.env", "*.ini" };
            foreach (var pattern in configExtensions)
            {
                foreach (var sourceFile in Directory.GetFiles(sourceDir, pattern))
                {
                    var fileName = Path.GetFileName(sourceFile);
                    var targetFile = Path.Combine(targetDir, fileName);

                    if (!File.Exists(targetFile))
                    {
                        // 目标不存在，复制文件
                        try
                        {
                            File.Copy(sourceFile, targetFile);
                            logger.LogInformation("✅ 复制默认配置到 data 目录：{FileName}", fileName);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "❌ 复制配置文件失败：{FileName}", fileName);
                        }
                    }
                }
            }
        }
    }
}
