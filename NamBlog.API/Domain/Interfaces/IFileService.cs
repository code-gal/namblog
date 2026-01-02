using System.Threading.Tasks;

namespace NamBlog.API.Domain.Interfaces
{
    /// <summary>
    /// 文件服务接口
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// 保存 Markdown 文件（不按版本存储）
        /// </summary>
        /// <param name="filePath">分类路径（如 "笔记/"，空字符串表示根目录）</param>
        /// <param name="fileName">文件名（文章标题，不含扩展名）</param>
        /// <param name="content">Markdown 内容</param>
        public Task SaveMarkdownAsync(string filePath, string fileName, string content);

        /// <summary>
        /// 读取 Markdown 文件
        /// </summary>
        public Task<string?> ReadMarkdownAsync(string filePath, string fileName);

        /// <summary>
        /// 删除 Markdown 文件
        /// </summary>
        public Task DeleteMarkdownAsync(string filePath, string fileName);

        /// <summary>
        /// 保存 HTML 文件（按版本存储）
        /// </summary>
        /// <param name="filePath">分类路径（如 "笔记/"）</param>
        /// <param name="fileName">文件名（文章标题）</param>
        /// <param name="versionName">版本名（如 "v1", "v2"）</param>
        /// <param name="html">HTML 内容</param>
        /// <returns>返回相对路径（如 "笔记/文章标题/v1/"）</returns>
        public Task<string> SaveHtmlAsync(string filePath, string fileName, string versionName, string html);

        /// <summary>
        /// 读取 HTML 文件
        /// </summary>
        public Task<string?> ReadHtmlAsync(string filePath, string fileName, string versionName);

        /// <summary>
        /// 删除指定版本的 HTML 目录（包含所有文件）
        /// </summary>
        public Task DeleteHtmlDirectoryAsync(string filePath, string fileName, string versionName);

        /// <summary>
        /// 删除文章的所有文件（Markdown 和所有版本的 HTML）
        /// </summary>
        public Task DeleteAllArticleFilesAsync(string filePath, string fileName);
    }
}
