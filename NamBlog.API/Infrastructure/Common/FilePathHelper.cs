using System.IO;
using System.Linq;

namespace NamBlog.API.Infrastructure.Common
{
    /// <summary>
    /// 文章文件路径辅助类
    /// 统一管理文件路径规则和默认值
    /// </summary>
    public static class FilePathHelper
    {
        /// <summary>
        /// 默认文件路径（根目录）
        /// </summary>
        public const string DefaultFilePath = "";

        /// <summary>
        /// 获取有效的文件路径（处理 null 和空字符串）
        /// </summary>
        public static string GetValidFilePath(string? filePath)
        {
            return string.IsNullOrWhiteSpace(filePath) ? DefaultFilePath : filePath.Trim();
        }

        /// <summary>
        /// 获取 Markdown 相对路径
        /// </summary>
        /// <param name="filePath">分类路径（可空）</param>
        /// <param name="fileName">文件名（不含扩展名）</param>
        /// <returns>相对路径，如 "笔记/文章名.md" 或 "文章名.md"</returns>
        public static string GetMarkdownRelativePath(string? filePath, string fileName)
        {
            var validPath = GetValidFilePath(filePath);
            return string.IsNullOrEmpty(validPath)
                ? $"{fileName}.md"
                : $"{validPath.TrimEnd('/')}/{fileName}.md";
        }

        /// <summary>
        /// 获取 HTML 版本相对路径
        /// </summary>
        /// <param name="filePath">分类路径（可空）</param>
        /// <param name="fileName">文件名</param>
        /// <param name="versionName">版本名称</param>
        /// <returns>相对路径，如 "笔记/文章名/v1/" 或 "文章名/v1/"</returns>
        public static string GetHtmlRelativePath(string? filePath, string fileName, string versionName)
        {
            var validPath = GetValidFilePath(filePath);
            return string.IsNullOrEmpty(validPath)
                ? $"{fileName}/{versionName}/"
                : $"{validPath.TrimEnd('/')}/{fileName}/{versionName}/";
        }

        /// <summary>
        /// 验证文件名是否符合规范
        /// </summary>
        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            // 检查是否包含非法字符（Windows + Linux + macOS）
            char[] invalidChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];
            return !fileName.Any(c => invalidChars.Contains(c));
        }

        /// <summary>
        /// 从完整路径中提取相对路径和文件名
        /// </summary>
        /// <param name="fullPath">文件绝对路径</param>
        /// <param name="markdownPath">*.md 路径</param>
        /// <returns>相对路径和文件名</returns>
        public static (string filePath, string fileName) GetRelativePathAndFileName(string fullPath, string markdownPath)
        {
            var relativePath = Path.GetRelativePath(markdownPath, fullPath);

            var fileName = Path.GetFileNameWithoutExtension(relativePath);
            var directory = Path.GetDirectoryName(relativePath);

            var filePath = string.IsNullOrEmpty(directory) || directory == "."
                ? string.Empty
                : directory.Replace("\\", "/");
            return (filePath, fileName);
        }
    }
}
