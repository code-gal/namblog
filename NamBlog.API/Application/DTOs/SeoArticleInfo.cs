namespace NamBlog.API.Application.DTOs
{
    /// <summary>
    /// SEO 文章信息
    /// 用于 SEO 中间件构造静态 HTML 路径
    /// </summary>
    public record SeoArticleInfo(
        /// <summary>
        /// 文件路径（分类路径）
        /// </summary>
        string FilePath,

        /// <summary>
        /// 文件名（不含扩展名）
        /// </summary>
        string FileName,

        /// <summary>
        /// 版本名称
        /// </summary>
        string VersionName
    );
}
