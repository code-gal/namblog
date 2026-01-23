using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NamBlog.API.Application.Common;
using NamBlog.API.Application.DTOs;
using NamBlog.API.Application.Resources;
using NamBlog.API.Domain.Entities;
using NamBlog.API.Domain.Interfaces;

namespace NamBlog.API.Application.Services
{
    public class ArticleQueryService(
        IPostRepository postRepository,
        IFileService fileService,
        IStringLocalizer<SharedResource> localizer,
        ILogger<ArticleCommandService> logger)
    {
        private readonly IPostRepository _postRepository = postRepository;
        private readonly IFileService _fileService = fileService;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;
        private readonly ILogger<ArticleCommandService> _logger = logger;

        /// <summary>
        /// 查询文章列表（统一查询，支持分页和过滤）
        /// </summary>
        public async Task<PagedResult<ArticleListItemDto>> QueryArticlesAsync(QueryArticlesCommand query)
        {
            // 1. 构建基础查询
            var dbQuery = _postRepository.GetAll().AsNoTracking();

            // 2. 应用数据库层过滤
            if (query.IsPublished.HasValue)
                dbQuery = dbQuery.Where(p => p.IsPublished == query.IsPublished.Value);

            if (query.IsFeatured.HasValue)
                dbQuery = dbQuery.Where(p => p.IsFeatured == query.IsFeatured.Value);

            if (!string.IsNullOrWhiteSpace(query.Category))
                dbQuery = dbQuery.Where(p => p.Category == query.Category);

            // 3. 标签过滤（需要客户端评估）
            List<Post> posts;
            int totalCount;

            if (query.Tags != null && query.Tags.Length > 0)
            {
                var allPosts = await dbQuery.ToListAsync();
                var filteredPosts = allPosts
                    .Where(p => p.Tags != null && p.Tags.Any(tag => query.Tags.Contains(tag.Name)))
                    .ToList();

                totalCount = filteredPosts.Count;
                posts = [.. filteredPosts
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)];
            }
            else
            {
                totalCount = await dbQuery.CountAsync();
                posts = await dbQuery
                    .OrderByDescending(p => p.PostId)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();
            }

            // 4. 计算分页信息
            var pageInfo = PageInfo.Create(query.Page, query.PageSize, totalCount);

            // 5. 映射为 DTO
            var items = posts.Select(p => p.Adapt<ArticleListItemDto>()).ToList();

            return new PagedResult<ArticleListItemDto>
            {
                Items = items,
                PageInfo = pageInfo
            };
        }

        /// <summary>
        /// 获取文章详情（根据 Slug）
        /// </summary>
        public async Task<ArticleDetailDto?> GetArticleBySlugAsync(string slug, string? versionName = null, bool includeUnpublished = false)
        {
            var post = await _postRepository.GetBySlugAsync(slug);

            if (post == null)
                return null;

            // 如果不包含未发布的文章且文章未发布，返回 null
            if (!includeUnpublished && !post.IsPublished)
                return null;

            // 验证版本（如果指定）
            if (!string.IsNullOrWhiteSpace(versionName))
            {
                var versionExists = post.Versions?.Any(v => v.VersionName == versionName) ?? false;
                if (!versionExists)
                {
                    _logger.LogWarning("请求的版本不存在：Slug={Slug}, Version={Version}", slug, versionName);
                    return null;
                }

                // 如果请求的不是发布版本，需要权限
                if (versionName != post.MainVersion?.VersionName && !includeUnpublished)
                {
                    _logger.LogWarning("尝试访问非发布版本但未授权：Slug={Slug}, Version={Version}", slug, versionName);
                    return null;
                }
            }

            return post.Adapt<ArticleDetailDto>();
        }

        /// <summary>
        /// 获取指定版本的HTML内容（用于版本预览，不修改数据库）
        /// </summary>
        public async Task<Result<string>> GetVersionHtmlAsync(int id, string versionName)
        {
            var post = await _postRepository.GetByIdAsync(id);
            if (post == null)
                return Result.Failure<string>(_localizer["ArticleNotFound"].Value, ErrorCodes.NotFound);

            // 查找版本
            var version = post.Versions.FirstOrDefault(v => v.VersionName == versionName);
            if (version == null)
                return Result.Failure<string>(_localizer["VersionNotFound", versionName].Value, ErrorCodes.NotFound);

            // 读取HTML文件
            var html = await _fileService.ReadHtmlAsync(post.FilePath, post.FileName, versionName);
            if (html == null)
                return Result.Failure<string>(_localizer["HtmlFileNotFound"].Value, ErrorCodes.NotFound);

            // _logger.LogDebug("版本HTML查询成功 - ID: {PostId}, 版本: {VersionName}", id, versionName);

            return Result.Success(html);
        }

        /// <summary>
        /// 获取文章的所有版本
        /// </summary>
        public async Task<List<ArticleVersionDto>> GetVersionsAsync(string slug)
        {
            var post = await _postRepository.GetBySlugAsync(slug);
            if (post == null)
                return [];

            return [.. post.Versions
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => v.Adapt<ArticleVersionDto>())];
        }

        /// <summary>
        /// 获取文章的 Markdown 源文件（管理员专用）
        /// </summary>
        /// <param name="slug">文章 slug（可选）</param>
        /// <param name="id">文章 id（可选）</param>
        /// <param name="title">文章标题（可选，模糊匹配）</param>
        public async Task<Result<string>> GetArticleMarkdownAsync(string? slug = null, int? id = null, string? title = null)
        {
            // 检查至少有一个参数
            if (string.IsNullOrWhiteSpace(slug) && id == null && string.IsNullOrWhiteSpace(title))
                return Result.Failure<string>(_localizer["QueryParameterRequired"].Value, ErrorCodes.ValidationFailed);

            Post? post = null;

            // 优先使用 id
            if (id.HasValue)
            {
                post = await _postRepository.GetByIdAsync(id.Value);
                if (post != null)
                {
                    var markdown = await _fileService.ReadMarkdownAsync(post.FilePath, post.FileName);
                    if (markdown == null)
                        return Result.Failure<string>(_localizer["MarkdownFileNotFound"].Value, ErrorCodes.NotFound);

                    // _logger.LogDebug("Markdown 查询成功 - Id: {Id}", id);
                    return Result.Success(markdown);
                }
            }

            // 其次使用 slug
            if (!string.IsNullOrWhiteSpace(slug))
            {
                post = await _postRepository.GetBySlugAsync(slug);
                if (post != null)
                {
                    var markdown = await _fileService.ReadMarkdownAsync(post.FilePath, post.FileName);
                    if (markdown == null)
                        return Result.Failure<string>(_localizer["MarkdownFileNotFound"].Value, ErrorCodes.NotFound);

                    // _logger.LogDebug("Markdown 查询成功 - Slug: {Slug}", slug);
                    return Result.Success(markdown);
                }
            }

            // 最后使用 title（模糊匹配）
            if (!string.IsNullOrWhiteSpace(title))
            {
                var matchedPost = await _postRepository.GetAll()
                    .Where(p => p.Title != null && p.Title.Contains(title!))
                    .FirstOrDefaultAsync();
                if (matchedPost != null)
                {
                    post = matchedPost;
                    var markdown = await _fileService.ReadMarkdownAsync(post.FilePath, post.FileName);
                    if (markdown == null)
                        return Result.Failure<string>(_localizer["MarkdownFileNotFound"].Value, ErrorCodes.NotFound);

                    // _logger.LogDebug("Markdown 查询成功 - Title: {Title}", title);
                    return Result.Success(markdown);
                }
            }

            return Result.Failure<string>(_localizer["ArticleNotFound"].Value, ErrorCodes.NotFound);
        }

        /// <summary>
        /// 读取文章 Markdown（通过文件路径，供 GraphQL Resolver 调用）
        /// </summary>
        public async Task<Result<string>> ReadMarkdownByPathAsync(string filePath, string fileName)
        {
            try
            {
                var markdown = await _fileService.ReadMarkdownAsync(filePath, fileName);
                if (markdown == null)
                    return Result.Failure<string>(_localizer["MarkdownFileNotFound"].Value, ErrorCodes.NotFound);

                return Result.Success(markdown);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取 Markdown 失败 - FilePath: {FilePath}, FileName: {FileName}", filePath, fileName);
                return Result.Failure<string>(_localizer["ReadMarkdownFailed", ex.Message].Value, ErrorCodes.InternalError);
            }
        }

        /// <summary>
        /// 获取文章流（异步流，用于大数据集）
        /// </summary>
        public async IAsyncEnumerable<ArticleListItemDto> GetArticlesStreamAsync(
            bool? isPublished = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var query = _postRepository.GetAll().AsNoTracking();

            if (isPublished.HasValue)
                query = query.Where(p => p.IsPublished == isPublished.Value);

            query = query.OrderByDescending(p => p.PostId);

            await foreach (var post in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                yield return post.Adapt<ArticleListItemDto>();
            }
        }

        /// <summary>
        /// 获取文章详情（管理员编辑专用，包含所有分类和标签列表）
        /// </summary>
        public async Task<ArticleMetadataDto?> GetArticleForEditAsync(int postId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("获取编辑数据失败 - 文章不存在: {PostId}", postId);
                return null;
            }

            // 并行查询分类和标签统计（管理员可见所有数据）
            var categoriesTask = GetCategoriesStatisticsAsync(includeUnpublished: true);
            var tagsTask = GetTagsStatisticsAsync(includeUnpublished: true);

            await Task.WhenAll(categoriesTask, tagsTask);

            // 构建 MainVersionDetail
            MainVersionDetail? mainVersionDetail = null;
            if (post.MainVersion != null)
            {
                var html = await _fileService.ReadHtmlAsync(
                    post.FilePath,
                    post.FileName,
                    post.MainVersion.VersionName
                );

                mainVersionDetail = new MainVersionDetail(
                    post.MainVersion.VersionName,
                    html ?? "",
                    post.MainVersion.ValidationStatus,
                    post.MainVersion.HtmlValidationError
                );
            }

            // 提取 AI 提示词列表（去重并过滤 null）
            var aiPrompts = post.Versions
                .Select(v => v.AiPrompt)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .Select(p => p!)
                .ToArray();

            // 提取版本名称列表
            var versionNames = post.Versions
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => v.VersionName)
                .ToArray();

            _logger.LogInformation("获取编辑数据成功 - PostId: {PostId}, 版本数: {VersionCount}",
                postId, versionNames.Length);

            return new ArticleMetadataDto(
                PostId: post.PostId,
                Title: post.Title,
                Slug: post.Slug,
                Excerpt: post.Excerpt,
                Category: post.Category,
                Tags: [.. post.Tags.Select(t => t.Name)],
                AllCategories: await categoriesTask,
                AllTags: await tagsTask,
                AiPrompts: aiPrompts,
                VersionNames: versionNames,
                MainVersion: mainVersionDetail,
                IsPublished: post.IsPublished,
                IsFeatured: post.IsFeatured,
                CreateTime: post.CreateTime,
                LastModified: post.LastModified
            );
        }

        #region ===================== 统计查询 =====================

        /// <summary>
        /// 获取标签统计信息（每个标签有多少篇文章）
        /// </summary>
        /// <param name="category">可选的分类过滤</param>
        /// <param name="includeUnpublished">是否包含未发布的文章（需要管理员权限，默认只统计已发布）</param>
        public async Task<List<TagStatistic>> GetTagsStatisticsAsync(string? category = null, bool includeUnpublished = false)
        {
            try
            {
                var rawStats = await _postRepository.GetTagsStatisticsAsync(category, includeUnpublished);

                // 应用层负责转换为 DTO 并排序
                return [.. rawStats
                    .Select(s => new TagStatistic(s.Name, s.Count))
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Name)];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取标签统计失败，Category={Category}", category);
                return [];
            }
        }

        /// <summary>
        /// 获取分类统计信息（每个分类有多少篇文章）
        /// </summary>
        /// <param name="tags">可选的标签过滤</param>
        /// <param name="includeUnpublished">是否包含未发布的文章（默认只统计已发布）</param>
        public async Task<List<CategoryStatistic>> GetCategoriesStatisticsAsync(string[]? tags = null, bool includeUnpublished = false)
        {
            try
            {
                var rawStats = await _postRepository.GetCategoriesStatisticsAsync(tags, includeUnpublished);

                // 应用层负责转换为 DTO 并排序
                return [.. rawStats
                    .Select(s => new CategoryStatistic(s.Name, s.Count))
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Name)];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取分类统计失败，Tags={Tags}", tags != null ? string.Join(",", tags) : "null");
                return [];
            }
        }

        #endregion

        #region SEO 优化相关

        /// <summary>
        /// 获取文章的主版本名称（用于 SEO 优化的静态路径构建）
        /// </summary>
        /// <param name="slug">文章 slug</param>
        /// <returns>主版本名称，如果文章不存在或无主版本则返回 null</returns>
        public async Task<string?> GetMainVersionNameAsync(string slug)
        {
            var post = await _postRepository.GetBySlugAsync(slug);
            return post?.MainVersion?.VersionName;
        }

        /// <summary>
        /// 获取文章的 SEO 信息（用于 SEO 中间件构造静态 HTML 路径）
        /// 仅返回已发布且主版本验证通过的文章信息
        /// </summary>
        /// <param name="slug">文章 slug</param>
        /// <returns>SEO 文章信息（包含 FilePath、FileName、VersionName），不符合条件则返回 null</returns>
        public async Task<SeoArticleInfo?> GetSeoArticleInfoAsync(string slug)
        {
            var post = await _postRepository.GetBySlugAsync(slug);

            // 校验：必须已发布
            if (post == null || !post.IsPublished)
                return null;

            // 校验：必须有主版本且验证通过
            if (post.MainVersion?.ValidationStatus != HtmlValidationStatus.Valid)
                return null;

            return new SeoArticleInfo(
                post.FilePath,
                post.FileName,
                post.MainVersion.VersionName
            );
        }

        #endregion
    }
}
