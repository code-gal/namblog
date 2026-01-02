using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NamBlog.API.Application.Common;
using NamBlog.API.Domain.Interfaces;

namespace NamBlog.API.Application.Services
{
    /// <summary>
    /// 文章元数据处理结果
    /// </summary>
    public record ProcessedMetadata(
        string Title,
        string Slug,
        string Category,
        string[] Tags,
        string Excerpt);

    /// <summary>
    /// 元数据处理器
    /// 职责：处理并补全文章元数据（验证用户输入 + AI 生成缺失部分）
    /// </summary>
    public class MetadataProcessor(
        IAIService aiService,
        ValidationService validationService,
        ILogger<MetadataProcessor> logger)
    {
        private readonly IAIService _aiService = aiService;
        private readonly ValidationService _validationService = validationService;
        private readonly ILogger<MetadataProcessor> _logger = logger;

        private const int _maxRetries = 3;

        /// <summary>
        /// 处理并补全元数据（验证 + AI 生成）
        /// </summary>
        public async Task<Result<ProcessedMetadata>> ProcessMetadataAsync(
            string markdown,
            string? title,
            string? slug,
            string? category,
            string[]? tags,
            string? excerpt,
            int? excludePostId = null)
        {
            // 1. 处理标题
            var titleResult = await ProcessTitleAsync(markdown, title, excludePostId);
            if (!titleResult.IsSuccess)
                return Result.Failure<ProcessedMetadata>(titleResult.ErrorMessage!, titleResult.ErrorCode);

            var finalTitle = titleResult.Value!;

            // 2. 处理 Slug
            var slugResult = await ProcessSlugAsync(finalTitle, slug, excludePostId);
            if (!slugResult.IsSuccess)
                return Result.Failure<ProcessedMetadata>(slugResult.ErrorMessage!, slugResult.ErrorCode);

            var finalSlug = slugResult.Value!;

            // 3. 处理标签、摘要、分类
            var finalTags = await ProcessTagsAsync(markdown, tags);
            var finalExcerpt = await ProcessExcerptAsync(markdown, excerpt);
            var finalCategory = ProcessCategory(category);

            return Result.Success(new ProcessedMetadata(finalTitle, finalSlug, finalCategory, finalTags, finalExcerpt));
        }

        private async Task<Result<string>> ProcessTitleAsync(string markdown, string? title, int? excludePostId)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                var formatResult = _validationService.ValidateTitleFormat(title);
                if (!formatResult.IsSuccess)
                    return Result.Failure<string>(formatResult.ErrorMessage!, ErrorCodes.ValidationFailed);

                var uniqueResult = await _validationService.ValidateTitleUniqueAsync(title, excludePostId);
                if (!uniqueResult.IsSuccess)
                    return Result.Failure<string>(uniqueResult.ErrorMessage!, ErrorCodes.AlreadyExists);

                return Result.Success(title);
            }

            return await GenerateTitleWithRetryAsync(markdown, excludePostId);
        }

        private async Task<Result<string>> ProcessSlugAsync(string title, string? slug, int? excludePostId)
        {
            if (!string.IsNullOrWhiteSpace(slug))
            {
                var formatResult = _validationService.ValidateSlugFormat(slug);
                if (!formatResult.IsSuccess)
                    return Result.Failure<string>(formatResult.ErrorMessage!, ErrorCodes.ValidationFailed);

                var uniqueResult = await _validationService.ValidateSlugUniqueAsync(slug, excludePostId);
                if (!uniqueResult.IsSuccess)
                    return Result.Failure<string>(uniqueResult.ErrorMessage!, ErrorCodes.AlreadyExists);

                return Result.Success(slug);
            }

            return await GenerateSlugWithRetryAsync(title, excludePostId);
        }

        private async Task<string[]> ProcessTagsAsync(string markdown, string[]? tags)
        {
            if (tags != null && tags.Length > 0)
            {
                var formatResult = _validationService.ValidateTagsFormat(tags);
                return formatResult.IsSuccess ? tags : ["Untagged"];
            }

            var tagsResult = await _aiService.GenerateTagsAsync(markdown);
            if (!tagsResult.IsSuccess)
                return ["Untagged"];

            var validationResult = _validationService.ValidateTagsFormat(tagsResult.Value!);
            return validationResult.IsSuccess ? tagsResult.Value! : ["Untagged"];
        }

        private async Task<string> ProcessExcerptAsync(string markdown, string? excerpt)
        {
            if (!string.IsNullOrWhiteSpace(excerpt))
            {
                var excerptValidation = _validationService.ValidateExcerptFormat(excerpt);
                return excerptValidation.IsSuccess
                    ? excerpt
                    : (excerpt.Length > 500 ? excerpt[..500] : excerpt);
            }

            var excerptResult = await _aiService.GenerateExcerptAsync(markdown);
            if (!excerptResult.IsSuccess)
                return markdown.Length > 200 ? GenerateExcerpt(markdown) : markdown;

            var validation = _validationService.ValidateExcerptFormat(excerptResult.Value!);
            return validation.IsSuccess
                ? excerptResult.Value!
                : (excerptResult.Value!.Length > 500 ? excerptResult.Value![..500] : excerptResult.Value!);
        }

        private string ProcessCategory(string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return "Uncategorized";

            var categoryValidation = _validationService.ValidateCategoryFormat(category);
            return categoryValidation.IsSuccess ? category : "Uncategorized";
        }

        private async Task<Result<string>> GenerateTitleWithRetryAsync(string markdown, int? excludePostId)
        {
            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                var titleResult = await _aiService.GenerateTitleAsync(markdown);
                if (!titleResult.IsSuccess)
                {
                    if (attempt == _maxRetries)
                        return Result.Failure<string>(
                            $"AI 生成标题失败：{titleResult.ErrorMessage}",
                            titleResult.ErrorCode ?? ErrorCodes.ExternalServiceError);
                    continue;
                }

                var generatedTitle = titleResult.Value!;

                var formatResult = _validationService.ValidateTitleFormat(generatedTitle);
                if (!formatResult.IsSuccess)
                {
                    if (attempt < _maxRetries)
                        continue;
                    return Result.Failure<string>("AI 生成的标题格式不符合要求", ErrorCodes.ValidationFailed);
                }

                var uniqueResult = await _validationService.ValidateTitleUniqueAsync(generatedTitle, excludePostId);
                if (!uniqueResult.IsSuccess)
                {
                    if (attempt < _maxRetries)
                        continue;
                    return Result.Failure<string>("AI 生成的标题已存在，请手动指定标题", ErrorCodes.AlreadyExists);
                }

                return Result.Success(generatedTitle);
            }

            return Result.Failure<string>("AI 生成标题失败", ErrorCodes.ExternalServiceError);
        }

        private async Task<Result<string>> GenerateSlugWithRetryAsync(string title, int? excludePostId)
        {
            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                var slugResult = await _aiService.GenerateSlugAsync(title);
                if (!slugResult.IsSuccess)
                {
                    if (attempt == _maxRetries)
                        return Result.Failure<string>(
                            $"AI 生成 Slug 失败：{slugResult.ErrorMessage}",
                            slugResult.ErrorCode ?? ErrorCodes.ExternalServiceError);
                    continue;
                }

                var generatedSlug = slugResult.Value!;

                var formatResult = _validationService.ValidateSlugFormat(generatedSlug);
                if (!formatResult.IsSuccess)
                {
                    if (attempt < _maxRetries)
                        continue;
                    return Result.Failure<string>(
                        $"AI 生成的 Slug 格式不正确：{formatResult.ErrorMessage}",
                        ErrorCodes.ValidationFailed);
                }

                var uniqueResult = await _validationService.ValidateSlugUniqueAsync(generatedSlug, excludePostId);
                if (!uniqueResult.IsSuccess)
                {
                    if (attempt < _maxRetries)
                        continue;
                    return Result.Failure<string>("AI 生成的 Slug 已存在，请手动指定 Slug", ErrorCodes.AlreadyExists);
                }

                return Result.Success(generatedSlug);
            }

            return Result.Failure<string>("AI 生成 Slug 失败", ErrorCodes.ExternalServiceError);
        }

        /// <summary>
        /// 从 Markdown 提取简单摘要（前 200 字符）
        /// </summary>
        private static string GenerateExcerpt(string markdown)
        {
            var text = markdown
                .Replace("#", "")
                .Replace("*", "")
                .Replace("`", "")
                .Trim();

            return text.Length > 200 ? $"{text[..200]}..." : text;
        }
    }
}
