using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NamBlog.API.Domain.Entities;
using NamBlog.API.Domain.Interfaces;
using NamBlog.API.Infrastructure.Common;
using NamBlog.API.Infrastructure.Persistence;

namespace NamBlog.API.Infrastructure.Services
{
    /// <summary>
    /// Markdown 文件监控服务
    /// 监控 markdown 目录变化，自动创建/删除文章
    /// 支持修复场景：数据库有文章但缺少 HTML 版本
    /// </summary>
    public class FileWatcherService(
        ILogger<FileWatcherService> logger,
        IOptions<StorageSettings> storageSettings,
        IOptions<FileWatcherSettings> fileWatcherSettings,
        IServiceProvider serviceProvider) : IHostedService, IDisposable
    {
        private readonly StorageSettings _storageSettings = storageSettings.Value;
        private readonly FileWatcherSettings _fileWatcherSettings = fileWatcherSettings.Value;
        private FileSystemWatcher? _watcher;
        private readonly ConcurrentDictionary<string, Timer> _debounceTimers = new();
        private const int _debounceMilliseconds = 5000; // 防抖延迟5秒

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("MD监控 - 启动服务 - 路径: {Path}", _storageSettings.MarkdownPath);

            var markdownPath = _storageSettings.MarkdownPath;

            if (!Directory.Exists(markdownPath))
            {
                Directory.CreateDirectory(markdownPath);
                logger.LogInformation("MD监控 - 创建MD目录: {Path}", markdownPath);
            }

            _ = Task.Run(async () => await SyncAllFilesAsync(), cancellationToken); //后台全量扫描

            _watcher = new FileSystemWatcher(markdownPath)
            {
                Filter = "*.md",
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName
            };

            _watcher.Created += OnFileCreated;
            _watcher.Deleted += OnFileDeleted;
            _watcher.Changed += OnFileChanged;
            _watcher.Renamed += OnFileRenamed;

            _watcher.EnableRaisingEvents = true; //启动监听目录和文件变化

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("MD监控 - 停止服务");

            if (_watcher is not null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }

            // 清理所有防抖定时器
            foreach (var timer in _debounceTimers.Values)
            {
                timer.Dispose();
            }

            _debounceTimers.Clear();

            return Task.CompletedTask;
        }

        #region 事件处理

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            logger.LogInformation("MD监控 - 文件创建: {Path}", e.FullPath);
            DebounceAction(e.FullPath, async () => await HandleFileCreatedAsync(e.FullPath));
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            logger.LogInformation("MD监控 - 文件删除: {Path}", e.FullPath);
            DebounceAction(e.FullPath, async () => await HandleFileDeletedAsync(e.FullPath));
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("MD监控 - 文件变更: {Path}", e.FullPath);
            }

            DebounceAction(e.FullPath, async () => await HandleFileChangedAsync(e.FullPath));
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            logger.LogInformation("MD监控 - 文件重命名: {OldPath} → {NewPath}", e.OldFullPath, e.FullPath);
            DebounceAction(e.FullPath, async () => await HandleFileRenamedAsync(e.OldFullPath, e.FullPath));
        }

        #endregion

        #region 防抖机制

        private void DebounceAction(string filePath, Func<Task> action)
        {
            // 取消之前的定时器
            if (_debounceTimers.TryRemove(filePath, out var oldTimer))
            {
                oldTimer.Dispose();
            }

            // 创建新的定时器
            var timer = new Timer(async _ =>
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "MD监控 - 定时器发生错误：{Path}", filePath);
                }
                finally
                {
                    // 清理定时器
                    if (_debounceTimers.TryRemove(filePath, out var t))
                    {
                        t.Dispose();
                    }
                }
            }, null, _debounceMilliseconds, Timeout.Infinite);

            _debounceTimers[filePath] = timer;
        }

        #endregion

        #region 文件事件处理逻辑

        /// <summary>
        /// 处理文件创建事件（使用 AI 生成元数据）
        /// 支持修复场景：数据库有文章但缺少 HTML 版本
        /// </summary>
        private async Task<bool> HandleFileCreatedAsync(string fullPath)
        {
            try
            {
                if (!File.Exists(fullPath))
                {
                    logger.LogWarning("MD监控 - 文件已不存在，跳过创建: {Path}", fullPath);
                    return false;
                }

                var (filePath, fileName) = FilePathHelper.GetRelativePathAndFileName(fullPath, _storageSettings.MarkdownPath);

                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<BlogContext>();
                var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();
                var aiService = scope.ServiceProvider.GetRequiredService<IAIService>();
                var tagRepository = scope.ServiceProvider.GetRequiredService<ITagRepository>();

                // 检查文章是否已存在（包含版本信息）
                var existingPost = await context.Posts
                    .Include(p => p.Versions)
                    .Include(p => p.Tags)
                    .AsSplitQuery() // 拆分为多个查询，避免笛卡尔积
                    .FirstOrDefaultAsync(p => p.FileName == fileName && p.FilePath == filePath);

                if (existingPost != null)
                {
                    // 检查是否有有效的 HTML 版本
                    var hasValidVersion = existingPost.Versions.Any(v => v.ValidationStatus == HtmlValidationStatus.Valid);

                    if (hasValidVersion)
                    {
                        // 进一步检查 HTML 文件是否真实存在
                        var validVersion = existingPost.Versions.First(v => v.ValidationStatus == HtmlValidationStatus.Valid);
                        var htmlRelativePath = FilePathHelper.GetHtmlRelativePath(filePath, fileName, validVersion.VersionName);
                        var htmlFullPath = Path.Combine(_storageSettings.HtmlPath, htmlRelativePath);

                        if (File.Exists(htmlFullPath))
                        {
                            logger.LogInformation("MD监控 - 文章已存在且有有效 HTML，跳过创建: {FileName}", fileName);
                            return false;
                        }

                        logger.LogWarning("MD监控 - 文章存在但 HTML 文件丢失，重新生成: {FileName}", fileName);
                    }
                    else
                    {
                        logger.LogWarning("MD监控 - 文章存在但无有效版本，开始生成 HTML: {FileName}", fileName);
                    }

                    // 为现有文章生成 HTML 版本
                    return await GenerateHtmlForExistingPostAsync(existingPost, fullPath, context, aiService, fileService);
                }

                logger.LogInformation("MD监控 - 使用 AI 生成文章元数据: {FileName}", fileName);

                // 1. 使用领域工厂方法创建文章
                var post = Post.CreateFromFileSystem(
                    fileName: fileName,
                    filePath: filePath,
                    author: "System");

                var markdown = await File.ReadAllTextAsync(fullPath);

                // 2. 使用 AI 生成标题
                var titleResult = await aiService.GenerateTitleAsync(markdown);
                if (!titleResult.IsSuccess)
                {
                    logger.LogError("MD监控 - AI 生成标题失败: {Error}", titleResult.ErrorMessage);
                    return false;
                }

                var title = titleResult.Value!;

                // 3. 使用 AI 生成 Slug
                var slugResult = await aiService.GenerateSlugAsync(title);
                if (!slugResult.IsSuccess)
                {
                    logger.LogError("MD监控 - AI 生成 Slug 失败: {Error}", slugResult.ErrorMessage);
                    return false;
                }

                var slug = slugResult.Value!;

                // 检查 Slug 是否重复，如重复则添加时间戳
                var slugExists = await context.Posts.AnyAsync(p => p.Slug == slug);
                if (slugExists)
                {
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    slug = $"{slug}-{timestamp}";
                    logger.LogWarning("MD监控 - Slug 已存在，添加时间戳: {Slug}", slug);
                }

                // 4. 使用 AI 生成标签
                var tagsResult = await aiService.GenerateTagsAsync(markdown);
                PostTag[] tags = [];

                if (tagsResult.IsSuccess && tagsResult.Value != null && tagsResult.Value.Length > 0)
                {
                    // 批量获取或创建标签
                    var tagEntities = await tagRepository.GetOrCreateTagsAsync(tagsResult.Value);
                    tags = [.. tagEntities];
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        var tagsString = string.Join(", ", tagsResult.Value);
                        logger.LogInformation("MD监控 - 创建-生成标签: {Tags}", tagsString);
                    }
                }
                else
                {
                    logger.LogWarning("MD监控 - 创建-AI 生成标签失败，使用空数组");
                }

                // 5. 使用 AI 生成摘要
                var excerptResult = await aiService.GenerateExcerptAsync(markdown);
                var excerpt = excerptResult.IsSuccess && !string.IsNullOrEmpty(excerptResult.Value)
                    ? excerptResult.Value
                    : (markdown.Length > 200 ? string.Concat(markdown.AsSpan(0, 200), "...") : markdown);

                // 6. 应用 AI 生成的元数据
                post.ApplyAiGeneratedMetadata(
                    title: title,
                    slug: slug,
                    filename: fileName,
                    excerpt: excerpt,
                    tags: tags);

                // 7. 生成 HTML
                var aiResult = await aiService.RenderMarkdownToHtmlAsync(markdown, null);
                if (!aiResult.IsSuccess)
                {
                    logger.LogError("MD监控 - 创建-AI生成HTML失败，跳过创建: {Error}", aiResult.ErrorMessage);
                    return false;
                }

                var html = aiResult.Value!;

                // 8. 第一次保存：保存Post（避免循环依赖）
                context.Posts.Add(post);
                await context.SaveChangesAsync();

                // 9. 创建第一个版本（此时Post.PostId已生成）
                var version = post.SubmitNewVersion(aiPrompt: null);
                version.MarkAsValid();

                // 10. 保存 HTML 文件
                await fileService.SaveHtmlAsync(filePath, fileName, version.VersionName, html);

                // 11. 第二次保存：先保存版本到数据库
                context.Posts.Update(post);
                await context.SaveChangesAsync();

                // 12. 根据配置决定是否自动发布（必须在版本保存后）
                if (_fileWatcherSettings.AutoPublish)
                {
                    post.Publish();
                    await context.SaveChangesAsync();
                    logger.LogInformation("MD监控 - 创建-文章已自动发布（配置项 AutoPublish=true）");
                }

                logger.LogInformation(
                    "MD监控 - 创建-自动创建文章成功 - 标题: {Title}, Slug: {Slug}, PostId: {PostId}, 已发布: {Published}",
                    post.Title, post.Slug, post.PostId, post.IsPublished
                );
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MD监控 - 创建-处理文件创建事件失败: {Path}", fullPath);
                return false;
            }
        }

        /// <summary>
        /// 为已存在的文章生成 HTML 版本（修复场景：数据库有记录但无 HTML）
        /// </summary>
        private async Task<bool> GenerateHtmlForExistingPostAsync(
            Post post,
            string markdownFullPath,
            BlogContext context,
            IAIService aiService,
            IFileService fileService)
        {
            try
            {
                var markdown = await File.ReadAllTextAsync(markdownFullPath);

                // 生成 HTML
                var aiResult = await aiService.RenderMarkdownToHtmlAsync(markdown, null);
                if (!aiResult.IsSuccess)
                {
                    logger.LogError("MD监控 - 修复-AI生成HTML失败: {Error}", aiResult.ErrorMessage);
                    return false;
                }

                var html = aiResult.Value!;

                // 获取或创建版本
                PostVersion version;
                if (post.Versions.Count <= 0)
                {
                    // 如果没有任何版本，创建新版本
                    version = post.SubmitNewVersion(aiPrompt: null);
                    logger.LogInformation("MD监控 - 修复-创建新版本: {Title}", post.Title);
                }
                else
                {
                    // 使用现有的第一个版本
                    version = post.Versions.First();
                    logger.LogInformation("MD监控 - 修复-使用现有版本: {Title}, Version: {Version}", post.Title, version.VersionName);
                }

                // 标记为有效
                version.MarkAsValid();

                // 保存 HTML 文件
                await fileService.SaveHtmlAsync(post.FilePath, post.FileName, version.VersionName, html);

                // 保存数据库更改
                await context.SaveChangesAsync();

                logger.LogInformation("MD监控 - 修复-成功生成 HTML 版本: {Title}, Version: {Version}", post.Title, version.VersionName);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MD监控 - 修复-生成 HTML 失败: {Title}", post.Title);
                return false;
            }
        }

        /// <summary>
        /// 处理文件删除事件（使用领域模型）
        /// </summary>
        private async Task<bool> HandleFileDeletedAsync(string fullPath)
        {
            try
            {
                var (filePath, fileName) = FilePathHelper.GetRelativePathAndFileName(fullPath, _storageSettings.MarkdownPath);

                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<BlogContext>();
                var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();

                // 查找文章（包含版本信息）
                var post = await context.Posts
                    .Include(p => p.Versions)
                    .FirstOrDefaultAsync(p => p.FileName == fileName && p.FilePath == filePath);

                if (post is null)
                {
                    logger.LogWarning("MD监控 - 删除-未找到对应文章，跳过: {FileName}", fileName);
                    return false;
                }

                // EF Core 配置了级联删除，删除 Post 会自动删除关联的 Versions
                context.Posts.Remove(post);
                await context.SaveChangesAsync();

                // 删除所有文件（Markdown 和 HTML）
                await fileService.DeleteAllArticleFilesAsync(filePath, fileName);

                logger.LogInformation("MD监控 - 删除-自动删除文章成功 - 标题: {Title}, Slug: {Slug}", post.Title, post.Slug);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MD监控 - 删除-处理文件删除事件失败: {Path}", fullPath);
                return false;
            }
        }

        /// <summary>
        /// 处理文件变更事件（仅更新 Markdown 文件）
        /// </summary>
        private async Task HandleFileChangedAsync(string fullPath)
        {
            try
            {
                if (!File.Exists(fullPath))
                {
                    logger.LogWarning("MD监控 - 更变-文件已不存在，跳过更新: {Path}", fullPath);
                    return;
                }

                var (filePath, fileName) = FilePathHelper.GetRelativePathAndFileName(fullPath, _storageSettings.MarkdownPath);
                var markdown = await File.ReadAllTextAsync(fullPath);

                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<BlogContext>();
                var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();

                var post = await context.Posts.FirstOrDefaultAsync(p => p.FileName == fileName && p.FilePath == filePath);

                if (post is null)
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("MD监控 - 更变-未找到文章，尝试创建: {FileName}", fileName);
                    }

                    await HandleFileCreatedAsync(fullPath);
                    return;
                }

                // 保存 Markdown 文件（覆盖旧内容）
                await fileService.SaveMarkdownAsync(filePath, fileName, markdown);

                // 注意：不直接修改 post.LastModified，因为 Post 实体所有属性是 private set
                // 如果需要更新时间戳，应该在领域层添加业务方法
                // 这里只是更新 Markdown 文件，不触发数据库变更

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("MD监控 - 更变-自动更新 Markdown - 标题: {Title}", post.Title);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MD监控 - 更变-处理文件变更事件失败: {Path}", fullPath);
            }
        }

        /// <summary>
        /// 处理文件重命名/移动事件（使用领域方法 RenameFile）
        /// </summary>
        private async Task HandleFileRenamedAsync(string oldFullPath, string newFullPath)
        {
            try
            {
                if (!File.Exists(newFullPath))
                {
                    logger.LogWarning("MD监控 - 移动-新文件不存在，跳过: {Path}", newFullPath);
                    return;
                }

                var markdownPath = _storageSettings.MarkdownPath;
                var (oldFilePath, oldFileName) = FilePathHelper.GetRelativePathAndFileName(oldFullPath, markdownPath);
                var (newFilePath, newFileName) = FilePathHelper.GetRelativePathAndFileName(newFullPath, markdownPath);

                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<BlogContext>();

                var post = await context.Posts
                    .Include(p => p.Versions)
                    .FirstOrDefaultAsync(p => p.FileName == oldFileName && p.FilePath == oldFilePath);

                if (post is null)
                {
                    logger.LogWarning("MD监控 - 移动-未找到旧文件对应文章，尝试创建新文章: {OldFileName} → {NewFileName}", oldFileName, newFileName);
                    await HandleFileCreatedAsync(newFullPath);
                    return;
                }

                // 使用领域方法重命名文件
                post.RenameFile(newFileName, newFilePath);

                // 移动 HTML 目录
                if (oldFilePath != newFilePath || oldFileName != newFileName)
                {
                    var oldHtmlDir = string.IsNullOrEmpty(oldFilePath)
                        ? Path.Combine(_storageSettings.HtmlPath, oldFileName)
                        : Path.Combine(_storageSettings.HtmlPath, oldFilePath, oldFileName);

                    var newHtmlDir = string.IsNullOrEmpty(newFilePath)
                        ? Path.Combine(_storageSettings.HtmlPath, newFileName)
                        : Path.Combine(_storageSettings.HtmlPath, newFilePath, newFileName);

                    if (Directory.Exists(oldHtmlDir))
                    {
                        var newParentDir = Path.GetDirectoryName(newHtmlDir);
                        if (!string.IsNullOrEmpty(newParentDir) && !Directory.Exists(newParentDir))
                        {
                            Directory.CreateDirectory(newParentDir);
                        }

                        Directory.Move(oldHtmlDir, newHtmlDir);
                        logger.LogInformation("MD监控 - 移动-移动 HTML 目录: {OldDir} → {NewDir}", oldHtmlDir, newHtmlDir);
                    }
                }

                await context.SaveChangesAsync();

                logger.LogInformation(
                    "MD监控 - 移动-文件重命名成功 - 标题: {Title}, {OldPath}/{OldName} → {NewPath}/{NewName}",
                    post.Title, oldFilePath, oldFileName, newFilePath, newFileName
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "处理文件重命名/移动事件失败: {OldPath} → {NewPath}", oldFullPath, newFullPath);
            }
        }

        #endregion

        #region 全量扫描

        /// <summary>
        /// 启动时全量扫描，同步文件系统和数据库
        /// </summary>
        private async Task SyncAllFilesAsync()
        {
            try
            {
                logger.LogInformation("MD监控 - 扫描-开始全量扫描Markdown文件...");

                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<BlogContext>();

                var markdownPath = _storageSettings.MarkdownPath;
                var allMdFiles = Directory.GetFiles(markdownPath, "*.md", SearchOption.AllDirectories);
                var postsInDb = await context.Posts
                    .Include(p => p.Versions)
                    .ToListAsync();

                int createdCount = 0;
                int deletedCount = 0;
                int repairedCount = 0;

                // 创建新文章或修复缺失 HTML
                foreach (var mdFile in allMdFiles)
                {
                    var (filePath, fileName) = FilePathHelper.GetRelativePathAndFileName(mdFile, markdownPath);
                    if (filePath.Contains(".."))
                    {
                        logger.LogError("MD监控 - 扫描-非法路径: {FilePath}", filePath);
                        continue;
                    }

                    var existingPost = postsInDb.FirstOrDefault(p => p.FileName == fileName && p.FilePath == filePath);

                    if (existingPost == null)
                    {
                        // 文章不存在，创建新文章
                        var success = await HandleFileCreatedAsync(mdFile);
                        if (success)
                            createdCount++;
                        await Task.Delay(500);
                    }
                    else
                    {
                        // 文章存在，检查是否需要修复 HTML
                        var hasValidVersion = existingPost.Versions.Any(v => v.ValidationStatus == HtmlValidationStatus.Valid);
                        if (!hasValidVersion)
                        {
                            logger.LogWarning("MD监控 - 扫描-发现无效版本，尝试修复: {FileName}", fileName);
                            var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();
                            var aiService = scope.ServiceProvider.GetRequiredService<IAIService>();

                            var success = await GenerateHtmlForExistingPostAsync(existingPost, mdFile, context, aiService, fileService);
                            if (success)
                                repairedCount++;
                            await Task.Delay(500);
                        }
                    }
                }

                // 删除不存在的文章
                foreach (var post in postsInDb)
                {
                    var fullPath = Path.Combine(markdownPath, post.FilePath, $"{post.FileName}.md");
                    if (!File.Exists(fullPath))
                    {
                        var success = await HandleFileDeletedAsync(fullPath);
                        if (success)
                            deletedCount++;
                    }
                }

                logger.LogInformation(
                    "MD监控 - 扫描-全量扫描完成 - 发现 {Total} 个文件, 创建 {Created} 篇, 修复 {Repaired} 篇, 删除 {Deleted} 篇",
                    allMdFiles.Length, createdCount, repairedCount, deletedCount
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MD监控 - 扫描-全量扫描失败");
            }
        }

        #endregion

        public void Dispose()
        {
            _watcher?.Dispose();

            foreach (var timer in _debounceTimers.Values)
            {
                timer.Dispose();
            }

            _debounceTimers.Clear();

            GC.SuppressFinalize(this);
        }
    }
}
