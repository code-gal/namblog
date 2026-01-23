namespace NamBlog.API.Application.DTOs
{
    /// <summary>
    /// 保存文章命令（Save按钮，支持创建和更新）
    /// 说明：id为null表示创建新文章，否则更新现有文章
    /// 创建时生成HTML版本（Markdown必填），更新时只保存元数据（Markdown可选）
    /// </summary>
    public record SaveArticleCommand(
        int? Id = null,
        string? Markdown = null,
        string? Title = null,
        string? Slug = null,
        string? Category = null,
        string[]? Tags = null,
        string? Excerpt = null,
        bool? IsFeatured = null,
        bool? IsPublished = null,
        string? MainVersion = null,
        string? CustomPrompt = null);

    /// <summary>
    /// 查询文章命令（统一查询，支持分页和过滤）
    /// </summary>
    public record QueryArticlesCommand(
        int Page = 1,
        int PageSize = 10,
        bool? IsPublished = null,
        string? Category = null,
        string[]? Tags = null,
        bool? IsFeatured = null,
        string? SearchKeyword = null);

    /// <summary>
    /// 提交文章命令（Submit按钮，生成HTML并创建版本）
    /// 说明：id为null表示创建新文章并生成首个版本，否则为现有文章创建新版本
    /// 支持用户提供HTML（html参数）或AI自动生成HTML
    /// </summary>
    public record SubmitArticleCommand(
        string Markdown,
        int? Id = null,
        string? Html = null,
        string? Title = null,
        string? Slug = null,
        string? Category = null,
        string[]? Tags = null,
        string? Excerpt = null,
        bool? IsFeatured = null,
        bool? IsPublished = null,
        string? CustomPrompt = null);
}
