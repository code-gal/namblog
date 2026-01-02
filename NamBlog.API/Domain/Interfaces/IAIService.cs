using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NamBlog.API.Application.Common;
using NamBlog.API.Domain.ValueObjects;

namespace NamBlog.API.Domain.Interfaces
{
    /// <summary>
    /// AI 服务接口
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// 将 Markdown 渲染为 HTML（同步等待模式）
        /// </summary>
        /// <param name="markdown">Markdown 内容</param>
        /// <param name="customPrompt">自定义 Prompt（可选）</param>
        /// <returns>包含生成的 HTML 内容的结果对象</returns>
        public Task<Result<string>> RenderMarkdownToHtmlAsync(string markdown, string? customPrompt = null);

        /// <summary>
        /// 流式渲染 Markdown 为 HTML（实时推送生成进度）
        /// </summary>
        /// <param name="markdown">Markdown 内容</param>
        /// <param name="customPrompt">自定义 Prompt（可选）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>HTML 生成更新流</returns>
        public IAsyncEnumerable<HtmlRenderProgress> RenderMarkdownToHtmlStreamAsync(
            string markdown,
            string? customPrompt = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 生成文章标题（基于 Markdown 内容）
        /// </summary>
        /// <param name="markdownContent">Markdown 文章内容</param>
        /// <param name="titlePrompt">自定义标题生成提示词（可选）</param>
        /// <returns>生成的标题</returns>
        public Task<Result<string>> GenerateTitleAsync(string markdownContent, string? titlePrompt = null);

        /// <summary>
        /// 生成 Slug（基于标题）
        /// </summary>
        /// <param name="title">文章标题</param>
        /// <param name="slugPrompt">自定义 Slug 生成提示词（可选）</param>
        /// <returns>生成的 Slug</returns>
        public Task<Result<string>> GenerateSlugAsync(string title, string? slugPrompt = null);

        /// <summary>
        /// 生成标签列表（基于 Markdown 内容）
        /// </summary>
        /// <param name="markdownContent">Markdown 文章内容</param>
        /// <param name="tagsPrompt">自定义标签生成提示词（可选）</param>
        /// <returns>生成的标签数组</returns>
        public Task<Result<string[]>> GenerateTagsAsync(string markdownContent, string? tagsPrompt = null);

        /// <summary>
        /// 生成摘要（基于 Markdown 内容）
        /// </summary>
        /// <param name="markdownContent">Markdown 文章内容</param>
        /// <param name="excerptPrompt">自定义摘要生成提示词（可选）</param>
        /// <returns>生成的摘要</returns>
        public Task<Result<string>> GenerateExcerptAsync(string markdownContent, string? excerptPrompt = null);
    }
}
