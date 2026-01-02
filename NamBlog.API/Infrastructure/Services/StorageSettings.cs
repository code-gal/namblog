using System.IO;

namespace NamBlog.API.Infrastructure.Services
{
    /// <summary>
    /// 存储配置
    /// </summary>
    public class StorageSettings
    {
        private string _dataRootPath = "./data";

        /// <summary>
        /// 数据根目录（所有数据文件的根路径）
        /// </summary>
        public required string DataRootPath
        {
            get => _dataRootPath;
            set
            {
                _dataRootPath = value;
                // 当 DataRootPath 改变时，自动更新所有派生路径
                UpdateDerivedPaths();
            }
        }

        /// <summary>
        /// SQLite 数据库文件路径（派生）
        /// </summary>
        public string DatabasePath { get; private set; } = "./data/sqlite.db";

        /// <summary>
        /// Markdown 文件存储路径（派生）
        /// </summary>
        public string MarkdownPath { get; private set; } = "./data/articles/markdown";

        /// <summary>
        /// HTML 文件存储路径（派生）
        /// </summary>
        public string HtmlPath { get; private set; } = "./data/articles/html";

        /// <summary>
        /// 公开静态资源存储路径（派生，映射到 /resources URL）
        /// </summary>
        public string ResourcesPath { get; private set; } = "./data/resources";

        /// <summary>
        /// 配置文件目录（派生）
        /// </summary>
        public string ConfigPath { get; private set; } = "./data/config";

        /// <summary>
        /// AI Prompts 配置文件路径（派生，JSON 格式）
        /// </summary>
        public string PromptsConfigPath { get; private set; } = "./data/config/prompts.json";

        /// <summary>
        /// 更新所有派生路径
        /// </summary>
        private void UpdateDerivedPaths()
        {
            DatabasePath = Path.Combine(_dataRootPath, "sqlite.db");
            MarkdownPath = Path.Combine(_dataRootPath, "articles", "markdown");
            HtmlPath = Path.Combine(_dataRootPath, "articles", "html");
            ResourcesPath = Path.Combine(_dataRootPath, "resources");
            ConfigPath = Path.Combine(_dataRootPath, "config");
            PromptsConfigPath = Path.Combine(_dataRootPath, "config", "prompts.json");
        }
    }
}
