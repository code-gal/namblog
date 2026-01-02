using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NamBlog.API.Domain.Interfaces;
using NamBlog.API.Infrastructure.Common;

namespace NamBlog.API.Infrastructure.Services
{
    /// <summary>
    /// 文件服务实现
    /// </summary>
    public class FileService(IOptions<StorageSettings> storageSettings) : IFileService
    {
        private readonly StorageSettings _storageSettings = storageSettings.Value;

        // UTF-8 编码（无 BOM，遵循 .editorconfig 的 charset = utf-8）
        private static readonly Encoding _utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public async Task SaveMarkdownAsync(string filePath, string fileName, string content)
        {
            var relativePath = FilePathHelper.GetMarkdownRelativePath(filePath, fileName);
            var fullPath = Path.Combine(_storageSettings.MarkdownPath, relativePath);

            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 直接写入，使用显式 UTF-8 编码
            await File.WriteAllTextAsync(fullPath, content, _utf8WithoutBom);
        }

        public async Task<string?> ReadMarkdownAsync(string filePath, string fileName)
        {
            var relativePath = FilePathHelper.GetMarkdownRelativePath(filePath, fileName);
            var fullPath = Path.Combine(_storageSettings.MarkdownPath, relativePath);

            try
            {
                // 使用 FileShare.Read 允许多个读取者同时访问
                using var fileStream = new FileStream(
                    fullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: true);

                using var reader = new StreamReader(fileStream, _utf8WithoutBom);
                return await reader.ReadToEndAsync();
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
        }

        public Task DeleteMarkdownAsync(string filePath, string fileName)
        {
            var relativePath = FilePathHelper.GetMarkdownRelativePath(filePath, fileName);
            var fullPath = Path.Combine(_storageSettings.MarkdownPath, relativePath);

            try
            {
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"无法删除 Markdown 文件: {fullPath}", ex);
            }

            return Task.CompletedTask;
        }

        public async Task<string> SaveHtmlAsync(string filePath, string fileName, string versionName, string html)
        {
            var relativePath = FilePathHelper.GetHtmlRelativePath(filePath, fileName, versionName);
            var fullPath = Path.Combine(_storageSettings.HtmlPath, relativePath, "index.html");

            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(fullPath, html, _utf8WithoutBom);
            return relativePath;
        }

        public async Task<string?> ReadHtmlAsync(string filePath, string fileName, string versionName)
        {
            var relativePath = FilePathHelper.GetHtmlRelativePath(filePath, fileName, versionName);
            var fullPath = Path.Combine(_storageSettings.HtmlPath, relativePath, "index.html");

            try
            {
                using var fileStream = new FileStream(
                    fullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: true);

                using var reader = new StreamReader(fileStream, _utf8WithoutBom);
                return await reader.ReadToEndAsync();
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
        }

        public Task DeleteHtmlDirectoryAsync(string filePath, string fileName, string versionName)
        {
            var relativePath = FilePathHelper.GetHtmlRelativePath(filePath, fileName, versionName);
            var fullPath = Path.Combine(_storageSettings.HtmlPath, relativePath);

            try
            {
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, recursive: true);
                }
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"无法删除 HTML 目录: {fullPath}", ex);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAllArticleFilesAsync(string filePath, string fileName)
        {
            // 删除 Markdown 文件
            var markdownRelativePath = FilePathHelper.GetMarkdownRelativePath(filePath, fileName);
            var markdownPath = Path.Combine(_storageSettings.MarkdownPath, markdownRelativePath);

            try
            {
                if (File.Exists(markdownPath))
                {
                    File.Delete(markdownPath);
                }
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"无法删除 Markdown 文件: {markdownPath}", ex);
            }

            // 删除整个 HTML 文章目录（包含所有版本）
            var validPath = FilePathHelper.GetValidFilePath(filePath);
            var htmlArticleDir = string.IsNullOrEmpty(validPath)
                ? Path.Combine(_storageSettings.HtmlPath, fileName)
                : Path.Combine(_storageSettings.HtmlPath, validPath, fileName);

            try
            {
                if (Directory.Exists(htmlArticleDir))
                {
                    Directory.Delete(htmlArticleDir, recursive: true);
                }
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"无法删除 HTML 目录: {htmlArticleDir}", ex);
            }

            return Task.CompletedTask;
        }
    }
}
