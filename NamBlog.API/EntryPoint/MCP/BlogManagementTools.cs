using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using NamBlog.API.Application.DTOs;
using NamBlog.API.Application.Services;

namespace NamBlog.API.EntryPoint.MCP
{
    /// <summary>
    /// MCP 工具集合 - 博客管理
    /// 提供通过 Model Context Protocol (MCP) 协议管理博客的工具集
    /// 所有工具调用 Application 层服务，符合 DDD 分层架构
    /// MCP 层作为表示层，不包含业务逻辑，仅负责参数转换和结果封装
    /// </summary>
    [McpServerToolType]
    public class BlogManagementTools(
        ArticleCommandService commandService,
        ArticleQueryService queryService,
        IOptionsSnapshot<BlogInfo> blogSettings,
        ILogger<BlogManagementTools> logger)
    {
        private readonly ArticleCommandService _commandService = commandService;
        private readonly ArticleQueryService _queryService = queryService;
        private readonly IOptionsSnapshot<BlogInfo> _blogSettings = blogSettings;
        private readonly ILogger<BlogManagementTools> _logger = logger;

        #region ===================== 博客配置查询 =====================

        /// <summary>
        /// 获取博客基础信息和博主资料
        /// </summary>
        [McpServerTool(Name = "get_blog_info")]
        [Description("获取博客基础信息和博主资料，包括博客名称、博主昵称、联系邮箱、头像URL、个人签名、域名、网站图标及外链列表。返回完整的博客配置信息。")]
        public BlogInfo GetBlogInfo()
        {
            // _logger.LogDebug("MCP: 获取博客信息");
            return _blogSettings.Value;
        }

        #endregion

        #region ===================== 文章查询工具 =====================

        /// <summary>
        /// 查询文章列表（支持多条件过滤和分页）
        /// </summary>
        [McpServerTool(Name = "query_articles")]
        [Description("查询文章列表，支持多维度筛选和分页。返回文章摘要信息（不含 Markdown/HTML 内容）。筛选条件：分类（精确匹配）、标签（包含任意标签，用英文逗号分隔如'C#,Vue'）、发布状态、精选标记。分页参数：page（页码，从1开始，默认1）、pageSize（每页数量，范围1-100，默认10）。所有筛选条件采用 AND 组合，标签匹配采用 OR 逻辑。")]
        public async Task<PagedResult<ArticleListItemDto>> QueryArticles(
            [Description("分类名称，精确匹配。可选，不传则不按分类过滤。示例：'技术博客'")] string? category = null,
            [Description("标签列表，英文逗号分隔。匹配包含任意标签的文章（OR逻辑）。可选，不传则不按标签过滤。示例：'C#,ASP.NET,DDD'")] string? tags = null,
            [Description("发布状态过滤。可选：null（全部文章）、true（仅已发布）、false（仅未发布草稿）。默认 null。")] bool? isPublished = null,
            [Description("精选状态过滤。可选：null（全部文章）、true（仅精选文章）、false（仅非精选）。默认 null。")] bool? isFeatured = null,
            [Description("页码，从1开始。小于1时自动调整为1。默认 1。")] int page = 1,
            [Description("每页文章数量。有效范围：1-100，超出范围将自动调整。默认 10。")] int pageSize = 10)
        {
            // _logger.LogDebug("MCP: 查询文章列表 - 分类:{Category}, 标签:{Tags}, 发布:{IsPublished}, 精选:{IsFeatured}, 页码:{Page}/{PageSize}",
            //     category, tags, isPublished, isFeatured, page, pageSize);

            // 解析标签
            string[]? tagArray = string.IsNullOrWhiteSpace(tags)
                ? null
                : [.. tags.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t))];

            // 构建查询命令
            var queryCommand = new QueryArticlesCommand(
                Page: page,
                PageSize: pageSize,
                IsPublished: isPublished,
                Category: category,
                Tags: tagArray,
                IsFeatured: isFeatured,
                SearchKeyword: null); // 搜索功能暂未实现

            return await _queryService.QueryArticlesAsync(queryCommand);
        }

        /// <summary>
        /// 获取文章元数据（根据 Slug）
        /// </summary>
        [McpServerTool(Name = "get_article_metadata")]
        [Description("根据 slug 获取文章的完整元数据，包括标题、分类、标签、摘要、发布状态、版本列表等。不包含 Markdown 原文和 HTML 内容。参数：slug（文章的 URL 标识，必填，如 'hello-world'）、versionName（可选，用于验证指定版本是否存在，不存在则返回 null）。MCP 调用具有管理员权限，可查看未发布的文章。")]
        public async Task<ArticleDetailDto?> GetArticleMetadata(
            [Description("文章的 URL 标识（slug），必填。格式：小写字母、数字、连字符，如 'hello-world'。")] string slug,
            [Description("版本名称，可选。用于验证指定版本是否存在，版本不存在时返回 null。")] string? versionName = null)
        {
            // _logger.LogDebug("MCP: 获取文章元数据 - Slug:{Slug}, Version:{Version}", slug, versionName);

            if (string.IsNullOrWhiteSpace(slug))
            {
                _logger.LogWarning("MCP: Slug 不能为空");
                return null;
            }

            // 调用业务层服务（MCP 具有管理员权限，可查看未发布的文章）
            return await _queryService.GetArticleBySlugAsync(slug, versionName, includeUnpublished: true);
        }

        /// <summary>
        /// 获取文章指定版本的 HTML 内容
        /// </summary>
        [McpServerTool(Name = "get_article_html")]
        [Description("根据文章 ID 和版本名称获取该版本的 HTML 内容。用于预览或查看历史版本的渲染结果。参数：id（文章ID，必填）、versionName（版本名称，必填）。成功返回 HTML 字符串，失败返回错误信息字符串（以'错误：'开头）。")]
        public async Task<string> GetArticleHtml(
            [Description("文章ID，必填。可通过 get_article_metadata 或 query_articles 获取。")] int id,
            [Description("版本名称，必填。可通过 get_article_versions 获取可用版本列表。")] string versionName)
        {
            // _logger.LogDebug("MCP: 获取文章HTML - Id:{Id}, Version:{Version}", id, versionName);

            var result = await _queryService.GetVersionHtmlAsync(id, versionName);

            return result.IsSuccess
                ? result.Value!
                : $"错误：{result.ErrorMessage}";
        }

        /// <summary>
        /// 获取文章所有版本列表
        /// </summary>
        [McpServerTool(Name = "get_article_versions")]
        [Description("根据 slug 获取文章的所有历史版本列表，每个版本包含版本名称、创建时间、验证状态等信息。参数：slug（文章的 URL 标识，必填）。返回版本列表，文章不存在时返回空列表。")]
        public async Task<List<ArticleVersionDto>> GetArticleVersions(
            [Description("文章的 URL 标识（slug），必填。格式：小写字母、数字、连字符，如 'hello-world'。")] string slug)
        {
            // _logger.LogDebug("MCP: 获取文章版本列表 - Slug:{Slug}", slug);

            return await _queryService.GetVersionsAsync(slug);
        }

        /// <summary>
        /// 获取文章的 Markdown 原文
        /// </summary>
        [McpServerTool(Name = "get_article_markdown")]
        [Description("获取文章的 Markdown 原文。支持三种查询方式（至少提供一个）：1. 通过 slug（推荐，最准确）；2. 通过 id（次选）；3. 通过 title（模糊匹配，可能不精确）。优先级：id > slug > title。成功返回 Markdown 文本，失败返回错误信息字符串（以'错误：'开头）。")]
        public async Task<string> GetArticleMarkdown(
            [Description("文章的 URL 标识（slug），可选。格式：小写字母、数字、连字符，如 'hello-world'。")] string? slug = null,
            [Description("文章ID，可选。精确查询，优先级最高。")] int? id = null,
            [Description("文章标题，可选。模糊匹配，优先级最低，可能匹配到错误的文章。")] string? title = null)
        {
            // _logger.LogDebug("MCP: 获取文章Markdown - Slug:{Slug}, Id:{Id}, Title:{Title}", slug, id, title);

            var result = await _queryService.GetArticleMarkdownAsync(slug, id, title);

            return result.IsSuccess
                ? result.Value!
                : $"错误：{result.ErrorMessage}";
        }

        #endregion

        #region ===================== 统计查询工具 =====================

        /// <summary>
        /// 获取标签统计信息
        /// </summary>
        [McpServerTool(Name = "get_tags_statistics")]
        [Description("获取所有标签及其使用次数的统计信息，按使用次数降序排列。可选参数：category（分类名称，用于过滤指定分类下的标签统计）。MCP 调用具有管理员权限，统计包含未发布文章的标签。返回标签列表，每个标签包含名称和使用次数。")]
        public async Task<List<TagStatistic>> GetTagsStatistics(
            [Description("分类名称，可选。仅统计该分类下文章使用的标签。不传则统计所有标签。")] string? category = null)
        {
            // _logger.LogDebug("MCP: 获取标签统计 - Category:{Category}", category);

            // MCP 具有管理员权限，包含未发布文章的统计
            return await _queryService.GetTagsStatisticsAsync(category, includeUnpublished: true);
        }

        /// <summary>
        /// 获取分类统计信息
        /// </summary>
        [McpServerTool(Name = "get_categories_statistics")]
        [Description("获取所有分类及其文章数量的统计信息，按文章数量降序排列。可选参数：tags（标签列表，英文逗号分隔，用于过滤包含指定标签的分类统计）。MCP 调用具有管理员权限，统计包含未发布文章的分类。返回分类列表，每个分类包含名称和文章数量。")]
        public async Task<List<CategoryStatistic>> GetCategoriesStatistics(
            [Description("标签列表，英文逗号分隔，可选。仅统计包含这些标签的文章所属的分类。示例：'C#,DDD'。不传则统计所有分类。")] string? tags = null)
        {
            // _logger.LogDebug("MCP: 获取分类统计 - Tags:{Tags}", tags);

            // 解析标签
            string[]? tagArray = string.IsNullOrWhiteSpace(tags)
                ? null
                : [.. tags.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t))];

            // MCP 具有管理员权限，包含未发布文章的统计
            return await _queryService.GetCategoriesStatisticsAsync(tagArray, includeUnpublished: true);
        }

        #endregion

        #region ===================== 文章命令工具 =====================

        /// <summary>
        /// 保存文章（快速保存，不生成 HTML）
        /// </summary>
        [McpServerTool(Name = "save_article")]
        [Description("保存文章元数据和 Markdown 原文，不生成 HTML 版本。适用于草稿快速保存。使用场景：1. 创建文章（不传id）：Markdown 必填，AI 自动生成未提供的元数据（title/slug/category/tags/excerpt），返回丰富的文章数据供查验；2. 更新元数据（传id）：Markdown 可选，仅更新提供的字段，不创建新版本。参数：id（可选，创建/更新标识）、markdown（创建时必填，更新时可选）、title/slug/category/tags/excerpt（可选，AI自动生成）、isPublished/isFeatured（可选）。返回保存后的完整元数据，失败返回错误信息字符串（以'错误：'开头）。")]
        public async Task<string> SaveArticle(
            [Description("文章ID，可选。不传则创建新文章，传入则更新现有文章。")] int? id = null,
            [Description("Markdown 格式的文章内容，创建时必填，更新时可选。支持标准 Markdown 语法及扩展语法（表格、代码块等）。")] string? markdown = null,
            [Description("文章标题，可选。不传则由 AI 根据 Markdown 内容自动生成。")] string? title = null,
            [Description("URL 标识（slug），可选。格式：小写字母、数字、连字符。不传则由 AI 根据标题自动生成。")] string? slug = null,
            [Description("分类名称，可选。不传则使用系统默认分类。示例：'技术博客'。")] string? category = null,
            [Description("标签列表，英文逗号分隔，可选。不传则由 AI 根据内容自动生成。示例：'C#,DDD,GraphQL'。")] string? tags = null,
            [Description("文章摘要，可选。不传则由 AI 根据内容自动生成。")] string? excerpt = null,
            [Description("是否发布，可选。true=发布，false=草稿，null=保持现有状态（新文章默认 false）。")] bool? isPublished = null,
            [Description("是否精选，可选。true=精选，false=普通，null=保持现有状态（新文章默认 false）。")] bool? isFeatured = null,
            [Description("主版本名称，可选。仅更新时有效，用于切换文章的发布版本。")] string? mainVersion = null,
            [Description("自定义 AI 提示词，可选。用于指导 AI 生成元数据的风格。")] string? customPrompt = null)
        {
            _logger.LogInformation("MCP: 保存文章 - Id:{Id}, Title:{Title}", id, title);

            // 解析标签
            string[]? tagArray = string.IsNullOrWhiteSpace(tags)
                ? null
                : [.. tags.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t))];

            var command = new SaveArticleCommand(
                Id: id,
                Markdown: markdown,
                Title: title,
                Slug: slug,
                Category: category,
                Tags: tagArray,
                Excerpt: excerpt,
                IsFeatured: isFeatured,
                IsPublished: isPublished,
                MainVersion: mainVersion,
                CustomPrompt: customPrompt);

            var result = await _commandService.SaveArticleAsync(command);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("MCP: 保存文章失败 - {Error}", result.ErrorMessage);
                return $"错误：{result.ErrorMessage}";
            }

            _logger.LogInformation("MCP: 保存文章成功 - PostId:{PostId}, Slug:{Slug}",
                result.Value!.PostId, result.Value.Slug);

            // 返回成功信息及关键数据
            return $"成功保存文章 - ID: {result.Value.PostId}, Slug: {result.Value.Slug}, 标题: {result.Value.Title}";
        }

        /// <summary>
        /// 提交文章版本（完整流程：生成 HTML + 创建版本）
        /// </summary>
        [McpServerTool(Name = "submit_article_version")]
        [Description("提交文章新版本，调用 AI 生成 HTML 并创建版本历史记录。适用于正式发布文章。使用场景：1. 创建文章（不传id）：可提供完整元数据和HTML（不经AI生成），返回 slug；2. 为现有文章创建新版本（传id）：生成新HTML版本，可同时更新元数据。与 save_article 的区别：本方法创建版本历史，可不调用AI生成HTML而是自己提供，适合自定义。参数：markdown（必填）、id（可选）、html（可选，通常留空让AI生成）、title/slug/category/tags/excerpt（可选）、isPublished/isFeatured（可选）、customPrompt（可选，控制HTML生成风格）。返回 slug 或错误信息。")]
        public async Task<string> SubmitArticleVersion(
            [Description("Markdown 格式的文章内容，必填。支持标准 Markdown 语法及扩展语法。")] string markdown,
            [Description("文章ID，可选。不传则创建新文章，传入则为现有文章创建新版本。")] int? id = null,
            [Description("HTML 内容，可选。通常不需要传入，AI 会根据 Markdown 自动生成。仅在需要自定义 HTML 时手动指定。")] string? html = null,
            [Description("文章标题，可选。不传则由 AI 自动生成。")] string? title = null,
            [Description("URL 标识（slug），可选。格式：小写字母、数字、连字符。不传则由 AI 自动生成。")] string? slug = null,
            [Description("分类名称，可选。不传则使用系统默认分类。")] string? category = null,
            [Description("标签列表，英文逗号分隔，可选。不传则由 AI 自动生成。示例：'C#,DDD,GraphQL'。")] string? tags = null,
            [Description("文章摘要，可选。不传则由 AI 自动生成。")] string? excerpt = null,
            [Description("是否发布，可选。true=发布，false=草稿，null=保持现有状态（新文章默认 false）。")] bool? isPublished = null,
            [Description("是否精选，可选。true=精选，false=普通，null=保持现有状态（新文章默认 false）。")] bool? isFeatured = null,
            [Description("自定义 AI 提示词，可选。用于指导 AI 生成特定风格的 HTML（如简洁风格、学术风格等）。")] string? customPrompt = null)
        {
            _logger.LogInformation("MCP: 提交文章版本 - Id:{Id}, Title:{Title}", id, title);

            // 解析标签
            string[]? tagArray = string.IsNullOrWhiteSpace(tags)
                ? null
                : [.. tags.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t))];

            var command = new SubmitArticleCommand(
                Markdown: markdown,
                Id: id,
                Html: html,
                Title: title,
                Slug: slug,
                Category: category,
                Tags: tagArray,
                Excerpt: excerpt,
                IsFeatured: isFeatured,
                IsPublished: isPublished,
                CustomPrompt: customPrompt);

            var result = await _commandService.SubmitArticleAsync(command);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("MCP: 提交版本失败 - {Error}", result.ErrorMessage);
                return $"错误：{result.ErrorMessage}";
            }

            _logger.LogInformation("MCP: 提交版本成功 - Slug:{Slug}", result.Value!.Slug);

            return $"成功提交文章版本 - Slug: {result.Value.Slug}";
        }

        /// <summary>
        /// 删除文章指定版本
        /// </summary>
        [McpServerTool(Name = "delete_article_version")]
        [Description("删除文章的指定版本。如果删除的是文章唯一的版本，则整篇文章也会被删除。注意：此操作不可逆，删除后无法恢复。参数：id（文章ID，必填）、versionName（版本名称，必填，如 'v1.0.0'）。返回操作结果消息。")]
        public async Task<string> DeleteArticleVersion(
            [Description("文章ID，必填。")] int id,
            [Description("要删除的版本名称，必填。如 'v1.0.0'、'draft-2024-01-01'。")] string versionName)
        {
            _logger.LogInformation("MCP: 删除文章版本 - Id:{Id}, Version:{Version}", id, versionName);

            var result = await _commandService.DeleteVersionAsync(id, versionName);

            if (!result.IsSuccess)
            {
                return $"错误：{result.ErrorMessage}";
            }

            _logger.LogInformation("MCP: 删除版本成功 - Id:{Id}, Version:{Version}", id, versionName);

            return $"成功删除版本：{versionName}";
        }

        /// <summary>
        /// 切换文章发布状态
        /// </summary>
        [McpServerTool(Name = "toggle_publish")]
        [Description("切换文章的发布状态（发布 ↔ 取消发布）。已发布的文章将取消发布，未发布的文章将发布。参数：id（文章ID，必填）。返回操作结果消息及新的发布状态。")]
        public async Task<string> TogglePublish(
            [Description("文章ID，必填。")] int id)
        {
            _logger.LogInformation("MCP: 切换发布状态 - Id:{Id}", id);

            var result = await _commandService.TogglePublishAsync(id);

            if (!result.IsSuccess)
            {
                return $"错误：{result.ErrorMessage}";
            }

            var newStatus = result.Value!.IsPublished ? "已发布" : "未发布";
            _logger.LogInformation("MCP: 切换发布状态成功 - Id:{Id}, 新状态:{Status}", id, newStatus);

            return $"成功切换发布状态 - Slug: {result.Value.Slug}, 当前状态：{newStatus}";
        }

        /// <summary>
        /// 删除整篇文章
        /// </summary>
        [McpServerTool(Name = "delete_article")]
        [Description("删除整篇文章及其所有版本。注意：此操作不可逆，删除后无法恢复，包括所有历史版本和文件。参数：id（文章ID，必填）。返回操作结果消息。")]
        public async Task<string> DeleteArticle(
            [Description("文章ID，必填。")] int id)
        {
            _logger.LogInformation("MCP: 删除文章 - Id:{Id}", id);

            var result = await _commandService.DeleteArticleAsync(id);

            if (!result.IsSuccess)
            {
                return $"错误：{result.ErrorMessage}";
            }

            _logger.LogInformation("MCP: 删除文章成功 - Id:{Id}", id);

            return $"成功删除文章 - ID: {id}";
        }

        #endregion
    }
}
