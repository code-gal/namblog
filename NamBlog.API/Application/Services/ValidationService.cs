using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NamBlog.API.Application.Common;
using NamBlog.API.Domain.Interfaces;
using NamBlog.API.Domain.Specifications;

namespace NamBlog.API.Application.Services
{
    /// <summary>
    /// 验证服务实现
    /// </summary>
    public partial class ValidationService(IPostRepository postRepository, ILogger<ValidationService> logger)
    {
        // ==================== 唯一性验证 ====================

        /// <summary>
        /// 验证标题是否唯一
        /// </summary>
        public async Task<Result<bool>> ValidateTitleUniqueAsync(string title, int? excludePostId = null)
        {
            if (!ValidationRuleset.Post.Title.IsValid(title))
            {
                return Result.Failure<bool>(
                    ValidationRuleset.Post.Title.GetValidationError(title, "Title"),
                    ErrorCodes.ValidationFailed);
            }

            var query = postRepository.GetAll().Where(p => p.Title == title);

            if (excludePostId.HasValue)
            {
                query = query.Where(p => p.PostId != excludePostId.Value);
            }

            var exists = await query.AnyAsync();

            if (exists)
            {
                logger.LogWarning("标题已存在：{Title}", title);
                return Result.Failure<bool>($"标题 '{title}' 已存在", ErrorCodes.AlreadyExists);
            }

            return Result.Success(true);
        }

        /// <summary>
        /// 验证Slug是否唯一
        /// </summary>
        public async Task<Result<bool>> ValidateSlugUniqueAsync(string slug, int? excludePostId = null)
        {
            if (!ValidationRuleset.Post.Slug.IsValid(slug))
            {
                return Result.Failure<bool>(
                    ValidationRuleset.Post.Slug.GetValidationError(slug, "Slug"),
                    ErrorCodes.ValidationFailed);
            }

            var query = postRepository.GetAll().Where(p => p.Slug == slug);

            if (excludePostId.HasValue)
            {
                query = query.Where(p => p.PostId != excludePostId.Value);
            }

            var exists = await query.AnyAsync();

            if (exists)
            {
                logger.LogWarning("Slug已存在：{Slug}", slug);
                return Result.Failure<bool>($"Slug '{slug}' 已存在", ErrorCodes.AlreadyExists);
            }

            return Result.Success(true);
        }

        // ==================== 格式验证 ====================

        /// <summary>
        /// 验证Markdown格式和长度
        /// </summary>
        public Result<bool> ValidateMarkdown(string markdown)
        {
            if (!ValidationRuleset.Post.Markdown.IsValid(markdown))
            {
                return Result.Failure<bool>(
                    ValidationRuleset.Post.Markdown.GetValidationError(markdown, "Markdown"),
                    ErrorCodes.ValidationFailed);
            }

            return Result.Success(true);
        }

        /// <summary>
        /// 验证标题格式和长度
        /// </summary>
        public Result<bool> ValidateTitleFormat(string title)
        {
            if (!ValidationRuleset.Post.Title.IsValid(title))
            {
                return Result.Failure<bool>(
                    ValidationRuleset.Post.Title.GetValidationError(title, "Title"),
                    ErrorCodes.ValidationFailed);
            }

            return Result.Success(true);
        }

        /// <summary>
        /// 验证Slug格式
        /// </summary>
        public Result<bool> ValidateSlugFormat(string slug)
        {
            if (!ValidationRuleset.Post.Slug.IsValid(slug))
            {
                return Result.Failure<bool>(
                    ValidationRuleset.Post.Slug.GetValidationError(slug, "Slug"),
                    ErrorCodes.ValidationFailed);
            }

            return Result.Success(true);
        }

        /// <summary>
        /// 验证分类格式和长度
        /// </summary>
        public Result<bool> ValidateCategoryFormat(string category)
        {
            if (!ValidationRuleset.Post.Category.IsValid(category))
            {
                return Result.Failure<bool>(
                    ValidationRuleset.Post.Category.GetValidationError(category, "Category"),
                    ErrorCodes.ValidationFailed);
            }

            return Result.Success(true);
        }

        /// <summary>
        /// 验证标签格式
        /// </summary>
        public Result<bool> ValidateTagsFormat(string[] tags)
        {
            if (!ValidationRuleset.Post.Tags.IsValid(tags))
            {
                return Result.Failure<bool>(
                    ValidationRuleset.Post.Tags.GetValidationError(tags, "Tags"),
                    ErrorCodes.ValidationFailed);
            }

            return Result.Success(true);
        }

        /// <summary>
        /// 验证摘要格式和长度
        /// </summary>
        public Result<bool> ValidateExcerptFormat(string excerpt)
        {
            if (!ValidationRuleset.Post.Excerpt.IsValid(excerpt))
            {
                return Result.Failure<bool>(
                    ValidationRuleset.Post.Excerpt.GetValidationError(excerpt, "Excerpt"),
                    ErrorCodes.ValidationFailed);
            }

            return Result.Success(true);
        }

        // ==================== 查询方法 ====================

        /// <summary>
        /// 获取所有已存在的标题列表
        /// </summary>
        public async Task<List<string>> GetExistingTitlesAsync()
        {
            return [.. (await postRepository.GetAll()
                .Select(p => p.Title)
                .ToListAsync())
                .Where(title => title is not null)
                .Cast<string>()];
        }

        /// <summary>
        /// 获取所有已存在的Slug列表
        /// </summary>
        public async Task<List<string>> GetExistingSlugsAsync()
        {
            return [.. (await postRepository.GetAll()
                .Select(p => p.Slug)
                .ToListAsync())
                .Where(slug => slug is not null)
                .Cast<string>()];
        }
    }
}
