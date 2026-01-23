using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NamBlog.API.Application.Common;
using NamBlog.API.Application.DTOs;
using NamBlog.API.Application.Resources;
using NamBlog.API.Domain.Entities;
using NamBlog.API.Domain.Interfaces;

namespace NamBlog.API.Application.Services
{
    /// <summary>
    /// 文章服务实现
    /// </summary>
    public partial class ArticleCommandService(
        IPostRepository postRepository,
        ITagRepository tagRepository,
        IFileService fileService,
        IAIService aiService,
        ValidationService validationService,
        MetadataProcessor metadataProcessor,
        ArticleQueryService queryService,
        IOptionsMonitor<BlogInfo> blogInfo,
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        IStringLocalizer<SharedResource> localizer,
        ILogger<ArticleCommandService> logger)
    {
        private readonly IPostRepository _postRepository = postRepository;
        private readonly ITagRepository _tagRepository = tagRepository;
        private readonly IFileService _fileService = fileService;
        private readonly IAIService _aiService = aiService;
        private readonly ValidationService _validationService = validationService;
        private readonly MetadataProcessor _metadataProcessor = metadataProcessor;
        private readonly ArticleQueryService _queryService = queryService;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMemoryCache _cache = cache;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;
        private readonly ILogger<ArticleCommandService> _logger = logger;
        private readonly string _blogName = blogInfo.CurrentValue.BlogName ?? "Admin";

        /// <summary>
        /// 保存文章（Save按钮，支持创建和更新）
        /// 创建时生成HTML版本（Markdown必填），更新时只保存元数据（Markdown可选）
        /// </summary>
        public async Task<Result<ArticleMetadataDto>> SaveArticleAsync(SaveArticleCommand command)
        {
            // 场景A：创建新文章 (id = null)
            if (command.Id == null)
            {
                // 1. 验证 Markdown（创建时必填，ValidateMarkdown 会检查空值）
                var markdownValidation = _validationService.ValidateMarkdown(command.Markdown ?? string.Empty);
                if (!markdownValidation.IsSuccess)
                    return Result.Failure<ArticleMetadataDto>(markdownValidation.ErrorMessage!, markdownValidation.ErrorCode);

                _logger.LogInformation("创建文章 - Markdown长度: {Length}", command.Markdown!.Length);

                // 2. 处理元数据（验证 + AI 生成）
                var metadataResult = await _metadataProcessor.ProcessMetadataAsync(
                    command.Markdown,
                    command.Title,
                    command.Slug,
                    command.Category,
                    command.Tags,
                    command.Excerpt);

                if (!metadataResult.IsSuccess)
                    return Result.Failure<ArticleMetadataDto>(metadataResult.ErrorMessage!, metadataResult.ErrorCode);

                var metadata = metadataResult.Value!;

                // 3. 生成 HTML
                var htmlResult = await _aiService.RenderMarkdownToHtmlAsync(command.Markdown, command.CustomPrompt);
                if (!htmlResult.IsSuccess)
                {
                    _logger.LogError("AI生成HTML失败 - Slug: {Slug}, 错误: {Error}", metadata.Slug, htmlResult.ErrorMessage);
                    return Result.Failure<ArticleMetadataDto>(
                        htmlResult.ErrorMessage ?? _localizer["AiHtmlGenerationFailed"].Value,
                        htmlResult.ErrorCode ?? ErrorCodes.ExternalServiceError);
                }

                // 4. 使用领域模型创建文章
                var post = Post.CreateFromUserInput(
                    fileName: metadata.Slug,
                    author: _blogName,
                    category: metadata.Category);

                // 5. 获取或创建标签
                var postTags = await _tagRepository.GetOrCreateTagsAsync(metadata.Tags);

                // 6. 应用元数据
                post.ApplyAiGeneratedMetadata(
                    title: metadata.Title,
                    slug: metadata.Slug,
                    filename: metadata.Slug,
                    excerpt: metadata.Excerpt,
                    tags: postTags);

                // 7. 保存 Markdown 文件（新文件直接保存）
                await _fileService.SaveMarkdownAsync("", metadata.Slug, command.Markdown);

                // 8. 持久化文章（获取 PostId）
                await _postRepository.AddAsync(post);
                await _unitOfWork.SaveChangesAsync();

                // 9. 创建版本
                var version = post.SubmitNewVersion(aiPrompt: command.CustomPrompt);

                // 10. 保存 HTML 文件
                await _fileService.SaveHtmlAsync("", metadata.Slug, version.VersionName, htmlResult.Value!);

                // 10.5. AI生成的HTML已经在OpenAIService中验证过，直接标记为有效
                version.MarkAsValid();

                // 11. 设置发布状态（必须在创建版本之后）
                if (command.IsPublished == true)
                {
                    post.Publish(version);
                }

                // 12. 设置精选状态（必须在创建版本之后）
                if (command.IsFeatured == true)
                {
                    post.Feature();
                }

                // 13. 保存版本和发布状态
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("文章创建成功（Save按钮） - 标题: {Title}, Slug: {Slug}, PostId: {PostId}",
                    metadata.Title, metadata.Slug, post.PostId);

                // 清除 SEO 缓存（新文章）
                InvalidateSeoCache(metadata.Slug);

                // 14. 返回完整的编辑数据
                var result = await _queryService.GetArticleForEditAsync(post.PostId);
                return Result.Success(result!);
            }

            // 场景B：更新现有文章 (id = 123)
            else
            {
                _logger.LogInformation("更新文章 - Id: {Id}", command.Id);

                // 1. 查询文章
                var post = await _postRepository.GetByIdAsync(command.Id.Value);
                if (post == null)
                {
                    _logger.LogWarning("更新文章失败 - 文章不存在: {Id}", command.Id);
                    return Result.Failure<ArticleMetadataDto>(_localizer["ArticleNotFoundWithId", command.Id].Value, ErrorCodes.NotFound);
                }

                // 记录旧 slug（用于缓存清除）
                var oldSlug = post.Slug;

                // 2. 如果提供了 Markdown，则验证并更新
                if (!string.IsNullOrWhiteSpace(command.Markdown))
                {
                    var markdownValidation = _validationService.ValidateMarkdown(command.Markdown);
                    if (!markdownValidation.IsSuccess)
                        return Result.Failure<ArticleMetadataDto>(markdownValidation.ErrorMessage!, markdownValidation.ErrorCode);

                    var existingMarkdown = await _fileService.ReadMarkdownAsync(post.FilePath, post.FileName);
                    if (existingMarkdown != command.Markdown)
                    {
                        await _fileService.SaveMarkdownAsync(post.FilePath, post.FileName, command.Markdown);
                        _logger.LogInformation("Markdown文件已更新 - Id: {Id}, Slug: {Slug}", command.Id, post.Slug);
                    }
                }

                // 3. 处理元数据（如果提供）
                if (command.Title != null || command.Category != null || command.Tags != null || command.Excerpt != null)
                {
                    // 获取或创建标签
                    IEnumerable<PostTag>? postTags = null;
                    if (command.Tags != null)
                    {
                        postTags = await _tagRepository.GetOrCreateTagsAsync(command.Tags);
                    }

                    // 更新元数据（只更新提供的字段）
                    post.UpdateMetadata(
                        title: command.Title,
                        slug: command.Slug,
                        category: command.Category,
                        tags: postTags,
                        excerpt: command.Excerpt);

                    _logger.LogInformation("文章元数据已更新 - Id: {Id}, Slug: {Slug}", command.Id, post.Slug);
                }

                // 4. 更新精选状态
                if (command.IsFeatured.HasValue)
                {
                    if (command.IsFeatured.Value)
                        post.Feature();
                    else
                        post.Unfeature();
                }

                // 5. 更新发布状态
                if (command.IsPublished.HasValue)
                {
                    if (command.IsPublished.Value)
                        post.Publish();
                    else
                        post.Unpublish();
                }

                // 6. 切换主版本（如果提供）
                if (!string.IsNullOrEmpty(command.MainVersion))
                {
                    var version = post.Versions.FirstOrDefault(v => v.VersionName == command.MainVersion);
                    if (version != null)
                    {
                        post.SwitchPublishedVersion(version);
                        // _logger.LogDebug("主版本已切换 - Id: {Id}, 新版本: {VersionName}", command.Id, command.MainVersion);
                    }
                    else
                    {
                        _logger.LogWarning("切换主版本失败 - 版本不存在: {VersionName}", command.MainVersion);
                    }
                }

                // 7. 持久化（不生成新版本）
                _postRepository.Update(post);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("文章更新成功（Save按钮） - Id: {Id}, Slug: {Slug} (未生成HTML版本)",
                    command.Id, post.Slug);

                // 清除 SEO 缓存（如果 slug 改变或主版本改变）
                if (command.Slug != null && oldSlug != post.Slug)
                {
                    // slug 改变：清除旧 slug 和新 slug 的缓存
                    if (oldSlug != null)
                        InvalidateSeoCache(oldSlug);
                    InvalidateSeoCache(post.Slug!);
                    _logger.LogDebug("Slug 已改变，清除双份缓存: {OldSlug} -> {NewSlug}", oldSlug, post.Slug);
                }
                else if (!string.IsNullOrEmpty(command.MainVersion) && post.Slug != null)
                {
                    // 主版本改变：清除缓存
                    InvalidateSeoCache(post.Slug);
                    _logger.LogDebug("主版本已切换，清除缓存: {Slug}", post.Slug);
                }

                // 9. 返回完整的编辑数据
                var result = await _queryService.GetArticleForEditAsync(post.PostId);
                return Result.Success(result!);
            }
        }

        /// <summary>
        /// 提交文章（Submit按钮，生成HTML并创建版本）
        /// 说明：id为null表示创建新文章并生成首个版本，否则为现有文章创建新版本
        /// 支持用户提供HTML或AI自动生成HTML
        /// </summary>
        public async Task<Result<ArticleVersionSubmitDto>> SubmitArticleAsync(SubmitArticleCommand command)
        {
            // 1. 验证 Markdown
            var markdownValidation = _validationService.ValidateMarkdown(command.Markdown);
            if (!markdownValidation.IsSuccess)
                return Result.Failure<ArticleVersionSubmitDto>(markdownValidation.ErrorMessage!, markdownValidation.ErrorCode);

            // 场景A：创建新文章并生成版本 (id = null)
            if (command.Id == null)
            {
                _logger.LogInformation("创建文章并提交版本 - Markdown长度: {Length}", command.Markdown.Length);

                // 2. 处理元数据（验证 + AI 生成）
                var metadataResult = await _metadataProcessor.ProcessMetadataAsync(
                    command.Markdown,
                    command.Title,
                    command.Slug,
                    command.Category,
                    command.Tags,
                    command.Excerpt);

                if (!metadataResult.IsSuccess)
                    return Result.Failure<ArticleVersionSubmitDto>(metadataResult.ErrorMessage!, metadataResult.ErrorCode);

                var metadata = metadataResult.Value!;

                // 3. 处理 HTML（用户提供或 AI 生成）
                string htmlContent;
                if (!string.IsNullOrWhiteSpace(command.Html))
                {
                    // 用户提供了 HTML（前端已验证，直接使用）
                    htmlContent = command.Html;
                    // _logger.LogDebug("使用用户提供的HTML - Slug: {Slug}", metadata.Slug);
                }
                else
                {
                    // AI 生成 HTML（已在OpenAIService中验证）
                    var htmlResult = await _aiService.RenderMarkdownToHtmlAsync(command.Markdown, command.CustomPrompt);
                    if (!htmlResult.IsSuccess)
                    {
                        _logger.LogError("AI生成HTML失败 - Slug: {Slug}, 错误: {Error}", metadata.Slug, htmlResult.ErrorMessage);
                        return Result.Failure<ArticleVersionSubmitDto>(
                            htmlResult.ErrorMessage ?? _localizer["AiHtmlGenerationFailed"].Value,
                            htmlResult.ErrorCode ?? ErrorCodes.ExternalServiceError);
                    }

                    htmlContent = htmlResult.Value!;
                    // _logger.LogDebug("AI生成HTML成功 - Slug: {Slug}", metadata.Slug);
                }

                // 4. 使用领域模型创建文章
                var post = Post.CreateFromUserInput(
                    fileName: metadata.Slug,
                    author: _blogName,
                    category: metadata.Category);

                // 5. 获取或创建标签
                var postTags = await _tagRepository.GetOrCreateTagsAsync(metadata.Tags);

                // 6. 应用元数据
                post.ApplyAiGeneratedMetadata(
                    title: metadata.Title,
                    slug: metadata.Slug,
                    filename: metadata.Slug,
                    excerpt: metadata.Excerpt,
                    tags: postTags);

                // 7. 保存Markdown文件
                await _fileService.SaveMarkdownAsync(post.FilePath, post.FileName, command.Markdown);

                // 8. 第一次持久化：保存Post（不包含版本，避免循环依赖）
                await _postRepository.AddAsync(post);
                await _unitOfWork.SaveChangesAsync();

                // 9. 创建版本（此时Post.PostId已生成）
                var version = post.SubmitNewVersion(aiPrompt: command.CustomPrompt);

                // 10. 保存HTML文件
                await _fileService.SaveHtmlAsync(post.FilePath, post.FileName, version.VersionName, htmlContent);

                // 10.5. 标记验证状态（AI生成的已验证，用户提供的也已验证）
                version.MarkAsValid();

                // 11. 应用发布状态
                if (command.IsPublished.GetValueOrDefault())
                    post.Publish(version);

                // 12. 应用精选状态（必须在版本创建之后）
                if (command.IsFeatured.GetValueOrDefault())
                    post.Feature();

                // 13. 第二次持久化：保存版本和MainVersion关联
                _postRepository.Update(post);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("文章创建并提交成功（Submit按钮） - Slug: {Slug}, 版本: {Version}",
                    post.Slug, version.VersionName);

                // 清除 SEO 缓存
                InvalidateSeoCache(post.Slug!);

                // 12. 返回 Slug（前端用于跳转）
                return Result.Success(new ArticleVersionSubmitDto(post.Slug!));
            }
            // 场景B：为现有文章创建新版本 (id = 123)
            else
            {
                // 2. 根据 ID 查询文章
                var post = await _postRepository.GetByIdAsync(command.Id.Value);
                if (post == null)
                {
                    _logger.LogWarning("为文章创建版本失败 - 文章不存在: {Id}", command.Id);
                    return Result.Failure<ArticleVersionSubmitDto>(_localizer["ArticleNotFoundWithId", command.Id].Value, ErrorCodes.NotFound);
                }

                // 3. 比较并更新 Markdown 文件
                var existingMarkdown = await _fileService.ReadMarkdownAsync(post.FilePath, post.FileName);
                if (existingMarkdown != command.Markdown)
                {
                    await _fileService.SaveMarkdownAsync(post.FilePath, post.FileName, command.Markdown);
                    _logger.LogInformation("Markdown文件已更新 - Id: {Id}, Slug: {Slug}", command.Id, post.Slug);
                }

                // 4. 处理 HTML（用户提供或 AI 生成）
                string htmlContent;
                if (!string.IsNullOrWhiteSpace(command.Html))
                {
                    // 用户提供了 HTML（前端已验证，直接使用）
                    htmlContent = command.Html;
                    // _logger.LogDebug("使用用户提供的HTML - Id: {Id}, Slug: {Slug}", command.Id, post.Slug);
                }
                else
                {
                    // AI 生成 HTML（已在OpenAIService中验证）
                    var htmlResult = await _aiService.RenderMarkdownToHtmlAsync(command.Markdown, command.CustomPrompt);
                    if (!htmlResult.IsSuccess)
                    {
                        _logger.LogError("AI生成HTML失败 - Id: {Id}, Slug: {Slug}, 错误: {Error}",
                            command.Id, post.Slug, htmlResult.ErrorMessage);
                        return Result.Failure<ArticleVersionSubmitDto>(
                            htmlResult.ErrorMessage ?? _localizer["AiHtmlGenerationFailed"].Value,
                            htmlResult.ErrorCode ?? ErrorCodes.ExternalServiceError);
                    }

                    htmlContent = htmlResult.Value!;
                    // _logger.LogDebug("AI生成HTML成功 - Id: {Id}, Slug: {Slug}", command.Id, post.Slug);
                }

                // 5. 处理元数据更新（如果提供）
                if (command.Title != null || command.Category != null || command.Tags != null || command.Excerpt != null)
                {
                    // 获取或创建标签
                    IEnumerable<PostTag>? postTags = null;
                    if (command.Tags != null)
                    {
                        postTags = await _tagRepository.GetOrCreateTagsAsync(command.Tags);
                    }

                    // 更新元数据（只更新提供的字段）
                    post.UpdateMetadata(
                        title: command.Title,
                        slug: command.Slug,
                        category: command.Category,
                        tags: postTags,
                        excerpt: command.Excerpt);

                    _logger.LogInformation("文章元数据已更新 - Id: {Id}, Slug: {Slug}", command.Id, post.Slug);
                }

                // 6. 创建新版本
                var version = post.SubmitNewVersion(aiPrompt: command.CustomPrompt);

                // 7. 保存 HTML
                await _fileService.SaveHtmlAsync(post.FilePath, post.FileName, version.VersionName, htmlContent);

                // 7.5. 标记验证状态（AI生成的已验证，用户提供的也已验证）
                version.MarkAsValid();

                // 8. 更新发布状态
                if (command.IsPublished.HasValue)
                {
                    if (command.IsPublished.Value)
                        post.Publish(version);
                    else
                        post.Unpublish();
                }
                // 9. 更新精选状态（必须在版本创建之后）
                if (command.IsFeatured.HasValue)
                {
                    if (command.IsFeatured.Value)
                        post.Feature();
                    else
                        post.Unfeature();
                }
                // 10. 持久化
                _postRepository.Update(post);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("文章版本创建成功（Submit按钮） - Id: {Id}, Slug: {Slug}, 版本: {Version}",
                    command.Id, post.Slug, version.VersionName);

                // 清除 SEO 缓存
                InvalidateSeoCache(post.Slug!);

                // 11. 返回 Slug（前端用于跳转）
                return Result.Success(new ArticleVersionSubmitDto(post.Slug!));
            }
        }

        /// <summary>
        /// 删除指定版本
        /// </summary>
        public async Task<Result> DeleteVersionAsync(int id, string versionName)
        {
            var post = await _postRepository.GetByIdAsync(id);
            if (post == null)
            {
                return Result.Failure(_localizer["ArticleNotFound"].Value, ErrorCodes.NotFound);
            }

            // 查找版本
            var version = post.Versions.FirstOrDefault(v => v.VersionName == versionName);
            if (version == null)
            {
                return Result.Failure(_localizer["VersionNotFound", versionName].Value, ErrorCodes.NotFound);
            }

            // 检查是否为最后一个版本
            if (post.Versions.Count == 1)
            {
                // 删除整篇文章前，先打破循环引用（Post.MainVersionId <-> PostVersion.PostId）
                // 使用领域方法将MainVersion置空，避免EF Core循环依赖错误
                post.PrepareForDeletion();
                _postRepository.Update(post);
                await _unitOfWork.SaveChangesAsync();

                // 删除整篇文章（现在可以安全删除，会级联删除所有版本）
                // 删除所有文件（markdown + 所有HTML）
                try
                {
                    await _fileService.DeleteAllArticleFilesAsync(post.FilePath, post.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("删除最后版本时文件删除失败 - ID: {PostId}, 错误: {Error}", id, ex.Message);
                }

                _logger.LogInformation("删除最后版本，文章已删除 - ID: {PostId}, Slug: {Slug}", id, post.Slug);
                return Result.Success();
            }

            // 删除单个版本
            try
            {
                post.RemoveVersion(version);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(ex.Message, ErrorCodes.InvalidOperation);
            }

            _postRepository.Update(post);
            await _unitOfWork.SaveChangesAsync();

            // 删除HTML版本目录
            try
            {
                await _fileService.DeleteHtmlDirectoryAsync(post.FilePath, post.FileName, versionName);
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("删除版本文件时文件不存在 - ID: {PostId}, 版本: {VersionName}", id, versionName);
                // 文件不存在也认为删除成功
            }

            _logger.LogInformation("版本删除成功 - ID: {PostId}, 版本: {VersionName}", id, versionName);

            return Result.Success();
        }

        /// <summary>
        /// 发布/取消发布文章
        /// </summary>
        public async Task<Result<ArticleDetailDto>> TogglePublishAsync(int id)
        {
            var post = await _postRepository.GetByIdAsync(id);
            if (post == null)
                return Result.Failure<ArticleDetailDto>(_localizer["ArticleNotFoundWithId", id].Value, ErrorCodes.NotFound);

            // 使用领域方法切换发布状态
            if (post.IsPublished)
            {
                post.Unpublish();
            }
            else
            {
                // 检查是否有版本
                if (post.Versions.Count == 0)
                    return Result.Failure<ArticleDetailDto>(_localizer["CannotPublishNoVersion"].Value, ErrorCodes.InvalidOperation);

                post.Publish();
            }

            _postRepository.Update(post);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("文章发布状态切换成功：ID={Id}, IsPublished={IsPublished}", id, post.IsPublished);

            return Result.Success(post.Adapt<ArticleDetailDto>());
        }

        /// <summary>
        /// 删除文章（包括所有版本）
        /// </summary>
        public async Task<Result> DeleteArticleAsync(int id)
        {
            var post = await _postRepository.GetByIdAsync(id);
            if (post == null)
            {
                _logger.LogWarning("删除文章失败 - 文章不存在: {id}", id);
                return Result.Failure(_localizer["ArticleNotFound"].Value, ErrorCodes.NotFound);
            }

            // 版本会通过级联删除自动删除
            _postRepository.Delete(post);
            await _unitOfWork.SaveChangesAsync();

            try
            {
                await _fileService.DeleteAllArticleFilesAsync(post.FilePath, post.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("删除文章文件失败 - ID: {id}, 错误: {Error}", id, ex.Message);
            }

            _logger.LogInformation("文章删除成功 - ID: {id}", id);

            return Result.Success();
        }

        /// <summary>
        /// 清除文章的 SEO 缓存（当文章更新时调用）
        /// </summary>
        private void InvalidateSeoCache(string slug)
        {
            var cacheKey = $"seo:path:{slug}";
            _cache.Remove(cacheKey);
            _logger.LogDebug("SEO 缓存已清除: {Slug}", slug);
        }
    }
}
