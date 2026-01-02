namespace NamBlog.API.Infrastructure.Services
{
    /// <summary>
    /// 文件监控配置
    /// </summary>
    public class FileWatcherSettings
    {
        /// <summary>
        /// 是否启用文件监控
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 新创建的文章是否自动发布（公开）
        /// </summary>
        public bool AutoPublish { get; set; } = false;
    }
}
